using Revizor_Service.DTO;

namespace Revizor_Service.Database.Models;

public class PlanAssigment
{
    public Guid Id {get;private set;}
    public Guid PlanId {get;private set;}
    public Plan? Plan {get;private set;}
    public Guid FacilityId {get;private set;}
    public Facility? Facility {get;private set;}
    public DateTime PassAt {get;private set;}
    public Guid PassById {get;private set;}
    public User? PassBy {get;private set;}
    public Guid? ResponsiblePersonId {get;private set;} = null;
    public User? ResponsiblePerson {get;private set;}

    private PlanAssigment() {}
    public PlanAssigment
    (
        Guid planId, Guid facilityId, DateTime passAt,
        Guid passById, Guid responsiblePersonId
    )
    {
        PlanId = planId; FacilityId = facilityId; PassAt = passAt; 
        PassById = passById; ResponsiblePersonId = responsiblePersonId;
    }

    public PlanAssigment (Guid planId, MultipleAssigment dto )
    {
        PlanId = planId; FacilityId = dto.FacilityId; PassAt = dto.PassAt; 
        PassById = dto.PassById; ResponsiblePersonId = dto.ResponsiblePersonId;
    }

    public static List<PlanAssigment> CreateMany(Guid planId, List<MultipleAssigment> dtos) 
    {
        ArgumentNullException.ThrowIfNull(dtos);
        return dtos.Select(dto => new PlanAssigment(planId, dto)).ToList();
    }

    public void SetField(PatchAssigment dto)
    {
        if (dto.PlanId != null) PlanId = (Guid)dto.PlanId;
        if (dto.FacilityId != null) FacilityId = (Guid)dto.FacilityId;
        if (dto.PassAt != null) PassAt = (DateTime)dto.PassAt;
        if (dto.PassById != null) PassById = (Guid)dto.PassById;
        if (dto.ResponsiblePersonId != null) ResponsiblePersonId = (Guid)dto.ResponsiblePersonId;
    }
}