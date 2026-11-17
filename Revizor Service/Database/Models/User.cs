using Revizor_Service.DTO;

namespace Revizor_Service.Database.Models;

public class User
{
    public Guid Id {get;private set;}
    public string FirstName { get; private set;}
    public string SecondName { get; private set;}
    public string LastName { get; private set;}
    public int RoleId {get;private set;} 
    public Role? Role { get; private set; }
    public string Password {get;private set;} 
    public DateTime CreatedAt {get;private set;}
    public string? RefreshToken {get;private set;}
    public DateTime? RefreshExpireTime { get; private set; }
    public bool IsActive {get;private set;} = true;
    public User 
    (
        string firstName, string secondName, string lastName,
        int roleId
    )
    {
        FirstName = firstName; SecondName = secondName; LastName = lastName;
        RoleId = roleId;  CreatedAt = DateTime.UtcNow;
    }

    public void SetPassword (string password) => Password = password;

    public void ChangePassword (string password) => Password = password;

    public void DeleteUser()
    {
        if (IsActive == true) throw new ArgumentException("user already deleted");
        IsActive = true;
    }

    public void UpdateRefreshData(string refreshToken, DateTime refreshExpire)
    {
        RefreshToken = refreshToken; RefreshExpireTime = refreshExpire;
    }

    public void RevokeRefresh()
    {
        RefreshToken = null;
        RefreshExpireTime = null;
    }

    public void SetFields(PatchUser dto)
    {    
        if (dto.FirstName != null) FirstName=dto.FirstName;
        if (dto.SecondName != null) SecondName=dto.SecondName;
        if (dto.LastName != null) LastName=dto.LastName;
        if (dto.RoleId != null) RoleId = (int)dto.RoleId;
    } 

    public string GetFullName() => $"{SecondName} {FirstName} {LastName}";

    public string GetFullNameForFileName() => $"{SecondName}_{FirstName}_{LastName}";
}
