using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace MeterDataService.Net48
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller _serviceProcessInstaller;
        private ServiceInstaller _serviceInstaller;

        public ProjectInstaller()
        {
            _serviceProcessInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalSystem
            };

            _serviceInstaller = new ServiceInstaller
            {
                ServiceName = "MeterDataService",
                DisplayName = "Meter Data Service (.NET 4.8)",
                Description = "Sluzba pro prijem a zpracovani dat z elektrometru pres TCP",
                StartType = ServiceStartMode.Automatic
            };

            Installers.Add(_serviceProcessInstaller);
            Installers.Add(_serviceInstaller);
        }
    }
}
