using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MeterDataService.Net48.Logging;
using MeterDataService.Net48.Models;
using MeterDataService.Net48.Providers;
using MeterDataService.Net48.Workers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MeterDataService.Net48
{
    public class MeterDataWindowsService : ServiceBase
    {
        private readonly EventLog _eventLog;
        private CancellationTokenSource _cts;
        private TcpListener _listener;
        private Task _listenerTask;
        private ServiceConfiguration _config;
        private IDataProviderManager _providerManager;
        private IAppLogger _appLogger;

        public MeterDataWindowsService()
        {
            ServiceName = "MeterDataService";

            _eventLog = new EventLog();
            if (!EventLog.SourceExists("MeterDataService"))
            {
                EventLog.CreateEventSource("MeterDataService", "Application");
            }
            _eventLog.Source = "MeterDataService";
            _eventLog.Log = "Application";
        }

        protected override void OnStart(string[] args)
        {
            _eventLog.WriteEntry("MeterDataService starting...", EventLogEntryType.Information);

            _config = ServiceConfiguration.Load();
            _cts = new CancellationTokenSource();

            // Inicializace App Loggeru - konfigurovatelný
            if (_config.AppLogging.Enabled &&
                string.Equals(_config.AppLogging.Logger, "sqlite", StringComparison.OrdinalIgnoreCase))
            {
                _appLogger = new SqliteAppLogger(_eventLog, _config.Sqlite.DatabasePath);
            }
            else
            {
                _appLogger = new NullAppLogger();
            }

            // Inicializace provideru
            var providers = new List<IDataProvider>
            {
                new CsvDataProvider(_eventLog, _appLogger, _config),
                new EmailDataProvider(_eventLog, _appLogger, _config),
                new DatabaseDataProvider(_eventLog, _appLogger),
                new SqliteDataProvider(_eventLog, _appLogger, _config)
            };

            _providerManager = new DataProviderManager(_eventLog, _appLogger, providers, _config);

            // Spusteni TCP listeneru na pozadi
            _listenerTask = Task.Run(() => RunTcpListenerAsync(_cts.Token));

            _eventLog.WriteEntry(
                string.Format("MeterDataService started on port {0}", _config.ListenPort),
                EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            _eventLog.WriteEntry("MeterDataService stopping...", EventLogEntryType.Information);
            _appLogger.LogInformationAsync("MeterDataService stopping", "Service").Wait();

            _cts.Cancel();

            // Zastaveni listeneru zpusobi vyjimku v AcceptTcpClientAsync
            if (_listener != null)
            {
                _listener.Stop();
            }

            try
            {
                // Pockame na dokonceni (max 10 sekund)
                if (_listenerTask != null && !_listenerTask.Wait(TimeSpan.FromSeconds(10)))
                {
                    _eventLog.WriteEntry("TCP listener did not stop in time", EventLogEntryType.Warning);
                }
            }
            catch (AggregateException)
            {
                // Ocekavane pri cancellation
            }

            _cts.Dispose();
            _eventLog.WriteEntry("MeterDataService stopped", EventLogEntryType.Information);
        }

        private async Task RunTcpListenerAsync(CancellationToken stoppingToken)
        {
            _listener = new TcpListener(IPAddress.Any, _config.ListenPort);

            try
            {
                _listener.Start();
                _eventLog.WriteEntry(
                    string.Format("TCP Listener started on port {0}", _config.ListenPort),
                    EventLogEntryType.Information);
                await _appLogger.LogInformationAsync(
                    string.Format("TCP Listener started on port {0}", _config.ListenPort), "TcpListener");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        // .NET 4.8 AcceptTcpClientAsync nema CancellationToken overload
                        // Pri zastaveni sluzby se zavola _listener.Stop(), coz vyhodi vyjimku
                        var client = await _listener.AcceptTcpClientAsync();
                        var _ = HandleClientAsync(client, stoppingToken);
                    }
                    catch (ObjectDisposedException)
                    {
                        // Listener byl zastaven
                        break;
                    }
                    catch (SocketException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (!stoppingToken.IsCancellationRequested)
                        {
                            _eventLog.WriteEntry(
                                string.Format("Error accepting TCP client: {0}", ex),
                                EventLogEntryType.Error);
                            await _appLogger.LogErrorAsync(
                                "Error accepting TCP client", ex, "TcpListener");
                        }
                    }
                }
            }
            finally
            {
                try { _listener.Stop(); }
                catch { /* uz zastaveno */ }

                _eventLog.WriteEntry("TCP Listener stopped", EventLogEntryType.Information);
                await _appLogger.LogInformationAsync("TCP Listener stopped", "TcpListener");
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken stoppingToken)
        {
            var clientEndpoint = client.Client.RemoteEndPoint != null
                ? client.Client.RemoteEndPoint.ToString()
                : "unknown";
            var clientIp = (client.Client.RemoteEndPoint as IPEndPoint) != null
                ? ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString()
                : null;

            try
            {
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8, false, 4096, true))
                {
                    var buffer = new StringBuilder();
                    var charBuffer = new char[4096];
                    int bytesRead;

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            // .NET 4.8 ReadAsync nema Memory<char> overload
                            bytesRead = await reader.ReadAsync(charBuffer, 0, charBuffer.Length);

                            if (bytesRead == 0)
                            {
                                break;
                            }

                            buffer.Append(charBuffer, 0, bytesRead);
                            var content = buffer.ToString();

                            var messages = ExtractJsonMessages(ref content);
                            buffer.Clear();
                            buffer.Append(content);

                            foreach (var jsonMessage in messages)
                            {
                                try
                                {
                                    // Detekce DLMS formatu
                                    bool isDlms = false;
                                    try
                                    {
                                        var obj = JObject.Parse(jsonMessage);
                                        var resultToken = obj["result"];
                                        if (resultToken != null)
                                        {
                                            var dataToken = resultToken["data"];
                                            isDlms = dataToken != null && dataToken.Type == JTokenType.Array;
                                        }
                                    }
                                    catch
                                    {
                                        // Pokud parse selze, pokracujeme standardni cestou
                                        await _appLogger.LogWarningAsync(
                                            "Failed to detect message format", "TcpListener", clientIp);
                                    }

                                    if (isDlms)
                                    {
                                        var dlmsMsg = JsonConvert.DeserializeObject<DlmsMessage>(jsonMessage);
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
                                            var response = JsonConvert.SerializeObject(ack) + "\n";
                                            var responseBytes = Encoding.UTF8.GetBytes(response);
                                            await stream.WriteAsync(responseBytes, 0, responseBytes.Length, stoppingToken);
                                            await stream.FlushAsync(stoppingToken);

                                            _eventLog.WriteEntry(
                                                string.Format("DLMS message processed: SN={0}, Rows={1}", dlmsMsg.Sn, meterMessages.Count),
                                                EventLogEntryType.Information);
                                        }
                                    }
                                    else
                                    {
                                        var message = JsonConvert.DeserializeObject<MeterMessage>(jsonMessage);
                                        if (message != null)
                                        {
                                            await _providerManager.ProcessMessageAsync(message, stoppingToken);

                                            var ack = new { status = "ok", sn = message.Sn, timestamp = DateTime.UtcNow };
                                            var response = JsonConvert.SerializeObject(ack) + "\n";
                                            var responseBytes = Encoding.UTF8.GetBytes(response);
                                            await stream.WriteAsync(responseBytes, 0, responseBytes.Length, stoppingToken);
                                            await stream.FlushAsync(stoppingToken);

                                            _eventLog.WriteEntry(
                                                string.Format("Message processed and acknowledged for SN: {0}", message.Sn),
                                                EventLogEntryType.Information);
                                        }
                                    }
                                }
                                catch (JsonException ex)
                                {
                                    _eventLog.WriteEntry(
                                        string.Format("Invalid JSON received: {0}", ex.Message),
                                        EventLogEntryType.Warning);
                                    await _appLogger.LogErrorAsync(
                                        string.Format("Invalid JSON received from {0}. JSON {1} ", clientEndpoint, jsonMessage), ex,
                                        "TcpListener", clientIp);

                                    var errorResponse = "{\"status\":\"error\",\"message\":\"Invalid JSON\"}\n";
                                    var errorBytes = Encoding.UTF8.GetBytes(errorResponse);
                                    await stream.WriteAsync(errorBytes, 0, errorBytes.Length, stoppingToken);
                                    await stream.FlushAsync(stoppingToken);
                                }
                            }
                        }
                        catch (IOException ex)
                        {
                            await _appLogger.LogWarningAsync(
                                string.Format("Connection closed by client {0}: {1}", clientEndpoint, ex.Message),
                                "TcpListener", clientIp);
                            break;
                        }
                        catch (SocketException ex)
                        {
                            await _appLogger.LogWarningAsync(
                                string.Format("Socket error with client {0}: {1}", clientEndpoint, ex.Message),
                                "TcpListener", clientIp);
                            break;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ocekavane pri zastaveni
            }
            catch (Exception ex)
            {
                _eventLog.WriteEntry(
                    string.Format("Error handling client {0}: {1}", clientEndpoint, ex),
                    EventLogEntryType.Error);
                await _appLogger.LogErrorAsync(
                    string.Format("Error handling client {0}", clientEndpoint),
                    ex, "TcpListener", clientIp);
            }
            finally
            {
                client.Close();
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
                buffer = buffer.Substring(lastEnd);
            }

            return messages;
        }

        /// <summary>
        /// Umoznuje spusteni jako konzolova aplikace pro ladeni.
        /// </summary>
        public void StartConsole()
        {
            OnStart(null);
            Console.WriteLine("MeterDataService running on port {0}. Press Enter to stop...", _config.ListenPort);
            Console.ReadLine();
            OnStop();
        }
    }
}
