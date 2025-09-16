using IntegrationHub.SRP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Contracts
{
    public class GetPersonByPeselResponse
    {
        public Osoba? daneOsoby { get; set; }
    }
}
