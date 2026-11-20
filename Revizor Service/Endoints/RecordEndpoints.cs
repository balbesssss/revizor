using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Revizor_Service.Database;
using Revizor_Service.Database.Models;
using Revizor_Service.DTO;
using Revizor_Service.Extension;
using Revizor_Service.Responses;
using Revizor_Service.Services;

namespace Revizor_Service.Endoints;

public static class RecordEndpoints
{
    public static IEndpointRouteBuilder RecordBuilderEndpoints (this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/record")
            .WithTags("Record");
        
        group.MapPost("/", async (AppDbContext db, ClaimsPrincipal claims,CreateRecord dto, CancellationToken ct) =>
        {
            var role = claims.FindFirstValue(ClaimTypes.Role);
            var currentUserId = claims.UserFromClaims();
            Guid targetUserId;
            if (role == "admin")
            {
                if (dto.UserId is null)
                    return Results.BadRequest(ApiResponses.Fail("userId is required for admin", ExceptionCode.BadRequest));
                targetUserId = dto.UserId.Value;
            }
            else
            {
                targetUserId = currentUserId;
            }
            
            var currentPlan = await db.Plans.FirstOrDefaultAsync(p => p.IsArchived==false,ct);
            if (currentPlan == null) return Results.NotFound(ApiResponses.Fail("plan not found", ExceptionCode.NotFound));

            var assigment = await db.PlanAssigments.FirstOrDefaultAsync(p=> p.Id == dto.PlanAssigmentId && p.PassById == targetUserId, ct);
            if (assigment == null) return Results.NotFound(ApiResponses.Fail("assigment not found",ExceptionCode.NotFound));
            
            if (assigment is null)
                return Results.NotFound(ApiResponses.Fail("assignment not found", ExceptionCode.NotFound));

            var record = new Record (
                assigment.Id,
                dto.EndAt,
                dto.PassageWay);
            
            db.Records.Add(record);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/record/{record.Id}",ApiResponses.Ok("record add"));
        })
        .RequireAuthorization(p=> p.RequireRole("admin", "revizor"));

        group.MapPatch("/{recordId}", async (AppDbContext db, ClaimsPrincipal claims,Guid recordId, PatchRecord dto, CancellationToken ct) =>
        {
            var userId = claims.UserFromClaims();

            var record = await db.Records.FirstOrDefaultAsync(r=> r.Id == recordId, ct);
            if (record == null) return Results.NotFound(ApiResponses.Fail("record not found",ExceptionCode.NotFound));

            record.SetFields(dto);
            
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return app;
    }
}
