using System;
using System.Runtime.Serialization;

namespace IntegrationHub.Common.Exceptions
{
    /// <summary>
    /// Wyjątek rzucany podczas problemów z obsługą certyfikatów klienta (brak, brak klucza prywatnego, przeterminowanie itp.).
    /// </summary>
    [Serializable]
    public class CertificateException : Exception
    {
        public CertificateException() { }

        public CertificateException(string message)
            : base(message) { }

        public CertificateException(string message, Exception innerException)
            : base(message, innerException) { }

      
    }
}
