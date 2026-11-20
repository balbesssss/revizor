using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Revizor_Service.Database;
using Revizor_Service.Extension;
using Revizor_Service.Responses;
using Revizor_Service.Services;

namespace Revizor_Service.Endoints;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder ReportBuilderEndpoints (this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/reports")
            .WithTags("Report");

        group.MapGet("/{planId}/generate", async (AppDbContext db,ReportsServices reportsServices, ClaimsPrincipal claims, Guid planId, CancellationToken ct) =>
        {
            var userId = claims.UserFromClaims();
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u=> u.Id==userId, ct);
            if (user == null) return Results.Unauthorized();
            var success = await reportsServices.CreateDocumentReportAdmin(planId,user, ct);
            if (success == 0) return Results.BadRequest();
            return Results.Ok(ApiResponses.Ok("Success generate"));
        })
        .RequireAuthorization("admin");

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
        {
            var results = await db.Reports
                .AsNoTracking()
                .Select(p=> new DataReportToFront {
                    Id = p.Id,
                    PlanId = p.Plan!.Id,
                    Name = p.Plan.Name,
                    PeriodStart = p.Plan.PeriodStart,
                    PeriodEnd = (DateOnly)p.Plan.PeriodEnd!,
                    }).ToArrayAsync(ct);
            return Results.Ok(ApiResponses<IEnumerable<DataReportToFront>>.Ok(results));
        })
        .RequireAuthorization(p=> p.RequireRole("admin", "revizor"));

        group.MapGet("/{planId}" , async (AppDbContext db, ReportsServices reportsServices, Guid planId, CancellationToken ct) =>
        { 
            var result = await reportsServices.GetRecordsByPlan(planId, ct);
            return Results.Ok(ApiResponses<Dictionary<string, List<RecordItem>>>.Ok(result));
        })
        .RequireAuthorization("admin");

        group.MapGet("/{reportId}/download", async (AppDbContext db, IConfiguration cfg, ReportsServices reportsServices,ClaimsPrincipal claims ,Guid reportId, CancellationToken ct) =>
        {
            var userId = claims.UserFromClaims();
            
            var user = await db.Users.FirstOrDefaultAsync(u=>u.Id == userId, ct);
            if (user == null) return Results.Unauthorized();

            var report = await db.Reports
                                .Include(r=> r.Plan)
                                .FirstOrDefaultAsync(p=> p.Id == reportId, ct);
            if (report==null) return Results.NotFound(
                                        ApiResponses.Fail(
                                        """
                                        report not found, try to generate report use next link '/generate-reports/planId' . where planId - your necessary plan 
                                        """,
                                        ExceptionCode.NotFound));

            return Results.File(
                path: $"{cfg["folderReports"]}/Admin/{report.Plan.PeriodStart.Month}_{report.Plan.PeriodStart.Year}.xlsx",
                contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileDownloadName: $"{report.Plan.PeriodStart.Month}_{report.Plan.PeriodStart.Year}.xlsx"
            );
        })
        .RequireAuthorization("admin");

        return app;
    }
}
