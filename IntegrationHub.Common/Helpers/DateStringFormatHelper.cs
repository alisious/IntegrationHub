using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.Common.Helpers
{
    public static partial class DateStringFormatHelper
    {
        public static string? FormatYyyyMmDd(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var s = raw.Trim();
            if (YyyyMmDdRegex().IsMatch(s)) return s; // już yyyyMMdd
            if (DateTime.TryParse(s, out var dt)) return dt.ToString("yyyyMMdd");
            return null;
        }

        [System.Text.RegularExpressions.GeneratedRegex("^d{8}$")]
        private static partial System.Text.RegularExpressions.Regex YyyyMmDdRegex();
    }
}
