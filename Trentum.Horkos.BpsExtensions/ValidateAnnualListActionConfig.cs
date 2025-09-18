using WebCon.WorkFlow.SDK.Common;
using WebCon.WorkFlow.SDK.ConfigAttributes;

namespace Trentum.Horkos.BpsExtensions
{
    public class ValidateAnnualListActionConfig : PluginConfiguration
    {
        [ConfigGroupBox(DisplayName = "Zakres walidacji listy", Order = 1)]
        public ValidationConditions ValidationConditionsGroupBox { get; set; }

        [ConfigGroupBox(DisplayName = "Źródła danych do walidacji listy", Order = 3)]
        public ValidationDataSources ValidationDataSourcesGroupBox { get; set; }

        [ConfigGroupBox(DisplayName = "Wyniki walidacji listy", Order = 2)]
        public ValidationResults ValidationResultsGroupBox { get; set; }



    }


    public class ValidationConditions
    {
        [ConfigEditableFormFieldID(DisplayName = "Sprawdzaj PESEL", Description = "Zaznaczenie oznacza, że procedura będzie sprawdzała PESEL w rozszerzonym zakresie np. sumę kontrolną.", FormFieldTypes = FormFieldTypes.Boolean)]
        public int? ValidatePeselFormFieldId { get; set; }
        [ConfigEditableFormFieldID(DisplayName = "Wyszukuj duplikaty PESEL", Description = "Zaznaczenie oznacza, że procedura będzie wyszukiwała duplikaty PESEL.", FormFieldTypes = FormFieldTypes.Boolean)]
        public int? ValidatePeselDuplicatesFormFieldId { get; set; }
        [ConfigEditableFormFieldID(DisplayName = "Sprawdzaj stopień wojskowy", Description = "Zaznaczenie oznacza, że procedura będzie sprawdzała, czy podany stopień wojskowy znajduje się w systemowym słowniku stopni wojskowych.", FormFieldTypes = FormFieldTypes.Boolean)]
        public int? ValidateRankFormFieldId { get; set; }
        [ConfigEditableFormFieldID(DisplayName = "Sprawdzaj nazwę jednostki wojskowej", Description = "Zaznaczenie oznacza, że procedura będzie sprawdzała, czy podana nazwa jednostki wojskowej znajduje się w systemowym słowniku jednostek wojskowych.", FormFieldTypes = FormFieldTypes.Boolean)]
        public int? ValidateUnitNameFormFieldId { get; set; }

        [ConfigEditableConnectionID(DisplayName = "Połączenie do listy rocznej", Description = "Wybierz połączenie do źródła odniesienia dla stopni i jednostek wojskowych.", ConnectionsType = DataConnectionType.MSSQL)]
        public int? ReferenceListConnectionId { get; set; }


    }

    public class ValidationDataSources
    {
        [ConfigEditableDataSourceID(DisplayName = "Słownik stopni wojskowych", Description = "Słownik stopni wojskowych, na jego podstawie walidowane są stopnie wojskowe osób z listy.")]
        public int? RankDataSourceID { get; set; }
        
        [ConfigEditableDataSourceID(DisplayName = "Słownik jednostek wojskowych", Description = "Słownik jednostek wojskowych, na jego podstawie walidowane są nazwy jednostek wojskowych - miejsc służby osób z listy.")]
        public int? UnitDataSourceID { get; set; }
        
    }

    public class ValidationResults
    {
        [ConfigEditableFormFieldID(DisplayName = "Liczba wszystkich wierszy.", Description = "Pole z informacją o liczbie wszystkich wierszy.")]
        public int? AllRowsCountFormFieldID { get; set; }
        [ConfigEditableFormFieldID(DisplayName = "Liczba prawidłowych wierszy.", Description = "Pole z informacją o liczbie prawidłowych wierszy.",FormFieldTypes = FormFieldTypes.Integer)]
        public int? ValidRowsCountFormFieldID { get; set; }
        [ConfigEditableFormFieldID(DisplayName = "Liczba duplikatów PESEL.", Description = "Pole z informacją o liczbie duplikatów PESEL.", FormFieldTypes = FormFieldTypes.Integer)]
        public int? PeselDuplicatesCountFormFieldID { get; set; }
        [ConfigEditableFormFieldID(DisplayName = "Liczba błędnych wierszy.", Description = "Pole z informacją o liczbie wierszy z błędami.", FormFieldTypes = FormFieldTypes.Integer)]
        public int? ErrorRowsCountFormFieldID { get; set; }
        [ConfigEditableFormFieldID(DisplayName = "Brakujące kolumny.", Description = "Pole z informacją o brakujących kolumnach.", FormFieldTypes = FormFieldTypes.TextSingleLine)]
        public int? MissingHeadersFormFieldID { get; set; }

    }
}