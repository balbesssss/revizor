namespace Revizor_Service.DTO;

public record MultipleAssigment
(   
    Guid FacilityId,
    DateTime PassAt, 
    Guid PassById,
    Guid ResponsiblePersonId
);

public record CreateAssigment
(
    Guid PlanId,
    Guid FacilityId,
    DateTime PassAt, 
    Guid PassById,
    Guid ResponsiblePersonId
);
