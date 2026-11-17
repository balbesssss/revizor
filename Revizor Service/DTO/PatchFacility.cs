using System.ComponentModel.DataAnnotations;

namespace Revizor_Service.DTO;

public record PatchFacility 
(
    [StringLength(100, MinimumLength = 3)]
    string? Title = null,

    [StringLength(150, MinimumLength = 10)]
    string? Location = null
);
