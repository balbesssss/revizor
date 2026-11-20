using Revizor_Service.Database;
using ClosedXML.Excel;
using Revizor_Service.Database.Models;
using Microsoft.EntityFrameworkCore;
using Revizor_Service.Responses;
using Microsoft.Extensions.Caching.Memory;

namespace Revizor_Service.Services;
public class ReportsServices(IConfiguration cfg, AppDbContext db, IMemoryCache cache)
{
    private readonly string _folderReports = cfg["folderReports"]!;
    private readonly AppDbContext _db = db;
    private readonly IMemoryCache _cache = cache;

    private async Task<Dictionary<string, List<RecordItem>>> GetRecordsByPlanFromDB (Guid planId, CancellationToken ct)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p=> p.Id == planId,ct);
        if (plan == null) return [];

        var assignments = await _db.PlanAssigments
            .Where(a => a.PlanId == planId)
            .Include(a => a.PassBy)
            .Include(a => a.Facility)
            .ToListAsync(ct);

        var records = await _db.Records
            .Where(r => r.PlanAssigment.PlanId == planId)
            .Include(r=> r.PlanAssigment)
            .ToListAsync(ct);

        var result = assignments
            .Where(a => records.Any(r => r.PlanAssigmentId == a.Id))
            .GroupBy(a => a.PassBy)
            .Select(g => new DataReportWithRecords
            {
                Id = g.Key!.Id,
                FullName = g.Key.GetFullName(),
                Resords = g.Select(a =>
                {
                    var record = records.FirstOrDefault(r => r.PlanAssigmentId == a.Id);
                    return new RecordItem
                    {
                        FacilityTitle = a.Facility!.Title,
                        FacilityLocation = a.Facility.Location,
                        StartAt = record != null 
                            ? TimeOnly.FromDateTime(record.PlanAssigment.PassAt) 
                            : TimeOnly.MinValue,
                        EndAt = record?.EndAt ?? TimeOnly.MinValue,
                        PassegeWay = record?.PassageWay ?? 0f
                    };
                }).ToList()
            })
            .ToDictionary(p=> p.FullName, p=> p.Resords);
        return result;
    }

    public async Task<Dictionary<string, List<RecordItem>>> GetRecordsByPlan(Guid planId, CancellationToken ct)
    {
        var key = $"records_by_plan_{planId}";
        if (_cache.TryGetValue(key,out Dictionary<string, List<RecordItem>>? cached) && cached is not null)
        {
            return cached;
        }
        var result = await GetRecordsByPlanFromDB(planId, ct);
        _cache.Set(key,result, TimeSpan.FromHours(24));
        return result;
    }
    
    public TimeSpan GetNightHours(TimeOnly start, TimeOnly end)
    {
        var startTs = start.ToTimeSpan();
        var endTs = end.ToTimeSpan();
        
        if (endTs < startTs)
            endTs = endTs.Add(TimeSpan.FromHours(24));
        
        var night1Start = TimeSpan.Zero;
        var night1End = TimeSpan.FromHours(6);
        var night2Start = TimeSpan.FromHours(22);
        var night2End = TimeSpan.FromHours(30);
        
        var total = TimeSpan.Zero;
        
        var p1Start = startTs > night1Start ? startTs : night1Start;
        var p1End = endTs < night1End ? endTs : night1End;
        if (p1End > p1Start)
            total += p1End - p1Start;
        
        var p2Start = startTs > night2Start ? startTs : night2Start;
        var p2End = endTs < night2End ? endTs : night2End;
        if (p2End > p2Start)
            total += p2End - p2Start;
        
        return total;
    }

    public async Task<int> CreateDocumentReportAdmin(Guid planId, User admin, CancellationToken ct)
    {
        using var workbook = new XLWorkbook();
        var currentDate = DateTime.Now;
        var plan = await _db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return 0;
        var records = await GetRecordsByPlan(planId, ct);
        if (records.Count == 0) return 0;
        foreach (var pair in records)
        {
            var worksheet = workbook.Worksheets.Add($"{pair.Key}");
            worksheet.Cell("A1").Value = currentDate.ToString("dd.MM.yyyy");

            worksheet.Range("A2:G2").Merge();
            worksheet.Cell("A2").Value = pair.Key.ToString();
            worksheet.Cell("A3").Value = "Наименование объекта";
            worksheet.Cell("B3").Value = "Время";
            worksheet.Cell("C3").Value = "Общее";
            worksheet.Cell("D3").Value = "Ночные";
            worksheet.Cell("E3").Value = "Проезд";

            for (var i = 0; i < pair.Value.Count; i++)
            {
                var startAt = pair.Value[i].StartAt;
                var endAt = pair.Value[i].EndAt;

                var startTs = startAt.ToTimeSpan();
                var endTs = endAt.ToTimeSpan();
                if (endTs < startTs) endTs = endTs.Add(TimeSpan.FromHours(24));

                var total = endTs - startTs;
                var totalFormatted = $"{(int)total.TotalHours}:{total.Minutes:D2}:{total.Seconds:D2}";
                worksheet.Cell($"A{i + 4}").Value = $"{pair.Value[i].FacilityTitle} {pair.Value[i].FacilityLocation}";
                worksheet.Cell($"B{i + 4}").Value = $"{startAt.ToString("HH:mm:ss")} - {endAt.ToString("HH:mm:ss")}";
                worksheet.Cell($"C{i + 4}").Value = totalFormatted;
                worksheet.Cell($"D{i + 4}").Value = GetNightHours(startAt, endAt);
                worksheet.Cell($"E{i + 4}").Value = pair.Value[i].PassegeWay;
            }
        }
        _db.Reports.Add(new Report
        (
            admin.Id, planId, 
            $"user_{admin.GetFullName()}",
            $"{_folderReports}/Revizor/"
        ));
        await _db.SaveChangesAsync(ct);
        workbook.SaveAs($"{_folderReports}/Admin/{currentDate.Month}_{currentDate.Year}.xlsx");
        return 1;
    }   

    public async Task<int> CreateDocumentReportUser(Guid reportId, string userName, CancellationToken ct)
    {
        using var workbook = new XLWorkbook();
        var currentDate = DateTime.Now;
        var worksheet = workbook.AddWorksheet(" ");

        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == reportId);
        if (report == null) return 0;

        var key = $"records_by_plan_{report.PlanId}";

        Dictionary<string, List<RecordItem>> allRecords = [];
        
        allRecords = await GetRecordsByPlan(report.PlanId, ct);
        
        if (!allRecords.TryGetValue(userName,out var recordUser) || recordUser is null) 
            return 0;
        
        worksheet.Range("A2:G2").Merge();
        worksheet.Cell("A2").Value = userName;
        worksheet.Cell("A3").Value = "Дата";
        worksheet.Cell("B3").Value = "Наименование объекта";
        worksheet.Cell("C3").Value = "Время";
        worksheet.Cell("D3").Value = "Общее";
        worksheet.Cell("E3").Value = "Ночные";
        worksheet.Cell("F3").Value = "Проезд";
        for (int i = 0; i < recordUser.Count; i++)
        {
            worksheet.Cell($"A{i + 4}").Value = recordUser[i].CreatedAt.ToString("dd.MM.yyyy HH:mm:ss");
            worksheet.Cell($"B{i + 4}").Value = $"{recordUser[i].FacilityTitle} {recordUser[i].FacilityLocation}";
            worksheet.Cell($"C{i + 4}").Value = $"{recordUser[i].StartAt.ToString("HH:mm:ss")} - {recordUser[i].EndAt.ToString("HH:mm:ss")}";
            worksheet.Cell($"D{i + 4}").Value = (recordUser[i].EndAt.ToTimeSpan() - recordUser[i].StartAt.ToTimeSpan()).ToString(@"hh\:mm\:ss");

            var startAt = recordUser[i].StartAt;
            var endAt = recordUser[i].EndAt;

            worksheet.Cell($"E{i + 4}").Value = GetNightHours(startAt, endAt);
            worksheet.Cell($"F{i + 4}").Value = recordUser[i].PassegeWay;
        }
        workbook.SaveAs($"{_folderReports}/Revizor/{currentDate.Month}_{currentDate.Year}.xlsx");
        return 1;
    }
}