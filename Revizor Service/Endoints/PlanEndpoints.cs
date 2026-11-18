using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Revizor_Service.Database;
using Revizor_Service.Database.Models;
using Revizor_Service.DTO;
using Revizor_Service.Extension;
using Revizor_Service.Responses;
using Revizor_Service.Services;

namespace Revizor_Service.Endoints;

public static class PlanEndpoints
{
    public static IEndpointRouteBuilder PlanBuilderEndpoint ( this IEndpointRouteBuilder app )
    {
        var group = app.MapGroup("/plan")
            .WithTags("Plan");

        group.MapGet("/", async (AppDbContext db) => 
        {
            var res = await db.Plans.AsNoTracking().ToArrayAsync();
            return Results.Ok(ApiResponses<IEnumerable<Plan>>.Ok(res));
        })
        .RequireAuthorization("admin");

        group.MapGet("/{planId}", async (AppDbContext db, Guid planId, CancellationToken ct) =>
        {
            var plan = await db.Plans.AsNoTracking().FirstOrDefaultAsync(p=> p.Id == planId, ct);
            return plan != null? Results.Ok(ApiResponses<Plan>.Ok(plan)) : Results.NotFound(ApiResponses.Fail("plan not found", ExceptionCode.NotFound));
        })
        .RequireAuthorization("admin");

        group.MapPost("/", async (AppDbContext db, ClaimsPrincipal claims, CreatePlan dto, CancellationToken ct) =>
        {
            var userId = claims.UserFromClaims();
            db.Plans.Add(new Plan(dto.Name, userId, dto.PeriodStart,dto.PeriodEnd));
            await db.SaveChangesAsync(ct);
            return Results.Created("/plan/",ApiResponses.Ok("Plan is created"));    
        })
        .RequireAuthorization("admin");

        group.MapPatch("/{planId}",async (AppDbContext db, Guid planId,ClaimsPrincipal claims, PatchPlan dto, CancellationToken ct ) =>
        {
            var plan = await db.Plans.FirstOrDefaultAsync(p=>p.Id==planId,ct);
            if (plan==null) return Results.NotFound(ApiResponses.Fail("plan not found",ExceptionCode.NotFound));
            
            plan.SetFields(dto);

            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        })
        .RequireAuthorization("admin");

        group.MapDelete("/{planId}", async (AppDbContext db, Guid planId, CancellationToken ct) =>
        {
            var plan = await db.Plans.FirstOrDefaultAsync(p=> p.Id == planId,ct);
            if (plan == null) return Results.NotFound(ApiResponses.Fail("plan not found", ExceptionCode.NotFound));
            db.Plans.Remove(plan);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();     
        })
        .RequireAuthorization("admin");

        group.MapGet("/current", async (AppDbContext db, ClaimsPrincipal claims, CancellationToken ct ) =>
        {
            var currentPlan = await db.Plans.AsNoTracking().FirstOrDefaultAsync(p=>p.IsArchived==false, ct);
            if (currentPlan == null) return Results.Ok(ApiResponses.Ok("current plan is empty"));

            var userId = claims.UserFromClaims();

            if (claims.FindFirstValue(ClaimTypes.Role) == "admin")
            {
                var assigments = await db.PlanAssigments
                    .AsNoTracking()
                    .Where(r=> r.PlanId == currentPlan.Id)
                    .Include(r=> r.Facility)
                    .Include(r=> r.PassBy)
                    .Include(r=> r.ResponsiblePerson)
                    .ToListAsync(ct);
                var results = assigments.GroupBy(a=> a.Facility).Select(g=> new AssigmentItem
                {
                    Id = g.Select(p=> p.Id).First(),
                    FacilityId = g.Key.Id,
                    FacilityTitle = g.Key.Title,
                    FacilityLocation = g.Key.Location,
                    PassAt = g.Select(u=> u.PassAt).FirstOrDefault(),
                    Revizors = g.Select(u=> new UserShort {
                        Id = u.PassById,
                        FullName = u.PassBy!.GetFullName(),
                        }).ToList(),
                    ResponsiblePerson = g.Select(r => new UserShort
                    {
                        Id = (Guid)r.ResponsiblePersonId!,
                        FullName = r.ResponsiblePerson!.GetFullName()
                    }).FirstOrDefault(),
                }).ToList();
                return Results.Ok(ApiResponses<DataCurrentPlanOfAssigment>.Ok(new () {NamePlan = currentPlan.Name, Assigments=results}));
            }
            if (claims.FindFirstValue(ClaimTypes.Role) == "revizor")
            {
                var assigments = await db.PlanAssigments
                    .AsNoTracking()
                    .Where(r=> r.PlanId == currentPlan.Id)
                    .Where(r=> r.PassById == userId)
                    .Include(r=> r.Facility)
                    .Include(r=> r.PassBy)
                    .Include(r=> r.ResponsiblePerson)
                    .ToListAsync(ct);
                return Results.Ok(ApiResponses<List<PlanAssigment>>.Ok(assigments));
            }
            else {return Results.BadRequest();}

        })
        .RequireAuthorization(policy => policy.RequireRole("admin","revizor"));

        group.MapPost("/archived", async (AppDbContext db, CancellationToken ct) =>
        {
            var plan = await db.Plans.FirstOrDefaultAsync(p=> p.IsArchived == false, ct);
            if (plan == null) return Results.NotFound(ApiResponses.Fail("plan not found",ExceptionCode.NotFound));
            plan.Archived();
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        })
        .RequireAuthorization("admin");

        return app;
    }
}
