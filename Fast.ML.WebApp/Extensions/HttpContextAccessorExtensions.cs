using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Fast.ML.WebApp.Extensions;

public static class HttpContextAccessorExtensions
{
    public static string GetClaimValue(this IHttpContextAccessor httpContextAccessor, string claimType)
    {
        var identity = (ClaimsIdentity) httpContextAccessor.HttpContext?.User.Identity;
        var claims = identity?.Claims.ToList();
        return claims?.FirstOrDefault(claim => claim.Type.Equals(claimType))?.Value;
    }
}