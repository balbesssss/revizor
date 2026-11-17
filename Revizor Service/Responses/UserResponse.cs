namespace Revizor_Service.Responses;

public class UserResponse
{
    public Guid Id {get;set;}
    public required string FullName { get; set; }
    public required string Role {get;set;} 
    public DateTime CreatedAt {get;set;}
}
