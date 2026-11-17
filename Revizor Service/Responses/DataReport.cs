namespace Revizor_Service.Responses;

public class DataReportWithRecords
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required List<RecordItem> Resords {get;set;}
}

public class RecordItem
{
    public DateTime CreatedAt {get;set;}
    public required string FacilityTitle { get; set; }
    public required string FacilityLocation { get; set; }
    public TimeOnly StartAt { get; set; }
    public TimeOnly EndAt { get; set; }
    public float PassegeWay {get;set;}
}
