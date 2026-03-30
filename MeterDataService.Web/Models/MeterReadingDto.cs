using System;

namespace MeterDataService.Web.Models
{
    /// <summary>
    /// DTO pro přenos dat o měření z elektroměru do API odpovědi.
    /// </summary>
    public class MeterReadingDto
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; }
        public string Timestamp { get; set; }
        public string MessageId { get; set; }
        public string Network { get; set; }
        public string Model { get; set; }
        public string System { get; set; }
        public double? Data_1_8_0 { get; set; }
        public double? Data_1_8_1 { get; set; }
        public double? Data_1_8_2 { get; set; }
        public double? Data_2_8_0 { get; set; }
        public string CreatedAt { get; set; }
    }

    /// <summary>
    /// Obalový objekt pro stránkovanou odpověď API.
    /// </summary>
    public class PagedResult<T>
    {
        /// <summary>
        /// Kolekce výsledků na aktuální stránce.
        /// </summary>
        public T[] Data { get; set; }

        /// <summary>
        /// Celkový počet záznamů odpovídajících filtru.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Aktuální offset (kolik záznamů bylo přeskočeno).
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Maximální počet záznamů na stránku.
        /// </summary>
        public int Limit { get; set; }
    }
}
