using Microsoft.EntityFrameworkCore;
using Revizor_Service.Database;
using Revizor_Service.Database.Models;
using Revizor_Service.DTO;
using Revizor_Service.Responses;
using Revizor_Service.Services;

namespace Revizor_Service.Endoints;

public static class FacilityEndpoints
{
    public static IEndpointRouteBuilder FacilityBuilderEndpoints (this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("facility")
            .WithTags("Facility")
            .RequireAuthorization("admin");
        
        group.MapGet("/", async (AppDbContext db, CancellationToken ct) => 
        {
            var res = await db.Facilities.AsNoTracking().ToArrayAsync(ct);
            Results.Ok(ApiResponses<IEnumerable<Facility>>.Ok(res));
        });

        group.MapPost("/", async (AppDbContext db, CreateFacility dto, CancellationToken ct) =>
        {
            db.Facilities.Add(new Facility (dto.Title,dto.Location));
            await db.SaveChangesAsync(ct);
            return Results.Created("/facilities/",ApiResponses.Ok("Facility is created"));
        });

        group.MapDelete("/{facilityId}", async (AppDbContext db, Guid facilityId, CancellationToken ct) =>
        {
            var facility = await db.Facilities.FirstOrDefaultAsync(f => f.Id==facilityId,ct);
            if (facility == null) return Results.NotFound(ApiResponses.Fail("Facility not found", ExceptionCode.DoesNotExists));
            db.Facilities.Remove(facility);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        group.MapGet("/{facilityId}", async (AppDbContext db, Guid facilityId, CancellationToken ct) =>
        {
            var facility = await db.Facilities.AsNoTracking().FirstOrDefaultAsync(f=> f.Id==facilityId,ct);
            if (facility == null) return Results.NotFound(ApiResponses.Fail("Facility does not exists", ExceptionCode.DoesNotExists));
            return Results.Ok(ApiResponses<Facility>.Ok(data: facility));
        });

        group.MapPatch("/{facilityId}",async (AppDbContext db, Guid facilityId, PatchFacility dto, CancellationToken ct ) =>
        {
            var facility = await db.Facilities.FirstOrDefaultAsync(f=> f.Id == facilityId,ct);
            if (facility == null) return Results.NotFound(ApiResponses.Fail("Facility does not exists", ExceptionCode.DoesNotExists));
            facility.SetFields(dto);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return app;
    }
}
