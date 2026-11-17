using Revizor_Service.DTO;

namespace Revizor_Service.Database.Models;

public class Record
{
    public Guid Id {get;private set;}
    public Guid PlanAssigmentId {get;private set;}
    public PlanAssigment PlanAssigment {get;private set;}
    public TimeOnly EndAt {get;private set;}
    private float _passegeWay = 0;
    public float PassageWay 
    {
        get => _passegeWay;
        private set
        {
            if (value <= 0) throw new ArgumentException ($"value {nameof(PassageWay)} must be great than 0");
            _passegeWay = value; 
        }
    }

    private Record() {}
    public Record (Guid planAssigmentId, TimeOnly endAt, float passageWay = 0)
    {
        PlanAssigmentId = planAssigmentId;
        EndAt = endAt;
        PassageWay = passageWay;
    }

    public void SetFields(PatchRecord dto)
    {
        if (dto.PlanAssigmentId != null) PlanAssigmentId = (Guid)dto.PlanAssigmentId;
        if (dto.EndAt != null) EndAt = (TimeOnly)dto.EndAt;
        if (dto.PassageWay != null) PassageWay = (float)dto.PassageWay;
    }
}
