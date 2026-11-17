using Revizor_Service.DTO;

namespace Revizor_Service.Database.Models;

public class Plan
{
    public Guid Id {get;private set;}
    private string _name;
    public string Name 
    {
        get => _name;
        private set
        {
            if (value.Length < 10) throw new ArgumentException("lenght of name must be great than 10");
            _name = value;
        }
    }
    public DateTime CreatedAt {get;private set;}
    public Guid CreatedById {get;private set;}
    public User CreatedBy {get;private set;}
    public DateOnly PeriodStart {get;private set;}
    public DateOnly? PeriodEnd {get;private set;}
    public bool IsArchived {get;private set;} = false;

    private Plan() {}

    public Plan
    (
        string name,
        Guid createdBy,
        DateOnly periodStart,
        DateOnly? periodEnd = null
    )
    {
        Name = name; CreatedAt = DateTime.UtcNow; CreatedById = createdBy;
        PeriodStart = periodStart;
        if ( periodEnd == null ) PeriodEnd = periodStart.AddMonths(1);
        else { PeriodEnd = periodEnd; }
    }

    public void Archived()
    {
        if (IsArchived == true) throw new ArgumentException($"plan with {Id} already archived");
        IsArchived = true;
    }
    public void Unarchived()
    {
        if (IsArchived == false) throw new ArgumentException($"plan with {Id} already unarchived");
        IsArchived = false;
    }
    public void SetFields(PatchPlan dto)
    {
        if (dto.Name is not null) Name = dto.Name;
        if (dto.PeriodStart is not null) PeriodStart = dto.PeriodStart.Value;
        if (dto.PeriodEnd is not null) PeriodEnd = dto.PeriodEnd;
    }
}
