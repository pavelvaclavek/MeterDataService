using System.Configuration;
using MeterDataService.Web.Data;

namespace MeterDataService.Web
{
    /// <summary>
    /// Centrální konfigurace aplikace.
    /// Drží singleton instanci SqliteDataProvider pro sdílení mezi controllery.
    /// </summary>
    public static class AppConfig
    {
        private static SqliteDataProvider _dataProvider;
        private static readonly object _lock = new object();

        /// <summary>
        /// Cesta k SQLite databázi (načítá se z App.config).
        /// </summary>
        public static string DatabasePath
        {
            get
            {
                var path = ConfigurationManager.AppSettings["Sqlite:DatabasePath"];
                return string.IsNullOrEmpty(path) ? "MeterData.db" : path;
            }
        }

        /// <summary>
        /// Port, na kterém poběží webový server.
        /// </summary>
        public static int WebPort
        {
            get
            {
                var port = ConfigurationManager.AppSettings["WebPort"];
                int result;
                return int.TryParse(port, out result) ? result : 5000;
            }
        }

        /// <summary>
        /// Singleton instance SqliteDataProvider.
        /// Vytvoří se při prvním přístupu (thread-safe).
        /// </summary>
        public static SqliteDataProvider DataProvider
        {
            get
            {
                if (_dataProvider == null)
                {
                    lock (_lock)
                    {
                        if (_dataProvider == null)
                        {
                            _dataProvider = new SqliteDataProvider(DatabasePath);
                        }
                    }
                }
                return _dataProvider;
            }
        }
    }
}
