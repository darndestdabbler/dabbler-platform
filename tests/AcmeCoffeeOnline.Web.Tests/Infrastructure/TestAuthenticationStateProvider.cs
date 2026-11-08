using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace AcmeCoffeeOnline.Web.Tests.Infrastructure;

public sealed class TestAuthenticationStateProvider : AuthenticationStateProvider
{
    private AuthenticationState _authenticationState;

    public TestAuthenticationStateProvider(ClaimsPrincipal user)
    {
        _authenticationState = new AuthenticationState(user);
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(_authenticationState);

    public void SetUser(ClaimsPrincipal user)
    {
        _authenticationState = new AuthenticationState(user);
        NotifyAuthenticationStateChanged(Task.FromResult(_authenticationState));
    }
}
