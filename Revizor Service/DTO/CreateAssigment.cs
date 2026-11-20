namespace Revizor_Service.DTO;

public record CreateAssigment
(
    Guid FacilityId,
    DateTime PassAt, 
    Guid PassById,
    Guid? ResponsiblePersonId = null
);
