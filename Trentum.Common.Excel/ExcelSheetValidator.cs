using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;

namespace Trentum.Common.Excel;

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
    /// Waliduje XLSX, przycina do kolumn wymaganych i adnotuje STATUS WALIDACJI.
    /// Wymaga wartości we WSZYSTKICH kolumnach z <paramref name="requiredHeaders"/>.
    /// Opcjonalnie waliduje PESEL, duplikaty PESEL oraz zgodność Stopnia i Nazwy jednostki z listami referencyjnymi.
    /// Funkcja jest cyklicznie uruchamialna – czyści CF i starą kolumnę STATUS na wejściu.
    /// </summary>
    public static ExcelValidationSummary ValidateAndAnnotate(
        System.IO.Stream input,
        System.IO.Stream output,
        string? sheetName = null,
        string[]? requiredHeaders = null,
        string peselHeader = "PESEL",
        string nazwiskoHeader = "Nazwisko", // pozostawione dla kompatybilności API
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

        // ──────────────────────────────────────────────────────────────────────────
        // MINI-PATCH: wyczyść CF i usuń kolumnę STATUS WALIDACJI z poprzedniego cyklu
        // ──────────────────────────────────────────────────────────────────────────
        ws.ConditionalFormats.RemoveAll();

        var headers = ExcelCommon.BuildHeaderMap(ws, headerRow);
        if (headers.TryGetValue("STATUS WALIDACJI", out var prevStatusCol))
        {
            ws.Column(prevStatusCol).Delete();
            headers = ExcelCommon.BuildHeaderMap(ws, headerRow); // przebuduj po usunięciu
        }
        // ──────────────────────────────────────────────────────────────────────────

        // 1) Weryfikacja braków nagłówków
        var missing = requiredHeaders.Where(h => !headers.ContainsKey(h)).ToList();

        if (missing.Count > 0)
        {
            // Brakuje nagłówków – pokaż informację i zakończ (STATUS dodany jednorazowo)
            var lastColNow = ws.LastColumnUsed()?.ColumnNumber() ?? headers.Values.DefaultIfEmpty(1).Max();
            var statusColMissing = lastColNow + 1;

            ws.Cell(headerRow, statusColMissing).Value = "STATUS WALIDACJI";
            ws.Cell(headerRow, statusColMissing).Style.Font.Bold = true;

            var infoCell = ws.Cell(headerRow, statusColMissing + 1);
            infoCell.Value = $"Brak nagłówków: {string.Join(", ", missing)}";
            infoCell.Style.Font.Bold = true;
            infoCell.Style.Font.FontColor = XLColor.White;
            infoCell.Style.Fill.BackgroundColor = XLColor.Red;

            FinalizeSheet(ws, headerRow, statusColMissing);
            wb.SaveAs(output); output.Position = 0;

            return new ExcelValidationSummary(headerRow, 0, 0, 0, missing, new Dictionary<string, int>());
        }

        // 2) PRZYCIĘCIE: usuń wszystkie kolumny poza wymaganymi
        var keepCols = new HashSet<int>();
        foreach (var name in requiredHeaders)
        {
            if (ExcelCommon.TryResolveColumn(headers, name, out var col))
                keepCols.Add(col);
        }

        var lastUsedColBeforeTrim = ws.LastColumnUsed()?.ColumnNumber() ?? headers.Values.DefaultIfEmpty(1).Max();
        for (int c = lastUsedColBeforeTrim; c >= 1; c--)
        {
            if (!keepCols.Contains(c))
                ws.Column(c).Delete();
        }

        // 3) Po przycięciu odbuduj mapę i JEDNORAZOWO dodaj kolumnę statusu
        headers = ExcelCommon.BuildHeaderMap(ws, headerRow);
        var lastDataCol = ws.LastColumnUsed()?.ColumnNumber() ?? headers.Values.DefaultIfEmpty(1).Max();
        var statusCol = lastDataCol + 1;

        ws.Cell(headerRow, statusCol).Value = "STATUS WALIDACJI";
        ws.Cell(headerRow, statusCol).Style.Font.Bold = true;

        // 4) Mapy kolumn wymaganych (po przycięciu)
        var requiredCols = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in requiredHeaders)
        {
            if (!ExcelCommon.TryResolveColumn(headers, name, out var col))
                throw new InvalidOperationException($"Nieoczekiwany błąd: brak kolumny po przycięciu: {name}");
            requiredCols[name] = col;
        }

        // Specyficzne kolumny
        if (!ExcelCommon.TryResolveColumn(headers, peselHeader, out var peselCol))
            throw new InvalidOperationException("Brak kolumny PESEL po przycięciu.");

        int? stopienCol = ExcelCommon.TryResolveColumn(headers, "Stopień", out var cStopien) ? cStopien : (int?)null;
        int? unitCol = ExcelCommon.TryResolveColumn(headers, "Nazwa jednostki wojskowej", out var cUnit) ? cUnit : (int?)null;

        // 5) Jeżeli brak wierszy danych – zapis i wyjście
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow;
        if (lastRow <= headerRow)
        {
            FinalizeSheet(ws, headerRow, statusCol);
            wb.SaveAs(output); output.Position = 0;
            return new ExcelValidationSummary(headerRow, 0, 0, 0, Array.Empty<string>(), new Dictionary<string, int>());
        }

        // 6) Zbiory referencyjne (case-insensitive, po Trim). Włączone tylko jeśli jest lista i flaga true.
        HashSet<string>? rankSet = (validateRank && rankReferenceList is not null)
            ? new HashSet<string>(rankReferenceList.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()),
                                  StringComparer.OrdinalIgnoreCase)
            : null;

        HashSet<string>? unitSet = (validateUnitName && unitNameReferenceList is not null)
            ? new HashSet<string>(unitNameReferenceList.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()),
                                  StringComparer.OrdinalIgnoreCase)
            : null;

        // 7) Pierwszy przebieg – liczymy wiersze danych + PESEL-e (na potrzeby duplikatów)
        Dictionary<string, int>? peselCounts = validatePeselDuplicates
            ? new Dictionary<string, int>(StringComparer.Ordinal)
            : null;

        int dataRowCount = 0;

        for (int r = headerRow + 1; r <= lastRow; r++)
        {
            if (ExcelCommon.IsRowEmpty(ws.Row(r), lastDataCol)) continue;
            dataRowCount++;

            if (validatePeselDuplicates)
            {
                var pesel = ExcelCommon.GetPesel(ws.Cell(r, peselCol));
                if (pesel.Length > 0)
                    peselCounts![pesel] = peselCounts.TryGetValue(pesel, out var cnt) ? cnt + 1 : 1;
            }
        }

        var duplicatedPesels = (validatePeselDuplicates && peselCounts!.Count > 0)
            ? peselCounts.Where(kv => kv.Value > 1)
                         .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal)
            : new Dictionary<string, int>(StringComparer.Ordinal);

        // 8) Drugi przebieg – walidacja wierszy i nadawanie statusów
        int valid = 0, errors = 0;

        for (int r = headerRow + 1; r <= lastRow; r++)
        {
            if (ExcelCommon.IsRowEmpty(ws.Row(r), lastDataCol)) continue;

            var rowErrors = new List<string>();

            // WYMAGANE wartości we wszystkich wymaganych kolumnach
            foreach (var kv in requiredCols)
            {
                var name = kv.Key;
                var col = kv.Value;
                var val = ExcelCommon.GetTrimmed(ws.Cell(r, col));
                if (string.IsNullOrWhiteSpace(val))
                    rowErrors.Add($"Brak wartości w kolumnie '{name}'");
            }

            // Dodatkowa walidacja PESEL (jeśli włączona i wpisany)
            var peselVal = ExcelCommon.GetPesel(ws.Cell(r, peselCol));
            if (validatePesel && !string.IsNullOrWhiteSpace(peselVal))
            {
                if (!ExcelCommon.TryValidatePesel(peselVal, out var peselReason))
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
                var stopienVal = ExcelCommon.GetTrimmed(ws.Cell(r, stopienCol.Value));
                if (!string.IsNullOrWhiteSpace(stopienVal) && !rankSet.Contains(stopienVal))
                    rowErrors.Add("Nieznany 'Stopień'");
            }

            // Walidacja referencyjna NAZWA JEDNOSTKI
            if (validateUnitName && unitSet is not null && unitCol is not null)
            {
                var unitVal = ExcelCommon.GetTrimmed(ws.Cell(r, unitCol.Value));
                if (!string.IsNullOrWhiteSpace(unitVal) && !unitSet.Contains(unitVal))
                    rowErrors.Add("Nieznana 'Nazwa jednostki wojskowej'");
            }

            ws.Cell(r, statusCol).Value = rowErrors.Count == 0 ? "POPRAWNY" : string.Join("; ", rowErrors);
            if (rowErrors.Count == 0) valid++; else errors++;
        }

        // 9) Formatowanie warunkowe (NoColor dla „POPRAWNY”, czerwone dla błędnych)
        ApplyConditionalFormatting(ws, headerRow, statusCol, lastRow);
        FinalizeSheet(ws, headerRow, statusCol);

        wb.SaveAs(output); output.Position = 0;

        return new ExcelValidationSummary(headerRow, dataRowCount, valid, errors,
            Array.Empty<string>(), duplicatedPesels);
    }

    // --- helpers ---

    private static void ApplyConditionalFormatting(IXLWorksheet ws, int headerRow, int statusCol, int lastRow)
    {
        if (lastRow <= headerRow) return;

        string statusLetter = ws.Cell(headerRow, statusCol).Address.ColumnLetter;
        int firstDataRow = headerRow + 1;

        // Zakresy
        var rowsRange = ws.Range(firstDataRow, 1, lastRow, statusCol);
        var statusRange = ws.Range(firstDataRow, statusCol, lastRow, statusCol);

        // Reset bezpośrednich teł — żeby kolejne przebiegi zawsze startowały „na czysto”
        rowsRange.Style.Fill.SetBackgroundColor(XLColor.NoColor);
        statusRange.Style.Fill.SetBackgroundColor(XLColor.NoColor);

        // --- Wiersze BŁĘDNE: różowe tło ---
        var cfBadRows = rowsRange
            .AddConditionalFormat()
            .WhenIsTrue($"${statusLetter}{firstDataRow}<>\"POPRAWNY\"");
        cfBadRows.Fill.SetBackgroundColor(XLColor.LightPink);

        // --- Kolumna STATUS: BŁĘDY na czerwono + biała pogrubiona czcionka ---
        var cfStatusBad = statusRange
            .AddConditionalFormat()
            .WhenIsTrue($"${statusLetter}{firstDataRow}<>\"POPRAWNY\"");
        cfStatusBad.Fill.SetBackgroundColor(XLColor.Red);
        cfStatusBad.Font.SetFontColor(XLColor.White);
        cfStatusBad.Font.SetBold();

        // Nie dodajemy reguł „POPRAWNY” — zostaje NoColor (czytelne i niezawodne).
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
}
