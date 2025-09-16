using System.Net;
using System.Text.Json;
using IntegrationHub.Common.Contracts;
using IntegrationHub.Common.Helpers;
using IntegrationHub.SRP.Contracts;

namespace IntegrationHub.SRP.Services;

internal static class SrpServiceCommon
{
    // Jedne opcje JSON dla całego projektu (deserializacja plików testowych)
    internal static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    // Normalizacja do porównań tekstowych
    internal static string N(string? s) => (s ?? string.Empty).Trim().ToUpperInvariant();

    /// <summary>
    /// Sprawdza: PESEL lub (Nazwisko+Imię). Normalizuje daty do yyyyMMdd.
    /// Jeśli <paramref name="allowRange"/> = true, normalizuje też DataUrodzeniaOd/Do.
    /// W razie błędu zwraca przygotowany ProxyResponse&lt;SearchPersonResponse&gt;.
    /// </summary>
    internal static bool TryValidateAndNormalize(
        SearchPersonRequest body,
        string requestId,
        bool allowRange,
        out ProxyResponse<SearchPersonResponse>? error)
    {
        error = null;

        var hasPesel = !string.IsNullOrWhiteSpace(body.Pesel);
        var hasNamePair = !string.IsNullOrWhiteSpace(body.Nazwisko)
                       && !string.IsNullOrWhiteSpace(body.ImiePierwsze);

        if (!hasPesel && !hasNamePair)
        {
            error = Error<SearchPersonResponse>(requestId, HttpStatusCode.BadRequest,
                ProxyStatus.BusinessError, "Obowiazkowo podaj PESEL albo zestaw: nazwisko i imie.");
            return false;
        }

        // DataUrodzenia (dokładna)
        if (!string.IsNullOrWhiteSpace(body.DataUrodzenia))
        {
            var formatted = DateStringFormatHelper.FormatYyyyMmDd(body.DataUrodzenia);
            if (formatted is null)
            {
                error = Error<SearchPersonResponse>(requestId, HttpStatusCode.BadRequest,
                    ProxyStatus.BusinessError, "Niepoprawny format parametru dataUrodzenia. Wymagany format: yyyyMMdd lub yyyy-MM-dd.");
                return false;
            }
            body.DataUrodzenia = formatted;
        }

        if (allowRange)
        {
            // DataUrodzeniaOd
            if (!string.IsNullOrWhiteSpace(body.DataUrodzeniaOd))
            {
                var formatted = DateStringFormatHelper.FormatYyyyMmDd(body.DataUrodzeniaOd);
                if (formatted is null)
                {
                    error = Error<SearchPersonResponse>(requestId, HttpStatusCode.BadRequest,
                        ProxyStatus.BusinessError, "Niepoprawny format parametru dataUrodzeniaOd. Wymagany format: yyyyMMdd lub yyyy-MM-dd.");
                    return false;
                }
                body.DataUrodzeniaOd = formatted;
            }

            // DataUrodzeniaDo
            if (!string.IsNullOrWhiteSpace(body.DataUrodzeniaDo))
            {
                var formatted = DateStringFormatHelper.FormatYyyyMmDd(body.DataUrodzeniaDo);
                if (formatted is null)
                {
                    error = Error<SearchPersonResponse>(requestId, HttpStatusCode.BadRequest,
                        ProxyStatus.BusinessError, "Niepoprawny format parametru dataUrodzeniaDo. Wymagany format: yyyyMMdd lub yyyy-MM-dd.");
                    return false;
                }
                body.DataUrodzeniaDo = formatted;
            }
        }

        return true;
    }

    // Jedna fabryka błędów dla całego serwisu
    internal static ProxyResponse<T> Error<T>(
        string requestId, HttpStatusCode code, ProxyStatus status, string message) =>
        new()
        {
            RequestId = requestId,
            Source = "SRP",
            Status = status,
            SourceStatusCode = (int)code,
            ErrorMessage = message
        };
}
