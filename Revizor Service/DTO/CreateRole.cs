using System.ComponentModel.DataAnnotations;

namespace Revizor_Service.DTO;

public record CreateRole
(
    [StringLength(40, MinimumLength = 5)]
    string Name
);

