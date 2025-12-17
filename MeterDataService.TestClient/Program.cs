using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace MeterDataService.TestClient;

class Program
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { WriteIndented = true };

    private static string _targetHost = "127.0.0.1";
    private static int _targetPort = 461;
    private static int _timeoutSeconds = 10;

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        PrintHeader();

        // Parse command line arguments for non-interactive mode
        if (args.Length > 0)
        {
            await HandleCommandLineArgs(args);
            return;
        }

        // Interactive mode
        while (true)
        {
            PrintMenu();
            var choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    await SendSingleMessageAsync();
                    break;
                case "2":
                    await SendMultipleMessagesAsync();
                    break;
                case "3":
                    await SendCustomMessageAsync();
                    break;
                case "4":
                    await TestConnectionAsync();
                    break;
                case "5":
                    await LoadTestAsync();
                    break;
                case "6":
                    ConfigureSettings();
                    break;
                case "7":
                case "q":
                case "Q":
                    Console.WriteLine("Ukonèuji...");
                    return;
                default:
                    WriteColorLine("Neplatná volba. Zkuste znovu.", ConsoleColor.Yellow);
                    break;
            }

            Console.WriteLine();
        }
    }

    private static void PrintHeader()
    {
        Console.Clear();
        WriteColorLine("????????????????????????????????????????????????????",
                       ConsoleColor.Cyan);
        WriteColorLine("?     MeterDataService Test Client v1.0   ?",
                       ConsoleColor.Cyan);
        WriteColorLine("?       Testování TCP komunikace se službou  ?",
                       ConsoleColor.Cyan);
        WriteColorLine("????????????????????????????????????????????????????",
                       ConsoleColor.Cyan);
        Console.WriteLine();
    }

    private static void PrintMenu()
    {
        WriteColorLine(
            $"Aktuální konfigurace: {_targetHost}:{_targetPort} (timeout: {_timeoutSeconds}s)",
            ConsoleColor.Gray);
        Console.WriteLine();
        WriteColorLine("Vyberte akci:", ConsoleColor.Green);
        Console.WriteLine("  1. Odeslat jednu testovací zprávu");
        Console.WriteLine("  2. Odeslat více zpráv (simulace)");
        Console.WriteLine("  3. Odeslat vlastní JSON zprávu");
        Console.WriteLine("  4. Test pøipojení (ping)");
        Console.WriteLine("  5. Zátìžový test");
        Console.WriteLine("  6. Konfigurace");
        Console.WriteLine("  7. Konec (q)");
        Console.WriteLine();
        Console.Write("Vaše volba: ");
    }

    private static async Task HandleCommandLineArgs(string[] args)
    {
        // Usage: TestClient.exe <host> <port> [count]
        if (args.Length >= 1)
            _targetHost = args[0];
        if (args.Length >= 2 && int.TryParse(args[1], out var port))
            _targetPort = port;

        var count = 1;
        if (args.Length >= 3 && int.TryParse(args[2], out var c))
            count = c;

        Console.WriteLine(
            $"Odesílám {count} zpráv na {_targetHost}:{_targetPort}...");

        for (var i = 0; i < count; i++)
        {
            var message = CreateTestMessage(GenerateRandomSn(), i + 1);
            var result = await SendMessageAsync(message);

            if (result.Success)
                WriteColorLine($"[{i + 1}/{count}] OK - {result.ElapsedMs}ms",
                               ConsoleColor.Green);
            else
                WriteColorLine($"[{i + 1}/{count}] FAIL - {result.Error}",
                               ConsoleColor.Red);
        }
    }

    private static void ConfigureSettings()
    {
        Console.WriteLine();
        Console.Write($"Cílová IP adresa [{_targetHost}]: ");
        var input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input))
            _targetHost = input;

        Console.Write($"Cílový port [{_targetPort}]: ");
        input = Console.ReadLine();
        if (int.TryParse(input, out var port))
            _targetPort = port;

        Console.Write($"Timeout v sekundách [{_timeoutSeconds}]: ");
        input = Console.ReadLine();
        if (int.TryParse(input, out var timeout))
            _timeoutSeconds = timeout;

        WriteColorLine(
            $"Konfigurace uložena: {_targetHost}:{_targetPort}, timeout: {_timeoutSeconds}s",
            ConsoleColor.Green);
    }

    private static async Task SendSingleMessageAsync()
    {
        Console.WriteLine();
        Console.Write($"Sériové èíslo (SN) [náhodné]: ");
        var input = Console.ReadLine();
        var serialNumber =
            string.IsNullOrWhiteSpace(input) ? GenerateRandomSn() : input;

        var message = CreateTestMessage(serialNumber);
        await SendAndDisplayResultAsync(message);
    }

    private static async Task SendMultipleMessagesAsync()
    {
        Console.WriteLine();
        Console.Write("Poèet zpráv [5]: ");
        var input = Console.ReadLine();
        var count = int.TryParse(input, out var c) ? c : 5;

        Console.Write($"Sériové èíslo (SN) [náhodné]: ");
        input = Console.ReadLine();
        var serialNumber =
            string.IsNullOrWhiteSpace(input) ? GenerateRandomSn() : input;

        Console.Write("Prodleva mezi zprávami v ms [1000]: ");
        input = Console.ReadLine();
        var delayMs = int.TryParse(input, out var d) ? d : 1000;

        Console.WriteLine();
        WriteColorLine(
            $"{"#",-4} {"Èas",-15} {"SN",-12} {"Stav",-8} {"Èas (ms)",-10} Odpovìï",
            ConsoleColor.Cyan);
        WriteColorLine(new string('-', 80), ConsoleColor.Gray);

        var successCount = 0;
        var failCount = 0;

        for (var i = 1; i <= count; i++)
        {
            var message = CreateTestMessage(serialNumber, i);
            var result = await SendMessageAsync(message);

            var status = result.Success ? "OK" : "FAIL";
            var statusColor = result.Success ? ConsoleColor.Green : ConsoleColor.Red;

            if (result.Success)
                successCount++;
            else
                failCount++;

            Console.Write($"{i,-4} {DateTime.Now:HH:mm:ss.fff}  {serialNumber,-12} ");
            WriteColor($"{status,-8}", statusColor);
            Console.WriteLine(
                $" {result.ElapsedMs,-10} {TruncateResponse(result.Response ?? result.Error ?? "-")}");

            if (i < count)
                await Task.Delay(delayMs);
        }

        Console.WriteLine();
        WriteColorLine(
            $"Celkem: {count}, Úspìšných: {successCount}, Neúspìšných: {failCount}",
            failCount == 0 ? ConsoleColor.Green : ConsoleColor.Yellow);
    }

    private static async Task SendCustomMessageAsync()
    {
        Console.WriteLine();
        WriteColorLine("Zadejte JSON zprávu (prázdný øádek pro odeslání):",
                       ConsoleColor.Yellow);

        var lines = new List<string>();
        while (true)
        {
            var line = Console.ReadLine();
            if (string.IsNullOrEmpty(line))
                break;
            lines.Add(line);
        }

        var json = string.Join("\n", lines);

        if (string.IsNullOrWhiteSpace(json))
        {
            WriteColorLine("Prázdná zpráva, odesílání zrušeno.", ConsoleColor.Red);
            return;
        }

        try
        {
            JsonDocument.Parse(json);
            await SendAndDisplayResultAsync(json);
        }
        catch (JsonException ex)
        {
            WriteColorLine($"Neplatný JSON: {ex.Message}", ConsoleColor.Red);
        }
    }

    private static async Task TestConnectionAsync()
    {
        Console.WriteLine();
        Console.Write($"Testuji pøipojení k {_targetHost}:{_targetPort}... ");

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(_targetHost, _targetPort);

            if (await Task.WhenAny(
                    connectTask, Task.Delay(TimeSpan.FromSeconds(_timeoutSeconds))) ==
                connectTask)
            {
                await connectTask;
                WriteColorLine("? Pøipojení úspìšné!", ConsoleColor.Green);
            }
            else
            {
                WriteColorLine("? Timeout - server neodpovídá", ConsoleColor.Red);
            }
        }
        catch (SocketException ex)
        {
            WriteColorLine($"? Chyba: {ex.Message}", ConsoleColor.Red);
        }
    }

    private static async Task LoadTestAsync()
    {
        Console.WriteLine();
        Console.Write("Celkový poèet zpráv [100]: ");
        var input = Console.ReadLine();
        var messageCount = int.TryParse(input, out var mc) ? mc : 100;

        Console.Write("Poèet soubìžných pøipojení [10]: ");
        input = Console.ReadLine();
        var concurrency = int.TryParse(input, out var cc) ? cc : 10;

        Console.WriteLine();
        WriteColorLine(
            $"Spouštím zátìžový test: {messageCount} zpráv, {concurrency} soubìžných pøipojení...",
            ConsoleColor.Yellow);
        Console.WriteLine();

        var results = new LoadTestResults();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var completed = 0;

        var semaphore = new SemaphoreSlim(concurrency);
        var tasks = new List<Task>();

        for (var i = 0; i < messageCount; i++)
        {
            await semaphore.WaitAsync();

            var messageIndex = i;
            tasks.Add(Task.Run(async () => {
                try
                {
                    var message = CreateTestMessage(GenerateRandomSn(), messageIndex);
                    var result = await SendMessageAsync(message);

                    lock (results)
                    {
                        if (result.Success)
                            results.SuccessCount++;
                        else
                            results.FailureCount++;

                        results.TotalTime += result.ElapsedMs;
                        completed++;

                        // Progress update every 10%
                        if (completed % (messageCount / 10 + 1) == 0)
                        {
                            Console.Write(
                                $"\rProgress: {completed}/{messageCount} ({completed * 100 / messageCount}%)   ");
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine();
        WriteColorLine("???????????????????????????????????????",
                       ConsoleColor.Cyan);
        WriteColorLine("  VÝSLEDKY ZÁTÌŽOVÉHO TESTU       ", ConsoleColor.Cyan);
        WriteColorLine("???????????????????????????????????????",
                       ConsoleColor.Cyan);
        Console.WriteLine($"  Celkem zpráv:        {messageCount}");
        WriteColor("  Úspìšných:         ", ConsoleColor.White);
        WriteColorLine($"{results.SuccessCount}", ConsoleColor.Green);
        WriteColor("  Neúspìšných:         ", ConsoleColor.White);
        WriteColorLine($"{results.FailureCount}", ConsoleColor.Red);
        Console.WriteLine(
            $"  Celkový èas:         {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine(
            $"  Prùmìrný èas/zpráva: {(results.SuccessCount > 0 ? results.TotalTime / results.SuccessCount : 0):F2} ms");
        Console.WriteLine(
            $"  Zpráv/sekundu:       {messageCount / (stopwatch.ElapsedMilliseconds / 1000.0):F2}");
        WriteColorLine("???????????????????????????????????????",
                       ConsoleColor.Cyan);
    }

    private static async Task SendAndDisplayResultAsync(string json)
    {
        Console.WriteLine();
        WriteColorLine("Odesílaná zpráva:", ConsoleColor.Gray);
        WriteColorLine("?????????????????????????????????????", ConsoleColor.Gray);
        Console.WriteLine(json);
        WriteColorLine("?????????????????????????????????????", ConsoleColor.Gray);
        Console.WriteLine();

        Console.Write("Odesílám... ");
        var result = await SendMessageAsync(json);
       
        if (result.Success)
        {
            WriteColorLine(
                $"? Zpráva úspìšnì odeslána a potvrzena ({result.ElapsedMs}ms)",
                ConsoleColor.Green);
            if (!string.IsNullOrEmpty(result.Response))
            {
                Console.WriteLine();
                WriteColorLine("Odpovìï serveru:", ConsoleColor.Cyan);
                WriteColorLine("?????????????????????????????????????",
                               ConsoleColor.Gray);
                Console.WriteLine(result.Response);
                WriteColorLine("?????????????????????????????????????",
                               ConsoleColor.Gray);
            }
        }
        else
        {
            WriteColorLine($"? Chyba: {result.Error}", ConsoleColor.Red);
        }
    }

    private static async Task<SendResult> SendMessageAsync(string json)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var client = new TcpClient();

            var connectTask = client.ConnectAsync(_targetHost, _targetPort);
            
            if (await Task.WhenAny(
                    connectTask, Task.Delay(TimeSpan.FromSeconds(_timeoutSeconds))) !=
                connectTask)
            {
                return new SendResult { Success = false, Error = "Connection timeout" };
            }

            await connectTask;

            await using var stream = client.GetStream();
            stream.ReadTimeout = _timeoutSeconds * 1000;
            stream.WriteTimeout = _timeoutSeconds * 1000;

            // Send message
            var data = Encoding.UTF8.GetBytes(json);
            await stream.WriteAsync(data);

            // Wait for response
            var buffer = new byte[4096];
            using var cts =
                new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));

            try
            {
                var bytesRead = await stream.ReadAsync(buffer, cts.Token);
                stopwatch.Stop();

                if (bytesRead > 0)
                {
                    var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    return new SendResult
                    {
                        Success = true,
                        Response = response,
                        ElapsedMs = stopwatch.ElapsedMilliseconds
                    };
                }

                return new SendResult
                {
                    Success = false,
                    Error = "No response received",
                    ElapsedMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (OperationCanceledException)
            {
                return new SendResult
                {
                    Success = false,
                    Error = "Read timeout",
                    ElapsedMs = stopwatch.ElapsedMilliseconds
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new SendResult
            {
                Success = false,
                Error = ex.Message,
                ElapsedMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    private static string CreateTestMessage(string serialNumber, int index = 1)
    {
        var utcTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var random = new Random();

        var message = new
        {
            utc = utcTimestamp.ToString(),
            result =
              new
              {
                  enc = "text",
                  data =
                    $"1.8.1({random.Next(0, 1000):D7}.{random.Next(0, 9)}#kWh)\r\n" +
                    $"1.8.2({random.Next(0, 100):D7}.{random.Next(0, 9)}*kWh)\r\n" +
                    $"1.8.0({random.Next(0, 1000):D7}.{random.Next(0, 9)}*kWh)\r\n" +
                    $"2.8.0({random.Next(0, 10):D7}.{random.Next(0, 9)}*kWh)\r\n"
              },
            id = (1000 + index).ToString(),
            n = index.ToString(),
            m = $"L{random.Next(0, 100):D7}",
            network = $"LN{random.Next(0, 100):D5}",
            system = "nms",
            cname = "C0/ro",
            cdesc = "1.8.0:1.8.1:1.8.2:2.8.0",
            model = "FF0007/ED310",
            sn = serialNumber,
            fid = ""
        };

        return JsonSerializer.Serialize(message, JsonOptions);
    }

    private static string GenerateRandomSn()
    {
        return new Random().Next(10000000, 99999999).ToString();
    }

    private static string TruncateResponse(string response)
    {
        var singleLine = response.Replace("\r", "").Replace("\n", " ");
        return singleLine.Length > 40 ? singleLine[..37] + "..." : singleLine;
    }

    private static void WriteColorLine(string text, ConsoleColor color)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = originalColor;
    }

    private static void WriteColor(string text, ConsoleColor color)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = originalColor;
    }

    private class SendResult
    {
        public bool Success { get; set; }
        public string? Response { get; set; }
        public string? Error { get; set; }
        public long ElapsedMs { get; set; }
    }

    private class LoadTestResults
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public long TotalTime { get; set; }
    }
}
