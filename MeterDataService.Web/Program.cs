using System;
using Microsoft.Owin.Hosting;

namespace MeterDataService.Web
{
    /// <summary>
    /// Vstupní bod aplikace - spustí OWIN self-hosted webový server.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var port = AppConfig.WebPort;
            var baseUrl = string.Format("http://localhost:{0}", port);

            Console.WriteLine("=== MeterDataService.Web ===");
            Console.WriteLine("Databáze: {0}", AppConfig.DatabasePath);
            Console.WriteLine("Spouštím server na: {0}", baseUrl);
            Console.WriteLine();

            try
            {
                using (WebApp.Start<Startup>(baseUrl))
                {
                    Console.WriteLine("Server běží. Otevřete prohlížeč na: {0}", baseUrl);
                    Console.WriteLine("API endpoint: {0}/api/data", baseUrl);
                    Console.WriteLine();
                    Console.WriteLine("Stiskněte Enter pro ukončení...");
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Chyba při spuštění serveru: {0}", ex.Message);
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Tip: Zkontrolujte, zda port {0} není obsazený jiným procesem.", port);
                Console.WriteLine("     Případně spusťte jako Administrator (OWIN self-host vyžaduje oprávnění).");
                Console.WriteLine();
                Console.WriteLine("Stiskněte Enter pro ukončení...");
                Console.ReadLine();
            }
        }
    }
}
