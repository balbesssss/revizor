using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Revizor_Service.Database;
using Revizor_Service.Database.Models;
using Revizor_Service.DTO;
using Revizor_Service.Responses;
using Revizor_Service.Services;

namespace Revizor_Service.Endoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder UserBuiderEndpoints (this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users")
            .WithTags("User")
            .RequireAuthorization("admin");

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) => 
        {
            var users = await db.Users
            .Include(u=> u.Role)
            .AsNoTracking()
            .Select(u=> new UserResponse
            {
                Id = u.Id,
                FullName = u.GetFullName(),
                Role = u.Role!.Name,
                CreatedAt = u.CreatedAt
            }).ToArrayAsync(ct);
            return Results.Ok(ApiResponses<IEnumerable<UserResponse>>.Ok(data: users));
        });

        group.MapPost("/", async (IPasswordHasher<CreateUser> hasher,AppDbContext db, CreateUser dto, CancellationToken ct) =>
        {
            var user = new User (dto.FirstName,dto.SecondName,dto.LastName,dto.RoleId);
            user.SetPassword(hasher.HashPassword(dto, dto.Password));
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/users/{user.Id}", ApiResponses.Ok("User is created"));
        });

        group.MapDelete("/{userId}", async (AppDbContext db, Guid userId, CancellationToken ct) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u=> u.Id == userId, ct);
            if (user==null) {return Results.NotFound(ApiResponses.Fail("User not found", ExceptionCode.DoesNotExists));}
            user.DeleteUser();
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        group.MapGet("/{userId}", async (AppDbContext db, Guid userId, CancellationToken ct) =>
        {
            var user = await db.Users
                .AsNoTracking()
                .Include(u=> u.Role)
                .FirstOrDefaultAsync(u=> u.Id == userId, ct);
            return user != null ? Results.Ok(ApiResponses<UserResponse>.Ok(new ()
            {
                Id = user.Id,
                FullName = user.GetFullName(),
                Role = user.Role!.Name,
                CreatedAt = user.CreatedAt
            })) : Results.NotFound(ApiResponses.Fail("User does not exists", ExceptionCode.DoesNotExists));
        });

        group.MapPatch("/{userId}",async (AppDbContext db, Guid userId, PatchUser dto, CancellationToken ct ) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u=> u.Id == userId, ct);
            if (user == null) { return Results.NotFound(ApiResponses.Fail("User does not exists", ExceptionCode.DoesNotExists));}
            user.SetFields(dto);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
        
        return app;
    }
}
