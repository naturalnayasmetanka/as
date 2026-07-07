using AuthService.Contracts;
using AuthService.Domain.Accounts;

namespace AuthService.Core.Authentication;

public interface IJwtTokenService
{
    JwtTokenResult Create(Account user, IEnumerable<string> roles);
}
