namespace MeterDataService.TestClient;

/// <summary>
/// Testovací zprávy pro DLMS protokol s rùznými scénáøi.
/// </summary>
public static class DlmsTestMessages
{
    /// <summary>
    /// Standardní DLMS profil zpráva s hodinovými daty.
    /// </summary>
    public static string StandardHourlyProfile => """
        {
            "utc": 1771403150,
            "id": 3210,
            "m": "VVC",
            "result": {
                "enc": "text",
                "data": [
                    {
                        "class": "profile",
                        "attr": "columns",
                        "value": "1:8[UINT16],2:0.0.1.0.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:1[UINT16],2:0.0.96.10.8.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.1.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.1.8.1.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.1.8.2.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.2.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.5.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.6.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.7.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.8.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n"
                    },
                    {
                        "class": "profile",
                        "attr": "values",
                        "value": "1:1771313400[TIMESTAMP],2:0[UINT8],3:85646360[UINT64],4:85646360[UINT64],5:0[UINT64],6:1134850[UINT64],7:70646[UINT32],8:695[UINT32],9:40164[UINT32],10:686252[UINT32]\r\n1:1771314300[TIMESTAMP],2:0[UINT8],3:85860760[UINT64],4:85860760[UINT64],5:0[UINT64],6:1136050[UINT64],7:70686[UINT32],8:695[UINT32],9:40172[UINT32],10:687264[UINT32]\r\n"
                    }
                ],
                "form": "json",
                "protocol": "dlms"
            },
            "n": 39,
            "network": "LN00036",
            "system": "nms",
            "cname": "C3/dlms",
            "cdesc": "PROFIL:1H:0",
            "model": "FF0007/AM36x",
            "sn": "21967688",
            "fid": ""
        }
        """;

    /// <summary>
    /// DLMS profil s 15-minutovým intervalem.
    /// </summary>
    public static string QuarterHourlyProfile => """
        {
            "utc": 1771410000,
            "id": 3211,
            "m": "VVC",
            "result": {
                "enc": "text",
                "data": [
                    {
                        "class": "profile",
                        "attr": "columns",
                        "value": "1:8[UINT16],2:0.0.1.0.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.1.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.2.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n"
                    },
                    {
                        "class": "profile",
                        "attr": "values",
                        "value": "1:1771408200[TIMESTAMP],2:0[UINT8],3:12345678[UINT64],4:987654[UINT64]\r\n1:1771409100[TIMESTAMP],2:0[UINT8],3:12346789[UINT64],4:987700[UINT64]\r\n1:1771410000[TIMESTAMP],2:0[UINT8],3:12347890[UINT64],4:987750[UINT64]\r\n1:1771410900[TIMESTAMP],2:0[UINT8],3:12348901[UINT64],4:987800[UINT64]\r\n"
                    }
                ],
                "form": "json",
                "protocol": "dlms"
            },
            "n": 40,
            "network": "LN00037",
            "system": "nms",
            "cname": "C3/dlms",
            "cdesc": "PROFIL:15M:0",
            "model": "FF0007/AM36x",
            "sn": "21967689",
            "fid": ""
        }
        """;

    /// <summary>
    /// DLMS profil s denním intervalem a více registry.
    /// </summary>
    public static string DailyProfile => """
        {
            "utc": 1771430400,
            "id": 3212,
            "m": "VVC",
            "result": {
                "enc": "text",
                "data": [
                    {
                        "class": "profile",
                        "attr": "columns",
                        "value": "1:8[UINT16],2:0.0.1.0.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.1.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.1.8.1.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.1.8.2.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.2.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.3.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.4.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n"
                    },
                    {
                        "class": "profile",
                        "attr": "values",
                        "value": "1:1771344000[TIMESTAMP],2:0[UINT8],3:100000000[UINT64],4:60000000[UINT64],5:40000000[UINT64],6:5000000[UINT64],7:25000000[UINT64],8:15000000[UINT64]\r\n1:1771430400[TIMESTAMP],2:0[UINT8],3:100500000[UINT64],4:60300000[UINT64],5:40200000[UINT64],6:5010000[UINT64],7:25100000[UINT64],8:15050000[UINT64]\r\n"
                    }
                ],
                "form": "json",
                "protocol": "dlms"
            },
            "n": 41,
            "network": "LN00038",
            "system": "nms",
            "cname": "C3/dlms",
            "cdesc": "PROFIL:1D:0",
            "model": "FF0008/AM550",
            "sn": "21967690",
            "fid": ""
        }
        """;

    /// <summary>
    /// DLMS profil s prázdnými hodnotami (nový elektromìr).
    /// </summary>
    public static string EmptyValuesProfile => """
        {
            "utc": 1771440000,
            "id": 3213,
            "m": "VVC",
            "result": {
                "enc": "text",
                "data": [
                    {
                        "class": "profile",
                        "attr": "columns",
                        "value": "1:8[UINT16],2:0.0.1.0.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.1.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.2.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n"
                    },
                    {
                        "class": "profile",
                        "attr": "values",
                        "value": "1:1771440000[TIMESTAMP],2:0[UINT8],3:0[UINT64],4:0[UINT64]\r\n"
                    }
                ],
                "form": "json",
                "protocol": "dlms"
            },
            "n": 42,
            "network": "LN00039",
            "system": "nms",
            "cname": "C3/dlms",
            "cdesc": "PROFIL:1H:0",
            "model": "FF0007/AM36x",
            "sn": "21967691",
            "fid": ""
        }
        """;

    /// <summary>
    /// DLMS profil s maximálními hodnotami (stress test).
    /// </summary>
    public static string MaxValuesProfile => """
        {
            "utc": 1771450000,
            "id": 3214,
            "m": "VVC",
            "result": {
                "enc": "text",
                "data": [
                    {
                        "class": "profile",
                        "attr": "columns",
                        "value": "1:8[UINT16],2:0.0.1.0.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.1.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.2.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n"
                    },
                    {
                        "class": "profile",
                        "attr": "values",
                        "value": "1:1771450000[TIMESTAMP],2:255[UINT8],3:18446744073709551615[UINT64],4:18446744073709551615[UINT64]\r\n"
                    }
                ],
                "form": "json",
                "protocol": "dlms"
            },
            "n": 43,
            "network": "LN00040",
            "system": "nms",
            "cname": "C3/dlms",
            "cdesc": "PROFIL:1H:0",
            "model": "FF0009/AM550",
            "sn": "21967692",
            "fid": ""
        }
        """;

    /// <summary>
    /// DLMS profil s tøífázovým elektromìrem (více registrù na fázi).
    /// </summary>
    public static string ThreePhaseProfile => """
        {
            "utc": 1771460000,
            "id": 3215,
            "m": "VVC",
            "result": {
                "enc": "text",
                "data": [
                    {
                        "class": "profile",
                        "attr": "columns",
                        "value": "1:8[UINT16],2:0.0.1.0.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.1.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.21.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.41.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.61.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.32.7.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.52.7.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.72.7.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n"
                    },
                    {
                        "class": "profile",
                        "attr": "values",
                        "value": "1:1771460000[TIMESTAMP],2:0[UINT8],3:50000000[UINT64],4:16666666[UINT64],5:16666667[UINT64],6:16666667[UINT64],7:2300[UINT16],8:2305[UINT16],9:2298[UINT16]\r\n"
                    }
                ],
                "form": "json",
                "protocol": "dlms"
            },
            "n": 44,
            "network": "LN00041",
            "system": "nms",
            "cname": "C3/dlms",
            "cdesc": "PROFIL:1H:0",
            "model": "FF0010/AM550-3P",
            "sn": "21967693",
            "fid": ""
        }
        """;

    /// <summary>
    /// DLMS profil s chybovým stavem elektromìru.
    /// </summary>
    public static string ErrorStatusProfile => """
        {
            "utc": 1771470000,
            "id": 3216,
            "m": "VVC",
            "result": {
                "enc": "text",
                "data": [
                    {
                        "class": "profile",
                        "attr": "columns",
                        "value": "1:8[UINT16],2:0.0.1.0.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:1[UINT16],2:0.0.96.10.8.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.1.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n"
                    },
                    {
                        "class": "profile",
                        "attr": "values",
                        "value": "1:1771470000[TIMESTAMP],2:128[UINT8],3:99999999[UINT64]\r\n"
                    }
                ],
                "form": "json",
                "protocol": "dlms"
            },
            "n": 45,
            "network": "LN00042",
            "system": "nms",
            "cname": "C3/dlms",
            "cdesc": "PROFIL:1H:0",
            "model": "FF0007/AM36x",
            "sn": "21967694",
            "fid": "POWER_FAIL"
        }
        """;

    /// <summary>
    /// DLMS profil s velkým množstvím záznamù (batch test).
    /// </summary>
    public static string LargeBatchProfile => """
        {
            "utc": 1771480000,
            "id": 3217,
            "m": "VVC",
            "result": {
                "enc": "text",
                "data": [
                    {
                        "class": "profile",
                        "attr": "columns",
                        "value": "1:8[UINT16],2:0.0.1.0.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.1.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.2.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n"
                    },
                    {
                        "class": "profile",
                        "attr": "values",
                        "value": "1:1771473600[TIMESTAMP],2:0[UINT8],3:10000000[UINT64],4:500000[UINT64]\r\n1:1771474500[TIMESTAMP],2:0[UINT8],3:10010000[UINT64],4:500100[UINT64]\r\n1:1771475400[TIMESTAMP],2:0[UINT8],3:10020000[UINT64],4:500200[UINT64]\r\n1:1771476300[TIMESTAMP],2:0[UINT8],3:10030000[UINT64],4:500300[UINT64]\r\n1:1771477200[TIMESTAMP],2:0[UINT8],3:10040000[UINT64],4:500400[UINT64]\r\n1:1771478100[TIMESTAMP],2:0[UINT8],3:10050000[UINT64],4:500500[UINT64]\r\n1:1771479000[TIMESTAMP],2:0[UINT8],3:10060000[UINT64],4:500600[UINT64]\r\n1:1771479900[TIMESTAMP],2:0[UINT8],3:10070000[UINT64],4:500700[UINT64]\r\n"
                    }
                ],
                "form": "json",
                "protocol": "dlms"
            },
            "n": 46,
            "network": "LN00043",
            "system": "nms",
            "cname": "C3/dlms",
            "cdesc": "PROFIL:15M:0",
            "model": "FF0007/AM36x",
            "sn": "21967695",
            "fid": ""
        }
        """;

    /// <summary>
    /// DLMS zpráva s pouze columns (bez values) - konfiguraèní dotaz.
    /// </summary>
    public static string ColumnsOnlyProfile => """
        {
            "utc": 1771490000,
            "id": 3218,
            "m": "VVC",
            "result": {
                "enc": "text",
                "data": [
                    {
                        "class": "profile",
                        "attr": "columns",
                        "value": "1:8[UINT16],2:0.0.1.0.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.1.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.2.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n"
                    }
                ],
                "form": "json",
                "protocol": "dlms"
            },
            "n": 47,
            "network": "LN00044",
            "system": "nms",
            "cname": "C3/dlms",
            "cdesc": "PROFIL:CONFIG",
            "model": "FF0007/AM36x",
            "sn": "21967696",
            "fid": ""
        }
        """;

    /// <summary>
    /// DLMS profil s reaktivní energií (jalová energie).
    /// </summary>
    public static string ReactiveEnergyProfile => """
        {
            "utc": 1771500000,
            "id": 3219,
            "m": "VVC",
            "result": {
                "enc": "text",
                "data": [
                    {
                        "class": "profile",
                        "attr": "columns",
                        "value": "1:8[UINT16],2:0.0.1.0.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.1.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.2.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.3.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.4.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.5.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.6.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.7.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n1:3[UINT16],2:1.1.8.8.0.255[OBIS],3:2[INT8],4:0[UINT16]\r\n"
                    },
                    {
                        "class": "profile",
                        "attr": "values",
                        "value": "1:1771500000[TIMESTAMP],2:0[UINT8],3:75000000[UINT64],4:2500000[UINT64],5:18750000[UINT64],6:6250000[UINT64],7:12500000[UINT64],8:4166666[UINT64],9:25000000[UINT64],10:8333333[UINT64]\r\n"
                    }
                ],
                "form": "json",
                "protocol": "dlms"
            },
            "n": 48,
            "network": "LN00045",
            "system": "nms",
            "cname": "C3/dlms",
            "cdesc": "PROFIL:1H:0",
            "model": "FF0011/AM550-IND",
            "sn": "21967697",
            "fid": ""
        }
        """;

    /// <summary>
    /// Vrátí všechny testovací zprávy jako seznam.
    /// </summary>
    public static IReadOnlyList<(string Name, string Message)> GetAllTestMessages() =>
    [
        (nameof(StandardHourlyProfile), StandardHourlyProfile),
        (nameof(QuarterHourlyProfile), QuarterHourlyProfile),
        (nameof(DailyProfile), DailyProfile),
        (nameof(EmptyValuesProfile), EmptyValuesProfile),
        (nameof(MaxValuesProfile), MaxValuesProfile),
        (nameof(ThreePhaseProfile), ThreePhaseProfile),
        (nameof(ErrorStatusProfile), ErrorStatusProfile),
        (nameof(LargeBatchProfile), LargeBatchProfile),
        (nameof(ColumnsOnlyProfile), ColumnsOnlyProfile),
        (nameof(ReactiveEnergyProfile), ReactiveEnergyProfile)
    ];
}