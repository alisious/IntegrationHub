using IntegrationHub.SRP.Models;
using IntegrationHub.SRP.PESEL.SoapClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Interfaces
{
    public interface IPeselSoapMapper
    {
        string BuildSearchRequestEnvelopeXml(SearchPersonInputModel inputModel);
        wyszukajPodstawoweDaneOsobyResponse DeserializeSoapResponse(string responseXml);
        BasicPersonDataResponse MapToDto(wyszukajPodstawoweDaneOsobyResponse response);
    }
}
