using Revizor_Service.DTO;

namespace Revizor_Service.Database.Models;

public class Facility
{
    private Facility() { }
    public Facility(string title, string location)
    {
        Title = title;
        Location = location;
    }

    public Guid Id { get; private set; }
    public string Title {get;private set;}
    public string Location {get;private set;}
    public void SetFields(PatchFacility dto)
    {
        if (dto.Title is not null) Title = dto.Title;
        if (dto.Location is not null) Location = dto.Location;
    }
}
