using System.Security.Claims;

namespace Revizor_Service.Extension;

public static class ClaimsExtension
{
    public static Guid UserFromClaims(this ClaimsPrincipal claims)
    {
        var value = claims.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out Guid id))
            throw new UnauthorizedAccessException("invalid or missing user in claims");
        return id;
    }
}
