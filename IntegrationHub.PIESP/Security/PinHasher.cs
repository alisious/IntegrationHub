using System.Security.Cryptography;
using System.Text;

namespace IntegrationHub.PIESP.Security
{
    public static class PinHasher
    {
        public static string Hash(string pin)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(pin);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public static bool Verify(string enteredPin, string storedHash)
        {
            var enteredHash = Hash(enteredPin);
            return enteredHash == storedHash;
        }
    }
}

