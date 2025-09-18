using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions; // 👈
using Trentum.Common.Excel;
using WebCon.WorkFlow.SDK.ActionPlugins;
using WebCon.WorkFlow.SDK.ActionPlugins.Model;
using WebCon.WorkFlow.SDK.Tools.Data;
using WebCon.WorkFlow.SDK.Tools.Data.Model;

namespace Trentum.Horkos.BpsExtensions
{
    public class ValidateAnnualListAction : CustomAction<ValidateAnnualListActionConfig>
    {
        public override async Task RunAsync(RunCustomActionParams args)
        {
            bool _validatePesel = ActionParamsHelpers.GetParamBoolOrDefaultValue(args, Configuration.ValidationConditionsGroupBox.ValidatePeselFormFieldId);
            bool _validatePeselDuplicates = ActionParamsHelpers.GetParamBoolOrDefaultValue(args, Configuration.ValidationConditionsGroupBox.ValidatePeselDuplicatesFormFieldId);
            bool _validateRank = ActionParamsHelpers.GetParamBoolOrDefaultValue(args, Configuration.ValidationConditionsGroupBox.ValidateRankFormFieldId);
            bool _validateUnitName = ActionParamsHelpers.GetParamBoolOrDefaultValue(args, Configuration.ValidationConditionsGroupBox.ValidateUnitNameFormFieldId);

            int? _referenceListConnectionId = Configuration.ValidationConditionsGroupBox.ReferenceListConnectionId;
            int? _rankDataSourceId = Configuration.ValidationDataSourcesGroupBox.RankDataSourceID;
            int? _unitDataSourceId = Configuration.ValidationDataSourcesGroupBox.UnitDataSourceID;

            var _log = new StringBuilder();
            _log.AppendLine("Walidacja rocznej listy osób zobowiązanych.");
            _log.AppendLine($"Zakres: duplikaty PESEL={_validatePeselDuplicates}, stopień={_validateRank}, jednostka={_validateUnitName}, PESEL={_validatePesel}");

            string[] unitRefList = null;
            string[] rankRefList = null;

            try
            {
                var atts = args.Context.CurrentDocument.Attachments;
                if (atts.Count == 0) throw new Exception("Nie znaleziono załącznika.");
                if (atts.Count > 1) throw new Exception("Dozwolony jest tylko 1 załącznik.");

                var attachment = atts[0];
                if (!attachment.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) &&
                    !attachment.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Załącznik nie jest arkuszem Excel (.xlsx)");

                _log.AppendLine($"Załącznik: {attachment.FileName}");

                // >>> ZAWSZE await na metodach async SDK
                var content = await attachment.GetContentAsync();
                _log.AppendLine($"Pobrano zawartość. Rozmiar: {content.Length} B");


                // >>> WSZYSTKIE I/O do zewnętrznych baz poza transakcją WEBCON
                if (_validateRank || _validateUnitName)
                {
                    if (_referenceListConnectionId.HasValue)
                    {
                        var connectionsHelper = new ConnectionsHelper(args.Context);
                        var refConn = new GetByConnectionParams(_referenceListConnectionId.Value);
                        var refConnStr = connectionsHelper.GetConnectionStringToDatabase(refConn);

                        // Wymuś brak auto-enlistu do ambient TX
                        if (!refConnStr.Contains("Enlist="))
                            refConnStr += ";Enlist=false";

                        _log.AppendLine($"Połączono z: {refConnStr}");

                        using (var suppress = new TransactionScope(
                            TransactionScopeOption.Suppress,
                            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                            TransactionScopeAsyncFlowOption.Enabled))
                        {

                            var dict = new HorkosDictionaryService(new SqlConnectionFactory(refConnStr));

                            if (_validateUnitName)
                            {
                                var fromDb = await dict.GetUnitNameReferenceListAsync(args.Context.CancellationToken);
                                unitRefList = fromDb.ToArray();
                                _log.AppendLine("Pobrano referencyjną listę jednostek.");
                            }

                            if (_validateRank)
                            {
                                var fromDb = await dict.GetRankReferenceListAsync(args.Context.CancellationToken);
                                rankRefList = fromDb.ToArray();
                                _log.AppendLine("Pobrano referencyjną listę stopni.");
                            }

                            suppress.Complete();
                        } // koniec using suppress
                    }
                    else
                    {
                        if (_validateRank && !_rankDataSourceId.HasValue)
                            throw new Exception("Brak ustawionego źródła danych dla stopni wojskowych.");

                        if (_validateUnitName && !_unitDataSourceId.HasValue)
                            throw new Exception("Brak ustawionego źródła danych dla jednostek wojskowych.");

                        var dsHelper = new DataSourcesHelper(args.Context);
                        if (_validateUnitName)
                        {
                            var dt = await dsHelper.GetDataTableFromDataSourceAsync(new GetDataTableFromDataSourceParams(_unitDataSourceId.Value));
                            unitRefList = DataTableHelpers.GetStrings(dt, "WFD_AttText1", distinct: false, ignoreNullOrWhiteSpace: true);
                            _log.AppendLine("Pobrano referencyjną listę jednostek.");
                        }

                        if (_validateRank)
                        {
                            var dt = await dsHelper.GetDataTableFromDataSourceAsync(new GetDataTableFromDataSourceParams(_rankDataSourceId.Value));
                            rankRefList = DataTableHelpers.GetStrings(dt, "WFD_AttText1", distinct: false, ignoreNullOrWhiteSpace: true);
                            _log.AppendLine("Pobrano referencyjną listę stopni.");
                        }
                    }
                }

                using (var inMs = new MemoryStream(content, writable: false))
                using (var outMs = new MemoryStream())
                {

                    var summary = ExcelSheetValidator.ValidateAndAnnotate(
                        input: inMs,
                        output: outMs,
                        sheetName: null,
                        requiredHeaders: null,
                        headerRow: 1,
                        validatePesel: _validatePesel,
                        validatePeselDuplicates: _validatePeselDuplicates,
                        validateUnitName: _validateUnitName,
                        validateRank: _validateRank,
                        unitNameReferenceList: unitRefList,
                        rankReferenceList: rankRefList
                    );

                    outMs.Position = 0;

                    // Usuń oryginał i dodaj wynik — ZAWSZE await
                    await args.Context.CurrentDocument.Attachments.RemoveAsync(attachment);
                    var wynikFileName = "Wynik_walidacji" + attachment.FileExtension;
                    await args.Context.CurrentDocument.Attachments.AddNewAsync(wynikFileName, outMs.ToArray());
                    _log.AppendLine($"Dodano wynik: {wynikFileName}");

                    // Zapisz wyniki do pól formularza
                    if (Configuration.ValidationResultsGroupBox.AllRowsCountFormFieldID.HasValue)
                        await args.Context.CurrentDocument.IntegerFields.GetByID(Configuration.ValidationResultsGroupBox.AllRowsCountFormFieldID.Value).SetValueAsync(summary.DataRowCount);
                    if (Configuration.ValidationResultsGroupBox.ValidRowsCountFormFieldID.HasValue)
                        await args.Context.CurrentDocument.IntegerFields.GetByID(Configuration.ValidationResultsGroupBox.ValidRowsCountFormFieldID.Value).SetValueAsync(summary.ValidRows);
                    if (Configuration.ValidationResultsGroupBox.PeselDuplicatesCountFormFieldID.HasValue)
                    {
                        var peselDupCount = summary.PeselDuplicatesCount.Sum(d=>d.Value);
                        await args.Context.CurrentDocument.IntegerFields.GetByID(Configuration.ValidationResultsGroupBox.PeselDuplicatesCountFormFieldID.Value).SetValueAsync(peselDupCount);
                    }
                    if (Configuration.ValidationResultsGroupBox.ErrorRowsCountFormFieldID.HasValue)
                        await args.Context.CurrentDocument.IntegerFields.GetByID(Configuration.ValidationResultsGroupBox.ErrorRowsCountFormFieldID.Value).SetValueAsync(summary.ErrorRows);
                    if (Configuration.ValidationResultsGroupBox.MissingHeadersFormFieldID.HasValue)
                        await args.Context.CurrentDocument.TextFields.GetByID(Configuration.ValidationResultsGroupBox.MissingHeadersFormFieldID.Value).SetValueAsync(string.Join(", ", summary.MissingHeaders));

                }

                

            }
            catch (Exception ex)
            {
                _log.AppendLine($"Błąd: {ex.Message}");
                args.Message = _log.ToString();
                args.Context.PluginLogger.AppendDebug(_log.ToString());
                throw; // pozwól BPS poprawnie zwinąć transakcję
            }
            finally
            {
                _log.AppendLine("Koniec walidacji.");
                args.Context.PluginLogger.AppendDebug(_log.ToString());
                args.LogMessage = _log.ToString();
            }
        }
    }
}
