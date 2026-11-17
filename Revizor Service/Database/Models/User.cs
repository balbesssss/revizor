using System;

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

    private User () {}
    public User 
    (
        string firstName, string secondName, string lastName,
        int roleId, string password
    )
    {
        FirstName = firstName; SecondName = secondName; LastName = lastName;
        RoleId = roleId; Password = password; CreatedAt = DateTime.UtcNow;
    }

    public void DeleteUset()
    {
        if (IsActive == true) throw new ArgumentException("user already deleted");
        IsActive = true;
    }

    public void UpdateRefreshData(string refreshToken, DateTime refreshExpire)
    {
        RefreshToken = refreshToken; RefreshExpireTime = refreshExpire;
    }

    public string GetFullName() => $"{SecondName} {FirstName} {LastName}";

    public string GetFullNameForFileName() => $"{SecondName}_{FirstName}_{LastName}";
}
