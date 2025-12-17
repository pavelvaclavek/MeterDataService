using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MeterDataService.Models;
using MeterDataService.Providers;
using Microsoft.Extensions.Options;

namespace MeterDataService.Workers;

public class TcpListenerWorker : BackgroundService
{
    private readonly ILogger<TcpListenerWorker> _logger;
    private readonly IDataProviderManager _providerManager;
    private readonly ServiceConfiguration _config;
    private TcpListener? _listener;

    public TcpListenerWorker(
        ILogger<TcpListenerWorker> logger,
        IDataProviderManager providerManager,
        IOptions<ServiceConfiguration> config)
    {
        _logger = logger;
        _providerManager = providerManager;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener = new TcpListener(IPAddress.Any, _config.ListenPort);

        try
        {
            _listener.Start();
            _logger.LogInformation("TCP Listener started on port {Port}", _config.ListenPort);

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
                }
            }
        }
        finally
        {
            _listener.Stop();
            _logger.LogInformation("TCP Listener stopped");
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken stoppingToken)
    {
        var clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        _logger.LogDebug("Client connected: {Endpoint}", clientEndpoint);

        try
        {
            await using NetworkStream stream = client.GetStream();
            
            using var reader = new StreamReader(stream, Encoding.UTF8);
            await using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

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
                            var message = JsonSerializer.Deserialize<MeterMessage>(jsonMessage);
                            if (message != null)
                            {
                                await _providerManager.ProcessMessageAsync(message, stoppingToken);

                                // Send ACK response
                                var ack = new { status = "ok", sn = message.Sn, timestamp = DateTime.UtcNow };
                                await writer.WriteLineAsync(JsonSerializer.Serialize(ack));
                                _logger.LogInformation("Message processed and acknowledged for SN: {Sn}", message.Sn);
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Invalid JSON received: {Json}", jsonMessage);
                            await writer.WriteLineAsync("{\"status\":\"error\",\"message\":\"Invalid JSON\"}");
                        }
                    }
                }
                catch (IOException ex)
                {
                    // Connection was closed or reset by the client
                    _logger.LogDebug("Connection closed by client {Endpoint}: {Message}", clientEndpoint, ex.Message);
                    break;
                }
                catch (SocketException ex)
                {
                    // Socket error occurred
                    _logger.LogDebug("Socket error with client {Endpoint}: {Message}", clientEndpoint, ex.Message);
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
