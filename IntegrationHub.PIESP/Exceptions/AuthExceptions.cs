using System;

namespace IntegrationHub.PIESP.Exceptions
{
    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(string badgeNumber)
            : base($"Nie znaleziono użytkownika z odznaką '{badgeNumber}'.") { }

    }

    public class WrongPinException : Exception
    {
        public WrongPinException(string badgeNumber)
            : base($"Nieprawidłowy PIN użytkownika z odznaką '{badgeNumber}'.") { }

    }
}

