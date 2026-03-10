using System;
using System.ServiceProcess;

namespace MeterDataService.Net48
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var service = new MeterDataWindowsService();

            if (Environment.UserInteractive)
            {
                // Spusteni jako konzolova aplikace (pro ladeni)
                service.StartConsole();
            }
            else
            {
                // Spusteni jako Windows Service
                ServiceBase.Run(service);
            }
        }
    }
}
