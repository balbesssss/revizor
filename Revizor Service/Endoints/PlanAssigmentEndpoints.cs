using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Revizor_Service.Database;
using Revizor_Service.Database.Models;
using Revizor_Service.DTO;
using Revizor_Service.Extension;
using Revizor_Service.Responses;
using Revizor_Service.Services;

namespace Revizor_Service.Endoints;

public static class PlanAssigmentEndpoints
{
    public static IEndpointRouteBuilder PlanAssigmentBuilderEndpoint (this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/plan-assigment")
            .WithTags("PlanAssigment")
            .RequireAuthorization("admin");

        group.MapGet("/{planId}" , async (AppDbContext db, Guid planId, CancellationToken ct) =>
        {
            var assigments = await db.PlanAssigments
                .AsNoTrackingWithIdentityResolution()
                .Where(p=> p.PlanId == planId)
                .Include(p=> p.PassBy)
                .Include(p=> p.ResponsiblePerson)
                .Include(p=> p.Facility)
                .ToArrayAsync(ct);

            var result = assigments.GroupBy(p=> p.Facility)
                .Select(r=> new DataPlanOfAssigment
                {
                    Facility = $"{r.Key.Title} {r.Key.Location}",
                    PassAt = r.Select(p=> p.PassAt).FirstOrDefault(),
                    Revizors = r.Select(rev=> new UserShort
                    {
                        Id = rev.PassById,
                        FullName = rev.PassBy!.GetFullName()
                    }).ToList(),
                    ResponsiblePerson = r
                        .Where(a => a.ResponsiblePersonId is not null)
                        .Select(rp=> new UserShort
                        {
                            Id = rp.ResponsiblePersonId!.Value,
                            FullName = rp.ResponsiblePerson!.GetFullName()
                        })
                        .First()
                }).ToList();
            return Results.Ok(ApiResponses<IEnumerable<DataPlanOfAssigment>>.Ok(result)) ;
        });

        group.MapPost("/{planId}", async (AppDbContext db, ClaimsPrincipal claims, Guid planId, List<CreateAssigment> dtos, CancellationToken ct) =>
        {
            var userId = claims.UserFromClaims();

            var plan = await db.Plans.FirstOrDefaultAsync(p=> p.Id == planId, ct);
            if (plan == null) return Results.NotFound(ApiResponses.Fail("plan not found", ExceptionCode.NotFound));

            if (dtos.Count == 0) return Results.Ok(ApiResponses.Ok("you send empty body"));

            foreach (var assigment in dtos)
            {
                var objectAssigment = new PlanAssigment(planId, assigment);
                db.PlanAssigments.Add(objectAssigment);
            }
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponses.Ok("Plan Assigments created")); 
        });

        return app;
    }
}
