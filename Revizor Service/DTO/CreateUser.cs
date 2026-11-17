using System.ComponentModel.DataAnnotations;

namespace Revizor_Service.DTO;

public record CreateUser
(
    [StringLength(40, MinimumLength = 3)]
    string FirstName,
    
    [StringLength(40, MinimumLength = 3)]
    string SecondName,
    
    [StringLength(40, MinimumLength = 3)]
    string LastName,
    
    [Range(0, int.MaxValue)]
    int RoleId, 

    [RegularExpression(@"^[a-zA-Z0-9а-яА-ЯёЁ _\-\.()]{8,30}\z")]
    string Password 
);
