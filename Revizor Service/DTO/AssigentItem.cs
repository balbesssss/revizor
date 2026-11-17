namespace Revizor_Service.DTO;

public class AssigmentItem
{
    public Guid Id { get; set; }
    public Guid FacilityId { get; set; }
    public required string FacilityTitle { get; set; }
    public required string FacilityLocation { get; set; }
    public DateTime PassAt { get; set; }
    public required List<UserShort> Revizors { get; set; }
    public UserShort? ResponsiblePerson { get; set; }
}

public class UserShort
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
}