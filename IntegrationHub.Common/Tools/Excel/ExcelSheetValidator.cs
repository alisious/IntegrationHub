using ClosedXML.Excel;

namespace IntegrationHub.Common.Tools.Excel;

public sealed record ExcelValidationSummary(
    int HeaderRow,
    int DataRowCount,
    int ValidRows,
    int ErrorRows,
    IReadOnlyList<string> MissingHeaders,
    IReadOnlyDictionary<string, int> PeselDuplicatesCount
);

public static class ExcelSheetValidator
{
    /// <summary>
    /// Waliduje XLSX i adnotuje STATUS WALIDACJI.
    /// Wymaga wartości we WSZYSTKICH kolumnach z <paramref name="requiredHeaders"/>.
    /// Dodatkowo może walidować PESEL, duplikaty PESEL oraz zgodność Stopnia i Nazwy jednostki z listami referencyjnymi.
    /// </summary>
    public static ExcelValidationSummary ValidateAndAnnotate(
        Stream input,
        Stream output,
        string? sheetName = null,
        string[]? requiredHeaders = null,
        string peselHeader = "PESEL",
        string nazwiskoHeader = "Nazwisko",
        int headerRow = 1,
        bool validatePesel = true,
        bool validatePeselDuplicates = true,
        bool validateUnitName = false,
        bool validateRank = false,
        IEnumerable<string>? unitNameReferenceList = null,
        IEnumerable<string>? rankReferenceList = null)
    {
        // Domyślne wymagane kolumny – WSZYSTKIE muszą mieć wartość
        requiredHeaders ??= new[]
        {
            "Stopień", "Imiona", "Nazwisko", "PESEL",
            "Stanowisko", "Nr etatu", "Nazwa jednostki wojskowej"
        };

        using var wb = new XLWorkbook(input);
        var ws = sheetName is null ? wb.Worksheet(1) : wb.Worksheet(sheetName);

        var headers = BuildHeaderMap(ws, headerRow);
        var missing = requiredHeaders.Where(h => !headers.ContainsKey(h)).ToList();

        var lastDataCol = ws.LastColumnUsed()?.ColumnNumber() ?? headers.Values.DefaultIfEmpty(1).Max();
        var statusCol = lastDataCol + 1;

        ws.Cell(headerRow, statusCol).Value = "STATUS WALIDACJI";
        ws.Cell(headerRow, statusCol).Style.Font.Bold = true;

        if (missing.Count > 0)
        {
            var infoCell = ws.Cell(headerRow, statusCol + 1);
            infoCell.Value = $"Brak nagłówków: {string.Join(", ", missing)}";
            infoCell.Style.Font.Bold = true;
            infoCell.Style.Font.FontColor = XLColor.White;
            infoCell.Style.Fill.BackgroundColor = XLColor.Red;

            FinalizeSheet(ws, headerRow, statusCol);
            wb.SaveAs(output); output.Position = 0;

            return new ExcelValidationSummary(headerRow, 0, 0, 0, missing, new Dictionary<string, int>());
        }

        // Mapy kolumn wymaganych
        var requiredCols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in requiredHeaders)
            requiredCols[name] = headers[name];

        // Specyficzne kolumny
        var peselCol = headers[peselHeader];
        var stopienCol = headers.ContainsKey("Stopień") ? headers["Stopień"] : (int?)null;
        var unitCol = headers.ContainsKey("Nazwa jednostki wojskowej") ? headers["Nazwa jednostki wojskowej"] : (int?)null;

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow;
        if (lastRow <= headerRow)
        {
            FinalizeSheet(ws, headerRow, statusCol);
            wb.SaveAs(output); output.Position = 0;
            return new ExcelValidationSummary(headerRow, 0, 0, 0, Array.Empty<string>(), new Dictionary<string, int>());
        }

        // Zbiory referencyjne (case-insensitive, po Trim). Włączone tylko jeśli jest lista i flaga true.
        HashSet<string>? rankSet = (validateRank && rankReferenceList is not null)
            ? new HashSet<string>(rankReferenceList.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()),
                                  StringComparer.OrdinalIgnoreCase)
            : null;

        HashSet<string>? unitSet = (validateUnitName && unitNameReferenceList is not null)
            ? new HashSet<string>(unitNameReferenceList.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()),
                                  StringComparer.OrdinalIgnoreCase)
            : null;

        // 1) Pierwszy przebieg – liczymy wiersze danych + PESEL-e (jeśli trzeba szukać duplikatów)
        Dictionary<string, int>? peselCounts = validatePeselDuplicates
            ? new Dictionary<string, int>(StringComparer.Ordinal)
            : null;

        int dataRowCount = 0;

        for (int r = headerRow + 1; r <= lastRow; r++)
        {
            if (IsRowEmpty(ws.Row(r), lastDataCol)) continue;
            dataRowCount++;

            if (validatePeselDuplicates)
            {
                var pesel = GetTrimmed(ws.Cell(r, peselCol));
                if (pesel.Length > 0)
                    peselCounts![pesel] = peselCounts.TryGetValue(pesel, out var cnt) ? cnt + 1 : 1;
            }
        }

        var duplicatedPesels = (validatePeselDuplicates && peselCounts!.Count > 0)
            ? peselCounts.Where(kv => kv.Value > 1)
                         .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal)
            : new Dictionary<string, int>(StringComparer.Ordinal);

        // 2) Walidacja wierszy
        int valid = 0, errors = 0;

        for (int r = headerRow + 1; r <= lastRow; r++)
        {
            if (IsRowEmpty(ws.Row(r), lastDataCol)) continue;

            var rowErrors = new List<string>();

            // WYMAGANE wartości we wszystkich wymaganych kolumnach
            foreach (var kv in requiredCols)
            {
                var name = kv.Key;
                var col = kv.Value;
                var val = GetTrimmed(ws.Cell(r, col));
                if (string.IsNullOrWhiteSpace(val))
                    rowErrors.Add($"Brak wartości w kolumnie '{name}'");
            }

            // Dodatkowa walidacja PESEL (jeśli włączona i wpisany)
            var peselVal = GetTrimmed(ws.Cell(r, peselCol));
            if (validatePesel && !string.IsNullOrWhiteSpace(peselVal))
            {
                if (!TryValidatePesel(peselVal, out var peselReason))
                    rowErrors.Add($"PESEL niepoprawny: {peselReason}");
            }

            // Duplikaty PESEL (jeśli włączone i wpisany)
            if (validatePeselDuplicates && !string.IsNullOrWhiteSpace(peselVal))
            {
                if (duplicatedPesels.ContainsKey(peselVal))
                    rowErrors.Add("Zduplikowany PESEL");
            }

            // Walidacja referencyjna STOPIEŃ
            if (validateRank && rankSet is not null && stopienCol is not null)
            {
                var stopienVal = GetTrimmed(ws.Cell(r, stopienCol.Value));
                if (!string.IsNullOrWhiteSpace(stopienVal) && !rankSet.Contains(stopienVal))
                    rowErrors.Add($"Nieznany 'Stopień'");
            }

            // Walidacja referencyjna NAZWA JEDNOSTKI
            if (validateUnitName && unitSet is not null && unitCol is not null)
            {
                var unitVal = GetTrimmed(ws.Cell(r, unitCol.Value));
                if (!string.IsNullOrWhiteSpace(unitVal) && !unitSet.Contains(unitVal))
                    rowErrors.Add($"Nieznana 'Nazwa jednostki wojskowej'");
            }

            ws.Cell(r, statusCol).Value = rowErrors.Count == 0 ? "POPRAWNY" : string.Join("; ", rowErrors);
            if (rowErrors.Count == 0) valid++; else errors++;
        }

        ApplyConditionalFormatting(ws, headerRow, statusCol, lastRow);
        FinalizeSheet(ws, headerRow, statusCol);

        wb.SaveAs(output); output.Position = 0;

        return new ExcelValidationSummary(headerRow, dataRowCount, valid, errors,
            Array.Empty<string>(), duplicatedPesels);
    }

    
    // --- helpers ---

    private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet ws, int headerRow)
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

    private static bool IsRowEmpty(IXLRow row, int lastDataCol)
    {
        for (int c = 1; c <= lastDataCol; c++)
            if (!string.IsNullOrWhiteSpace(row.Cell(c).GetString()))
                return false;
        return true;
    }

    private static string GetTrimmed(IXLCell cell)
        => cell.IsEmpty() ? "" : cell.GetString().Trim();

    private static void ApplyConditionalFormatting(IXLWorksheet ws, int headerRow, int statusCol, int lastRow)
    {
        if (lastRow <= headerRow) return;

        string statusLetter = ws.Cell(headerRow, statusCol).Address.ColumnLetter;
        int firstDataRow = headerRow + 1;

        var rowsRange = ws.Range(firstDataRow, 1, lastRow, statusCol);
        rowsRange.AddConditionalFormat()
                 .WhenIsTrue($"${statusLetter}{firstDataRow}<>\"POPRAWNY\"")
                 .Fill.SetBackgroundColor(XLColor.LightPink);

        var statusRange = ws.Range(firstDataRow, statusCol, lastRow, statusCol);
        statusRange.AddConditionalFormat()
                   .WhenIsTrue($"${statusLetter}{firstDataRow}<>\"POPRAWNY\"")
                   .Fill.SetBackgroundColor(XLColor.Red)
                   .Font.SetFontColor(XLColor.White)
                   .Font.SetBold();
    }

    private static void FinalizeSheet(IXLWorksheet ws, int headerRow, int statusCol)
    {
        ws.Row(headerRow).Style.Font.Bold = true;
        ws.SheetView.FreezeRows(headerRow);

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow;
        if (lastRow > headerRow)
            ws.Range(headerRow, 1, lastRow, statusCol).SetAutoFilter();

        ws.Columns(1, statusCol).AdjustToContents();
    }

    // PESEL: 11 cyfr, checksum + poprawna data YYMMDD z zakodowanym stuleciem
    private static bool TryValidatePesel(string pesel, out string reason)
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
