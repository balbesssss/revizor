namespace Revizor_Service.Responses;

public class DataCurrentPlanOfAssigment
{
    public required string NamePlan { get; set; }
    public required List<AssigmentItem> Assigments { get; set; }
}
