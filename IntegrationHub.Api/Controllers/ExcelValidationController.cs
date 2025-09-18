using IntegrationHub.Api.Contracts.Excel;
using Trentum.Common.Excel;
using Trentum.Horkos;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace IntegrationHub.Api.Controllers;

[ApiController]
[Route("tools/excel")]
[ApiExplorerSettings(GroupName = "v1")]
public class ExcelValidationController : ControllerBase
{


    private readonly IHorkosDictionaryService _dict;

    public ExcelValidationController(IHorkosDictionaryService dict)
    {
        _dict = dict;
    }


    /// <summary>
    /// Waliduje XLSX (wymagane wartości we wszystkich kolumnach; opcjonalnie: PESEL, duplikaty PESEL,
    /// zgodność 'Stopień' i 'Nazwa jednostki wojskowej' z listami referencyjnymi).
    /// Zwraca ZIP: wynik.xlsx (z kolumną STATUS WALIDACJI) + summary.json.
    /// </summary>
    [HttpPost("validate")]
    [Consumes("multipart/form-data")]
    [Produces("application/zip")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(50_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 50_000_000)]
    [SwaggerOperation(
        Summary = "Walidacja arkusza Excel (PESEL/duplikaty/Stopień/Nazwa jednostki – sterowalne przełącznikami).",
        Description = "Zwraca ZIP: wynik.xlsx (kolumna STATUS WALIDACJI, podświetlenia) + summary.json (podsumowanie).",
        OperationId = "Tools_ValidateExcel",
        Tags = new[] { "Excel" }
    )]
    public async Task<IActionResult> Validate([FromForm] ExcelValidateForm form, CancellationToken ct)
    {
        if (form.File is null || form.File.Length == 0)
            return BadRequest("Brak pliku lub plik pusty.");

        // Jeśli włączona walidacja i NIE podano listy – pobierz z bazy
        string[]? rankRef = form.RankReferenceList;
        if ((form.ValidateRank ?? false) && (rankRef is null || rankRef.Length == 0))
        {
            var fromDb = await _dict.GetRankReferenceListAsync(ct);
            rankRef = fromDb.ToArray();
        }

        string[]? unitRef = form.UnitNameReferenceList;
        if ((form.ValidateUnitName ?? false) && (unitRef is null || unitRef.Length == 0))
        {
            var fromDb = await _dict.GetUnitNameReferenceListAsync(ct);
            unitRef = fromDb.ToArray();
        }


        await using var inMs = new MemoryStream();
        await form.File.CopyToAsync(inMs);
        inMs.Position = 0;

        await using var outMs = new MemoryStream();

        var summary = ExcelSheetValidator.ValidateAndAnnotate(
            input: inMs,
            output: outMs,
            sheetName: form.SheetName,
            requiredHeaders: (form.RequiredHeaders?.Length ?? 0) > 0 ? form.RequiredHeaders : null,
            headerRow: form.HeaderRow ?? 1,
            validatePesel: form.ValidatePesel ?? true,
            validatePeselDuplicates: form.ValidatePeselDuplicates ?? true,
            validateUnitName: form.ValidateUnitName ?? false,
            validateRank: form.ValidateRank ?? false,
            unitNameReferenceList: form.UnitNameReferenceList,
            rankReferenceList: form.RankReferenceList
        );

        outMs.Position = 0;

        await using var zipMs = new MemoryStream();
        using (var zip = new ZipArchive(zipMs, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entryXlsx = zip.CreateEntry("wynik.xlsx", CompressionLevel.Fastest);
            await using (var zs = entryXlsx.Open())
                await outMs.CopyToAsync(zs);

            var entryJson = zip.CreateEntry("summary.json", CompressionLevel.Fastest);
            await using (var js = entryJson.Open())
            {
                var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
                var buf = Encoding.UTF8.GetBytes(json);
                await js.WriteAsync(buf, 0, buf.Length);
            }
        }

        zipMs.Position = 0;
        var baseName = Path.GetFileNameWithoutExtension(form.File.FileName);
        if (string.IsNullOrWhiteSpace(baseName)) baseName = "walidacja";

        return File(zipMs.ToArray(), "application/zip", $"{baseName}_wynik.zip");
    }
}
