using System.ComponentModel.DataAnnotations;

namespace Revizor_Service.Database.Models;

public class Report
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; }
    public Guid PlanId { get; private set; }
    public Plan? Plan {get;private set;}

    private string _fileName;

    [Required]
    [RegularExpression(@"^[a-zA-Z0-9а-яА-ЯёЁ _\-().]+\.xlsx$",
        ErrorMessage = "FileName содержит недопустимые символы")]
    public string FileName 
    {
        get => _fileName;
        private set
        {
            if (value.Contains("..")) throw new ArgumentException ("filename cannot contain '..'");
            _fileName = value;
        }
    }
    public string FilePath { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Report() {}
    public Report ( Guid userId, Guid planId, string fileName, string filePath)
    {
        UserId = userId; PlanId = planId; FileName = fileName; FilePath = filePath; CreatedAt = DateTime.UtcNow;
    }
}
