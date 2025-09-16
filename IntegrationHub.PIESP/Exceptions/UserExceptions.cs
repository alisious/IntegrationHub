using System;

namespace IntegrationHub.PIESP.Exceptions
{
    public class UserAlreadyOnDutyException : Exception
    {
        public UserAlreadyOnDutyException(string badgeNumber)
            : base($"Użytkownik z odznaką '{badgeNumber}' pełni już służbę.") { }
    }
}

