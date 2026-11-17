using System.ComponentModel.DataAnnotations;

namespace Revizor_Service.DTO;

public record PatchPlan
(
    [StringLength(50, MinimumLength = 10)]
    string? Name = null,
    DateOnly? PeriodStart = null,
    DateOnly? PeriodEnd = null
);
