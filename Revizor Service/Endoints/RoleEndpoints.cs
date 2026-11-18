using Microsoft.EntityFrameworkCore;
using Revizor_Service.Database;
using Revizor_Service.Database.Models;
using Revizor_Service.DTO;
using Revizor_Service.Responses;
using Revizor_Service.Services;

namespace Revizor_Service.Endoints;

public static class RoleEndpoints
{
    public static IEndpointRouteBuilder RoleBuilderEndoints (this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/roles")
            .WithTags("Role")
            .RequireAuthorization("admin");

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) => 
        {
            var res = await db.Roles.AsNoTracking().ToArrayAsync(ct);
            return Results.Ok(ApiResponses<IEnumerable<Role>>.Ok(res)); 
        });

        group.MapPost("/", async (AppDbContext db, CreateRole dto, CancellationToken ct) =>
        {
            db.Roles.Add(new Role (dto.Name.ToLower()));
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponses.Ok("Role is created"));
        });

        group.MapDelete("/{roleId}", async (AppDbContext db, int roleId, CancellationToken ct) =>
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Id== roleId, ct);
            if (role == null) return Results.NotFound(ApiResponses.Fail("Role not found", ExceptionCode.DoesNotExists));
            db.Roles.Remove(role);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        group.MapGet("/{roleId}", async (AppDbContext db, int roleId, CancellationToken ct) =>
        {
            var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r=> r.Id==roleId, ct);
            return role != null ? Results.Ok(ApiResponses<Role>.Ok(data: role)) : Results.NotFound(ApiResponses.Fail("Role does not exists", ExceptionCode.DoesNotExists));
        });

        group.MapPatch("/{roleId}",async (AppDbContext db, int roleId, PatchRole dto, CancellationToken ct) =>
        {
            var role = await db.Roles.FirstOrDefaultAsync(r=> r.Id == roleId, ct);
            if (role == null) {return Results.NotFound(ApiResponses.Fail("Role does not exists", ExceptionCode.DoesNotExists));}
            role.SetFields(dto);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
        
        return app;
    }
}
