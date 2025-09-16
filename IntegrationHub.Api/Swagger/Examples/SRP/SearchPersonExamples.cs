using IntegrationHub.Common.Contracts;
using IntegrationHub.SRP.Contracts;      
using IntegrationHub.SRP.Models;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Filters;


namespace IntegrationHub.Api.Swagger.Examples.SRP;

public sealed class SearchPerson200Example : IExamplesProvider<ProxyResponse<SearchPersonResponse>>
{
    public ProxyResponse<SearchPersonResponse> GetExamples() => new()
    {
        RequestId = "0f1c2e3d-1234-5678-9abc-def012345678",
        Source = "SRP",
        Status = ProxyStatus.Success,
        SourceStatusCode = StatusCodes.Status200OK,
        Data = new SearchPersonResponse
        {
            // Uzupełnij minimalnie zgodnie z Twoim typem
            Persons = new() { new OsobaZnaleziona 
            { 
                IdOsoby = "123456789ab678cf000",
                Pesel = "73020916558",
                SeriaINumerDowodu = "ABC123456",
                Nazwisko = "Kowalski",
                ImiePierwsze = "Jan",
                ImieDrugie = "Andrzej",
                DataUrodzenia = "1973-02-09",
                MiejsceUrodzenia = "Warszawa",
                Plec = "MEZCZYZNA",
                CzyPeselAnulowany = false,
                CzyZyje = true,
                Zdjecie = "/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAAYEBQYFBAYGBQYHBwYIChAKCgkJChQODwwQFxQYGBcUFhYaHSUfGhsjHBYWICwgIyYnKSopGR8tMC0oMCUoKSj/2wBDAQcHBwoIChMKChMoGhYa..."
                } 
            }
        }
    };
}

public sealed class SearchPerson400Example : IExamplesProvider<ProxyResponse<SearchPersonResponse>>
{
    public ProxyResponse<SearchPersonResponse> GetExamples() => new()
    {
        RequestId = "9d8c7b6a-4321-8765-9abc-def012345678",
        Source = "SRP",
        Status = ProxyStatus.BusinessError,
        SourceStatusCode = StatusCodes.Status400BadRequest,
        ErrorMessage = "Podaj PESEL albo (nazwisko + imię).",
        Data = null
    };
}
