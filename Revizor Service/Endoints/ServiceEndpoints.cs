using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Revizor_Service.Database;
using Revizor_Service.Database.Models;
using Revizor_Service.DTO;
using Revizor_Service.Extension;
using Revizor_Service.Responses;
using Revizor_Service.Services;

namespace Revizor_Service.Endoints;

public static class ServiceEndpoints
{
    public static IEndpointRouteBuilder ServiceBuilderEndpoints (this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/service")
            .WithTags("Service");
            
        group.MapPost("/login", async (AppDbContext db, IPasswordHasher<User> hasher, IConfiguration cfg, HttpContext httpContext, Login dto, CancellationToken ct) =>
        {
            var user = await db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync
                (u=> 
                    u.FirstName == dto.FirstName
                    && u.SecondName==dto.SecondName 
                    && u.LastName==dto.LastName,
                ct);
            if (user==null)
            {
                return Results.Unauthorized();
            }
            if (hasher.VerifyHashedPassword(user,user.Password, dto.Password) != PasswordVerificationResult.Success)
            {
                return Results.Unauthorized();
            }
            
            var newRefresh = RefreshToken.GenerateRefreshToken();
            var newRefreshHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(newRefresh)));

            user.UpdateRefreshData(newRefreshHash, DateTime.UtcNow.AddDays(7));

            await db.SaveChangesAsync(ct);
            
            httpContext.AppendCookiesInfo("refreshToken", newRefresh, (DateTime)user.RefreshExpireTime!);

            return Results.Ok(
                ApiResponses<Token>.Ok(
                    new (){ 
                        AccessToken = JWT.GenToken(user,cfg)
                        }
                    )
                );
        });

        group.MapPost("/refresh", async (AppDbContext db, HttpContext http, IConfiguration cfg, CancellationToken ct) =>
        {
            var refreshToken = http.Request.Cookies["refreshToken"];
            if (refreshToken == null) return Results.Unauthorized();
            
            var refreshHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
            
            var user = await db.Users
                .Include(u=>u.Role)
                .FirstOrDefaultAsync(u=> u.RefreshToken == refreshHash, ct);
            if (user==null) return Results.Unauthorized();

            if (user.RefreshExpireTime is null || user.RefreshExpireTime < DateTime.UtcNow) 
            {
                user.RevokeRefresh();
                await db.SaveChangesAsync(ct);
                return Results.Unauthorized();
            }

            var newRefresh = RefreshToken.GenerateRefreshToken();
            var newRefreshHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(newRefresh)));

            user.UpdateRefreshData(newRefreshHash, DateTime.UtcNow.AddDays(7));
            http.AppendCookiesInfo("refreshToken", newRefresh, (DateTime)user.RefreshExpireTime!);

            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponses<Token>.Ok(new Token() {AccessToken = JWT.GenToken(user, cfg)}));
        });

        group.MapGet("/users/me", async (AppDbContext db, HttpContext http, ClaimsPrincipal claims, CancellationToken ct) =>
        {
            var userId = claims.UserFromClaims();
            var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u=> u.Id == userId, ct);
            if (user == null) return Results.Unauthorized();

            var userInfo = new
            {
                Id = user.Id,
                FirstName = user.FirstName,
                SecondName = user.SecondName,
                LastName = user.LastName,
                Role = user.Role!.Name,
                CreatedAt = user.CreatedAt
            };

            var json = JsonSerializer.Serialize(userInfo);
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .Replace("=", "");

            http.AppendCookiesInfo("userInfo", base64 , (DateTime)user.RefreshExpireTime);

            return Results.NoContent();
        })
        .RequireAuthorization(policy => policy.RequireRole("admin","revizor"));

        group.MapPost("/users/me/change-password/", async (AppDbContext db, ClaimsPrincipal claims, IPasswordHasher<User> hasher, string pwd, CancellationToken ct) =>
        {   
            var userId = claims.UserFromClaims();

            var user = await db.Users.FirstOrDefaultAsync(u=> u.Id == userId, ct); 
            if (user == null) return Results.NotFound();

            user.SetPassword(hasher.HashPassword(user, pwd));
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        })
        .RequireAuthorization(policy => policy.RequireRole("admin","revizor"));
        
        return app;
    }
}
