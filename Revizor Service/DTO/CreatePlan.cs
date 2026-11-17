using System.ComponentModel.DataAnnotations;

namespace Revizor_Service.DTO;

public record CreatePlan
( 
    [StringLength(50, MinimumLength = 10)]
    string Name,

    DateOnly PeriodStart,
    
    DateOnly? PeriodEnd
);
