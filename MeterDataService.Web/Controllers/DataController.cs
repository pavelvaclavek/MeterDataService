using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using MeterDataService.Web.Data;
using MeterDataService.Web.Models;

namespace MeterDataService.Web.Controllers
{
    /// <summary>
    /// API controller pro přístup k datům z elektroměrů.
    /// Endpoint: GET /api/data
    /// </summary>
    public class DataController : ApiController
    {
        private readonly SqliteDataProvider _dataProvider;

        public DataController()
        {
            // Provider se vytváří s cestou k databázi z konfigurace
            _dataProvider = AppConfig.DataProvider;
        }

        /// <summary>
        /// Vrátí stránkovaná data z tabulky MeterReadings.
        /// 
        /// Parametry:
        ///   offset  - počet záznamů k přeskočení (výchozí: 0)
        ///   limit   - počet záznamů na stránku (výchozí: 20, max: 500)
        ///   serialNumber - filtr podle sériového čísla (částečná shoda)
        ///   createdAtFrom - filtr od data (formát: yyyy-MM-dd)
        ///   createdAtTo   - filtr do data (formát: yyyy-MM-dd)
        ///   sortBy  - sloupec pro řazení (SerialNumber, Model, CreatedAt)
        ///   sortDir - směr řazení (asc, desc)
        /// </summary>
        [HttpGet]
        [Route("api/data")]
        public IHttpActionResult Get(
            int offset = 0,
            int limit = 20,
            string serialNumber = null,
            string createdAtFrom = null,
            string createdAtTo = null,
            string sortBy = "CreatedAt",
            string sortDir = "desc")
        {
            try
            {
                // Parsování datumových filtrů
                DateTime? dateFrom = ParseDate(createdAtFrom);
                DateTime? dateTo = ParseDate(createdAtTo);

                var result = _dataProvider.GetReadings(offset, limit, serialNumber, dateFrom, dateTo, sortBy, sortDir);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Logování chyby a vrácení obecné zprávy (bez interních detailů)
                System.Diagnostics.Debug.WriteLine(
                    string.Format("Chyba při čtení dat: {0}", ex));

                return InternalServerError(new Exception("Při načítání dat došlo k chybě."));
            }
        }

        /// <summary>
        /// Bezpečně rozparsuje datum z textového řetězce.
        /// Podporované formáty: yyyy-MM-dd
        /// </summary>
        private static DateTime? ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            DateTime result;
            if (DateTime.TryParseExact(value, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return result;
            }

            return null;
        }
    }
}
