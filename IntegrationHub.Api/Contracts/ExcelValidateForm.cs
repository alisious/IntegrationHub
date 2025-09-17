using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace IntegrationHub.Api.Contracts.Excel;

public sealed class ExcelValidateForm
{
    /// <summary>Plik XLSX do walidacji.</summary>
    [Required]
    public IFormFile File { get; set; } = default!;

    /// <summary>Opcjonalna nazwa arkusza (gdy brak – pierwszy arkusz).</summary>
    public string? SheetName { get; set; }

    /// <summary>Numer wiersza nagłówków (domyślnie 1).</summary>
    public int? HeaderRow { get; set; } = 1;

    /// <summary>Wymagane nagłówki (multi-value). Jeśli puste – użyte będą domyślne.</summary>
    public string[]? RequiredHeaders { get; set; }

    /// <summary>Włączyć walidację PESEL (format, data, checksum)? Domyślnie: true.</summary>
    public bool? ValidatePesel { get; set; }

    /// <summary>Włączyć sprawdzanie duplikatów PESEL? Domyślnie: true.</summary>
    public bool? ValidatePeselDuplicates { get; set; }

    /// <summary>Włączyć walidację kolumny 'Nazwa jednostki wojskowej' względem listy referencyjnej? Domyślnie: false.</summary>
    public bool? ValidateUnitName { get; set; }

    /// <summary>Lista referencyjna dopuszczalnych nazw jednostek (multi-value).</summary>
    public string[]? UnitNameReferenceList { get; set; }

    /// <summary>Włączyć walidację kolumny 'Stopień' względem listy referencyjnej? Domyślnie: false.</summary>
    public bool? ValidateRank { get; set; }

    /// <summary>Lista referencyjna dopuszczalnych stopni (multi-value).</summary>
    public string[]? RankReferenceList { get; set; }
}
