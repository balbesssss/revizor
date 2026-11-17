using Revizor_Service.DTO;

namespace Revizor_Service.Database.Models;

public class Role
{
    public int Id { get; private set; }
    public string Name { get; private set; }

    private Role() {}
    public Role (string name) { Name = name; }

    public void SetFields(PatchRole dto)
    {
        if (dto.Name != null) Name = dto.Name;
    }
}
