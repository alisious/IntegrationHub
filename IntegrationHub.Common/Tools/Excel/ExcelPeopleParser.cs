using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace IntegrationHub.Common.Tools.Excel;

public static class ExcelPeopleParser
{
    public sealed record Osoba(
        string Stopien,
        string ImiePierwsze,
        string ImieDrugie,
        string Nazwisko,
        string PESEL,
        string Stanowisko,
        string NrEtatu,
        string NazwaJednostkiWojskowej
    );

    /// Oczekiwane kolumny: Stopień, Imiona, Nazwisko, PESEL, Stanowisko, Nr etatu, Nazwa jednostki wojskowej
    public static List<Osoba> ParseFromExcel(byte[] excelContent, string? sheetName = null, int headerRow = 1)
    {
        using var ms = new MemoryStream(excelContent, writable: false);
        using var wb = new XLWorkbook(ms);
        var ws = sheetName is null ? wb.Worksheet(1) : wb.Worksheet(sheetName);

        var headers = ExcelCommon.BuildHeaderMap(ws, headerRow);

        // Resolve wszystkich wymaganych kolumn (z aliasami)
        int cStopien = Require(headers, "Stopień");
        int cImiona = Require(headers, "Imiona");
        int cNazw = Require(headers, "Nazwisko");
        int cPesel = Require(headers, "PESEL");
        int cStan = Require(headers, "Stanowisko");
        int cNrEt = Require(headers, "Nr etatu");
        int cNJW = Require(headers, "Nazwa jednostki wojskowej");

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? headerRow;
        var lastDataCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

        var result = new List<Osoba>();

        for (int r = headerRow + 1; r <= lastRow; r++)
        {
            var row = ws.Row(r);
            if (ExcelCommon.IsRowEmpty(row, lastDataCol)) continue;

            string stopien = ExcelCommon.GetTrimmed(row.Cell(cStopien));
            string imionaRaw = ExcelCommon.GetTrimmed(row.Cell(cImiona));
            string nazwisko = ExcelCommon.GetTrimmed(row.Cell(cNazw));
            string pesel = ExcelCommon.GetPesel(row.Cell(cPesel)); // zachowuje leading zeros
            string stanowisko = ExcelCommon.GetTrimmed(row.Cell(cStan));
            string nrEtatu = ExcelCommon.GetTrimmed(row.Cell(cNrEt));
            string njw = ExcelCommon.GetTrimmed(row.Cell(cNJW));

            var (first, second) = SplitImiona(imionaRaw);

            // Pomijamy „puste” rekordy bez sensownych danych
            if (string.IsNullOrWhiteSpace(nazwisko) && string.IsNullOrWhiteSpace(pesel))
                continue;

            result.Add(new Osoba(
                Stopien: stopien,
                ImiePierwsze: first,
                ImieDrugie: second,
                Nazwisko: nazwisko,
                PESEL: pesel,
                Stanowisko: stanowisko,
                NrEtatu: nrEtatu,
                NazwaJednostkiWojskowej: njw
            ));
        }

        return result;
    }

    private static (string first, string second) SplitImiona(string imiona)
    {
        if (string.IsNullOrWhiteSpace(imiona)) return ("", "");
        var parts = imiona.Trim()
                  .Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries); if (parts.Length == 1) return (parts[0], "");
        return (parts[0], parts[1]);
    }

    private static int Require(Dictionary<string, int> headers, string canonical)
    {
        if (!ExcelCommon.TryResolveColumn(headers, canonical, out var col))
            throw new InvalidDataException($"Brakuje wymaganej kolumny: {canonical}");
        return col;
    }
}
