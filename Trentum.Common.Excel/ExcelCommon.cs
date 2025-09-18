using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClosedXML.Excel;

namespace Trentum.Common.Excel;

public static class ExcelCommon
{
    // ===== Nagłówki i aliasy (opcjonalna tolerancja) =====
    // Klucz = nazwa kanoniczna; Wartości = dopuszczalne nagłówki w Excelu
    public static readonly Dictionary<string, string[]> HeaderAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Stopień"] = new[] { "Stopień", "Stopien" },
            ["Imiona"] = new[] { "Imiona" },
            ["Nazwisko"] = new[] { "Nazwisko" },
            ["PESEL"] = new[] { "PESEL" },
            ["Stanowisko"] = new[] { "Stanowisko" },
            ["Nr etatu"] = new[] { "Nr etatu", "Nr Etatu", "Nr_etatu", "NrEtatu" },
            ["Nazwa jednostki wojskowej"] = new[]
            {
                "Nazwa jednostki wojskowej", "Nazwa jednostki", "Jednostka"
            },
        };

    /// Buduje mapę nagłówków: Nazwa->Index (1-based). Bazuje na Twojej implementacji.
    public static Dictionary<string, int> BuildHeaderMap(IXLWorksheet ws, int headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? ws.Row(headerRow).LastCellUsed()?.Address.ColumnNumber ?? 0;

        for (int c = 1; c <= lastCol; c++)
        {
            var name = GetTrimmed(ws.Cell(headerRow, c));
            if (!string.IsNullOrEmpty(name) && !map.ContainsKey(name))
                map[name] = c;
        }
        return map;
    }

    /// Próbuj znaleźć indeks kolumny po kanoniku + aliasach; w pierwszej kolejności dokładna nazwa z BuildHeaderMap.
    public static bool TryResolveColumn(Dictionary<string, int> headerMap, string canonicalName, out int col)
    {
        // 1) Dokładny klucz (tak działało w Validatorze)
        if (headerMap.TryGetValue(canonicalName, out col))
            return true;

        // 2) Alias
        if (HeaderAliases.TryGetValue(canonicalName, out var aliases))
        {
            foreach (var a in aliases)
                if (headerMap.TryGetValue(a, out col))
                    return true;
        }

        col = 0;
        return false;
    }

    public static bool IsRowEmpty(IXLRow row, int lastDataCol)
    {
        for (int c = 1; c <= lastDataCol; c++)
            if (!string.IsNullOrWhiteSpace(row.Cell(c).GetString()))
                return false;
        return true;
    }

    public static string GetTrimmed(IXLCell cell)
        => cell.IsEmpty() ? "" : cell.GetString().Trim();

    /// Pobranie PESEL identyczne jak u Ciebie (zachowuje leading zeros, padLeft do 11)
    public static string GetPesel(IXLCell cell)
    {
        if (cell.IsEmpty()) return "";

        if (cell.DataType == XLDataType.Number)
        {
            var asLong = (long)Math.Truncate(cell.GetDouble());
            var s = asLong.ToString("0", CultureInfo.InvariantCulture);
            return s.Length < 11 ? s.PadLeft(11, '0') : s;
        }

        var txt = cell.GetString().Trim();
        if (txt.Length > 0 && txt.All(char.IsDigit) && txt.Length < 11)
            txt = txt.PadLeft(11, '0');

        return txt;
    }

    /// Walidacja PESEL 1:1 (checksum + data/stulecie)
    public static bool TryValidatePesel(string pesel, out string reason)
    {
        reason = string.Empty;
        if (pesel.Length != 11 || !pesel.All(char.IsDigit)) { reason = "musi mieć 11 cyfr"; return false; }

        int[] w = { 1, 3, 7, 9, 1, 3, 7, 9, 1, 3 };
        int sum = 0; for (int i = 0; i < 10; i++) sum += (pesel[i] - '0') * w[i];
        int check = (10 - (sum % 10)) % 10;
        if (check != (pesel[10] - '0')) { reason = "błędna suma kontrolna"; return false; }

        int year = (pesel[0] - '0') * 10 + (pesel[1] - '0');
        int month = (pesel[2] - '0') * 10 + (pesel[3] - '0');
        int day = (pesel[4] - '0') * 10 + (pesel[5] - '0');

        int century;
        if (month is >= 1 and <= 12) { century = 1900; }
        else if (month is >= 21 and <= 32) { century = 2000; month -= 20; }
        else if (month is >= 41 and <= 52) { century = 2100; month -= 40; }
        else if (month is >= 61 and <= 72) { century = 2200; month -= 60; }
        else if (month is >= 81 and <= 92) { century = 1800; month -= 80; }
        else { reason = "nieprawidłowy miesiąc w dacie"; return false; }

        try { _ = new DateTime(century + year, month, day); }
        catch { reason = "nieprawidłowa data urodzenia"; return false; }

        return true;
    }
}
