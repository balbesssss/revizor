using System.ComponentModel.DataAnnotations;

namespace Revizor_Service.DTO;

public record PatchUser
(
    [StringLength(40, MinimumLength = 3)]
    string? FirstName = null,
    
    [StringLength(40, MinimumLength = 3)]
    string? SecondName = null,
    
    [StringLength(40, MinimumLength = 3)]
    string? LastName = null,
    
    [Range(0, int.MaxValue)]
    int? RoleId = null
);
