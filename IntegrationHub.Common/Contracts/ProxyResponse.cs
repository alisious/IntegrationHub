namespace IntegrationHub.Common.Contracts;
/// <summary>
/// Uniwersalna odpowiedź zwracana przez API-proxy. 
/// Zawiera dane biznesowe, status wywołania (sukces, błąd biznesowy, błąd techniczny),
/// szczegóły błędu, źródło danych oraz identyfikator żądania RequestId.
/// </summary>
/// <typeparam name="T">Typ danych zwracanych z wywołania API.</typeparam>
public class ProxyResponse<T>
{
    /// <summary>
    /// Dane zwrócone z systemu zewnętrznego (lub null, jeśli wystąpił błąd).
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Status odpowiedzi – czy operacja zakończyła się sukcesem, błędem biznesowym czy technicznym.
    /// </summary>
    public ProxyStatus Status { get; set; }

    /// <summary>
    /// Szczegóły błędu – komunikat z systemu zewnętrznego lub własny opis.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Nazwa/identyfikator źródła danych (np. SRP, CEK, KSIP).
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Kod statusu zwrócony przez system zewnętrzny (np. HTTP status code, kod błędu domenowego).
    /// </summary>
    public int? SourceStatusCode { get; set; }

    /// <summary>
    /// Unikalny identyfikator żądania, pozwalający śledzić przebieg requestu w logach i systemach integracyjnych.
    /// </summary>
    public string? RequestId { get; set; }
}

/// <summary>
/// Status wywołania API-proxy:
/// Success – sukces, 
/// BusinessError – błąd biznesowy (np. brak uprawnień, walidacja, nie znaleziono danych, znaleziono zbyt dużo danych),
/// TechnicalError – błąd techniczny (np. problem z połączeniem, timeout, wyjątek serwera).
/// </summary>
public enum ProxyStatus
{
    Success,
    BusinessError,
    TechnicalError
}

