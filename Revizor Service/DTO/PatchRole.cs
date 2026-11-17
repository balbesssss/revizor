using System.ComponentModel.DataAnnotations;

namespace Revizor_Service.DTO;

public record PatchRole
(
    [Required, StringLength(40, MinimumLength = 5)]
    string Name
);
