using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;


public static class DataTableHelpers
{
    public static string[] GetStrings(
        DataTable table,
        string columnName,
        bool distinct = false,
        bool ignoreNullOrWhiteSpace = true)
    {
        if (table is null) throw new ArgumentNullException(nameof(table));
        if (!table.Columns.Contains(columnName))
            throw new ArgumentException($"Brak kolumny '{columnName}'.", nameof(columnName));
        if (table.Columns[columnName].DataType != typeof(string))
            throw new ArgumentException($"Kolumna '{columnName}' nie jest typu string.", nameof(columnName));

        IEnumerable<string> q = table.Rows
           .Cast<DataRow>()
           .Select(r => (r[columnName] as string)?.Trim());

        if (ignoreNullOrWhiteSpace)
            q = q.Where(s => !string.IsNullOrWhiteSpace(s));

        if (distinct)
            q = q.Distinct(StringComparer.Ordinal);

        return q.ToArray();
    }
}
