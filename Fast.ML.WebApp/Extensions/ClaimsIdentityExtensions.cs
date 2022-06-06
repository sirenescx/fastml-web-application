using System.Security.Claims;

namespace Fast.ML.WebApp.Extensions;

public static class ClaimsIdentityExtensions
{
    public static ClaimsIdentity AddClaim(
        this ClaimsIdentity claimsIdentity, 
        ClaimsPrincipal claimsPrincipal, 
        string claimType)
    {
        var claim = claimsPrincipal.FindFirst(claimType);
        if (claim != null) 
            claimsIdentity.AddClaim(claim);
        return claimsIdentity;
    }
    
    public static ClaimsIdentity AddClaim<T>(
        this ClaimsIdentity claimsIdentity, 
        string claimType, T value)
    {
        var claim = new Claim(claimType, value.ToString() ?? string.Empty);
        claimsIdentity.AddClaim(claim);
        return claimsIdentity;
    }
}