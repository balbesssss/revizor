using System.ComponentModel.DataAnnotations;

namespace Revizor_Service.DTO;

public record CreateRecord
(
    Guid PlanAssigmentId,
    TimeOnly EndAt,
    Guid? UserId,
    [Range(0, float.MaxValue)]
    float PassageWay
);
