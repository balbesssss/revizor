namespace Revizor_Service.DTO;

public record PatchAssigment 
(
    Guid? PlanId = null,
    Guid? FacilityId = null,
    DateTime? PassAt = null, 
    Guid? PassById = null,
    Guid? ResponsiblePersonId = null
);
