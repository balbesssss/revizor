namespace Revizor_Service.DTO;

public record PatchRecord
(
    Guid? PlanAssigmentId = null,
    TimeOnly? EndAt = null,
    float? PassageWay = null
);
