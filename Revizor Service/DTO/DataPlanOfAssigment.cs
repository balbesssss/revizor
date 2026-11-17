namespace Revizor_Service.DTO;

public class DataPlanOfAssigment
{
    public required string Facility { get; set; }
    public DateTime PassAt {get;set;}
    public required List<UserShort> Revizors { get; set; }
    public required UserShort ResponsiblePerson {get;set;}
}
