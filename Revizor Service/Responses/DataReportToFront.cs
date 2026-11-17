namespace Revizor_Service.Responses;
public class DataReportToFront
{
    public Guid PlanId {get;set;}
    public required string Name { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
}