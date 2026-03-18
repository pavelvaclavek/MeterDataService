using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MeterDataService.Logging;
using MeterDataService.Models;
using MeterDataService.Providers;
using Microsoft.Extensions.Options;

namespace MeterDataService.Workers;

public class TcpListenerWorker : BackgroundService
{
    private readonly ILogger<TcpListenerWorker> _logger;
    private readonly IDataProviderManager _providerManager;
    private readonly IAppLogger _appLogger;
    private readonly ServiceConfiguration _config;
    private TcpListener? _listener;

    public TcpListenerWorker(
        ILogger<TcpListenerWorker> logger,
        IDataProviderManager providerManager,
        IAppLogger appLogger,
        IOptions<ServiceConfiguration> config)
    {
        _logger = logger;
        _providerManager = providerManager;
        _appLogger = appLogger;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener = new TcpListener(IPAddress.Any, _config.ListenPort);

        try
        {
            _listener.Start();
            _logger.LogInformation("TCP Listener started on port {Port}", _config.ListenPort);
            await _appLogger.LogInformationAsync(
                $"TCP Listener started on port {_config.ListenPort}", "TcpListener");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                    _ = HandleClientAsync(client, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error accepting TCP client");
                    await _appLogger.LogErrorAsync(
                        "Error accepting TCP client", ex, "TcpListener");
                }
            }
        }
        finally
        {
            _listener.Stop();
            _logger.LogInformation("TCP Listener stopped");
            await _appLogger.LogInformationAsync("TCP Listener stopped", "TcpListener");
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken stoppingToken)
    {
        var clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        var clientIp = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString();
        _logger.LogDebug("Client connected: {Endpoint}", clientEndpoint);

        try
        {
            await using NetworkStream stream = client.GetStream();
            
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

            var buffer = new StringBuilder();
            var charBuffer = new char[4096];
            int bytesRead;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Use ReadAsync with Memory<char> overload that supports CancellationToken
                    bytesRead = await reader.ReadAsync(charBuffer.AsMemory(), stoppingToken);

                    if (bytesRead == 0)
                    {
                        // Client disconnected gracefully
                        _logger.LogDebug("Client {Endpoint} disconnected gracefully", clientEndpoint);
                        break;
                    }

                    buffer.Append(charBuffer, 0, bytesRead);
                    var content = buffer.ToString();

                    // Try to parse complete JSON messages
                    var messages = ExtractJsonMessages(ref content);
                    buffer.Clear();
                    buffer.Append(content);

                    foreach (var jsonMessage in messages)
                    {
                        try
                        {
                            var receivedAt = DateTime.UtcNow;
                            // --- DLMS detection: peek at result.data value kind ---
                            bool isDlms = false;
                            try
                            {
                                using var doc = JsonDocument.Parse(jsonMessage);
                                isDlms = doc.RootElement.TryGetProperty("result", out var resultEl)
                                      && resultEl.TryGetProperty("data", out var dataEl)
                                      && dataEl.ValueKind == JsonValueKind.Array;
                            }
                            catch
                            {
                                // If JsonDocument.Parse fails, fall through to existing path which
                                // will also fail and send the error response.
                            }

                            if (isDlms)
                            {
                                // --- DLMS branch ---
                                var dlmsMsg = JsonSerializer.Deserialize<DlmsMessage>(jsonMessage);
                                if (dlmsMsg != null)
                                {
                                    var meterMessages = DlmsMessageConverter.ToMeterMessages(dlmsMsg).ToList();
                                    foreach (var msg in meterMessages)
                                    {
                                        await _providerManager.ProcessMessageAsync(msg, stoppingToken);
                                    }

                                    var ack = new
                                    {
                                        status = "ok",
                                        sn = dlmsMsg.Sn,
                                        count = meterMessages.Count,
                                        timestamp = DateTime.UtcNow
                                    };
                                    var response = JsonSerializer.Serialize(ack) + "\n";
                                    var responseBytes = Encoding.UTF8.GetBytes(response);
                                    await stream.WriteAsync(responseBytes, stoppingToken);
                                    await stream.FlushAsync(stoppingToken);

                                    _logger.LogInformation(
                                        "DLMS message processed: SN={Sn}, Rows={Count}", dlmsMsg.Sn, meterMessages.Count);
                                }
                            }
                            else
                            {
                                // --- Existing MeterMessage branch (UNCHANGED) ---
                                var message = JsonSerializer.Deserialize<MeterMessage>(jsonMessage);
                                if (message != null)
                                {
                                    await _providerManager.ProcessMessageAsync(message, stoppingToken);

                                    // Send ACK response directly to stream
                                    var ack = new { status = "ok", sn = message.Sn, timestamp = DateTime.UtcNow };
                                    var response = JsonSerializer.Serialize(ack) + "\n";
                                    var responseBytes = Encoding.UTF8.GetBytes(response);
                                    await stream.WriteAsync(responseBytes, stoppingToken);
                                    // call always await stream.FlushAsync to ensure data is sent immediately
                                    await stream.FlushAsync(stoppingToken);

                                    _logger.LogInformation("Message processed and acknowledged for SN: {Sn}", message.Sn);
                                }
                            }                                
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Invalid JSON received: {Json}", jsonMessage);
                            await _appLogger.LogErrorAsync(
                                $"Invalid JSON received from {clientEndpoint}. JSON message {jsonMessage}", ex, "Error", clientIp);
                            var errorResponse = "{\"status\":\"error\",\"message\":\"Invalid JSON\"}\n";
                            var errorBytes = Encoding.UTF8.GetBytes(errorResponse);
                            await stream.WriteAsync(errorBytes, stoppingToken);
                            await stream.FlushAsync(stoppingToken);
                        }
                    }
                }
                catch (IOException ex)
                {
                    // Connection was closed or reset by the client
                    _logger.LogDebug("Connection closed by client {Endpoint}: {Message}", clientEndpoint, ex.Message);
                    await _appLogger.LogWarningAsync(
                        $"Connection closed by client {clientEndpoint}: {ex.Message}", "TcpListener", clientIp);
                    break;
                }
                catch (SocketException ex)
                {
                    // Socket error occurred
                    _logger.LogDebug("Socket error with client {Endpoint}: {Message}", clientEndpoint, ex.Message);
                    await _appLogger.LogWarningAsync(
                        $"Socket error with client {clientEndpoint}: {ex.Message}", "TcpListener", clientIp);
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Operation cancelled for client {Endpoint}", clientEndpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling client {Endpoint}", clientEndpoint);
            await _appLogger.LogErrorAsync(
                $"Error handling client {clientEndpoint}", ex, "TcpListener", clientIp);
        }
        finally
        {
            client.Close();
            _logger.LogDebug("Client disconnected: {Endpoint}", clientEndpoint);
        }
    }

    private static List<string> ExtractJsonMessages(ref string buffer)
    {
        var messages = new List<string>();
        var depth = 0;
        var start = -1;

        for (var i = 0; i < buffer.Length; i++)
        {
            switch (buffer[i])
            {
                case '{':
                    if (depth == 0) start = i;
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth == 0 && start >= 0)
                    {
                        messages.Add(buffer.Substring(start, i - start + 1));
                        start = -1;
                    }
                    break;
            }
        }

        if (messages.Count > 0)
        {
            var lastEnd = buffer.LastIndexOf('}') + 1;
            buffer = buffer[lastEnd..];
        }

        return messages;
    }
}
