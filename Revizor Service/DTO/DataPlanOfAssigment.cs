namespace Revizor_Service.DTO;

public class DataPlanOfAssigment
{
    public string Facility { get; set; }
    public DateTime PassAt {get;set;}
    public List<UserShort> Revizors { get; set; }
    public UserShort ResponsiblePerson {get;set;}
}
