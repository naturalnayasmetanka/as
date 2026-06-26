using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using System.Text;

namespace AuthService.Core.Authentication
{
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public string Key { get; set; } = string.Empty;
    }
}
