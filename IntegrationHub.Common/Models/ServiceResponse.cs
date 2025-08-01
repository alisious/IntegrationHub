using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationHub.Common.Models
{
    /// <summary>
    /// Klasa reprezentująca odpowiedź z proxy SOAP.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ServiceResponse<T>
    {
        /// <summary>
        /// Techniczny sukces operacji SOAP. True oznacza, że operacja została wykonana bez błędów.
        /// </summary>
        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; } 
        [JsonPropertyName("message")]
        public string Message { get; set; } = String.Empty; // Komunikat dla użytkownika
        [JsonPropertyName("responseCode")]
        public string? ResponseCode { get; set; } // Kod odpowiedzi (np. Success, Error SOAP Fault, limit. itp)
        [JsonPropertyName("data")]
        public T? Data { get; set; } // Dane zwrócone przez kontorler Web API np. SOAP Response (jeśli są)
        public ProblemDetails? ProblemDetails { get; set; } // Szczegóły problemu, jeśli wystąpił błąd

        public bool HasData => Data != null; // Czy są dane w odpowiedzi
        public bool HasProblemDetails => ProblemDetails != null; // Czy są szczegóły problemu w odpowiedzi

    }

}
