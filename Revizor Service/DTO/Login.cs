using System.ComponentModel.DataAnnotations;

namespace Revizor_Service.DTO;

public record Login 
(
    [StringLength(40, MinimumLength = 3)]
    string FirstName,
    
    [StringLength(40, MinimumLength = 3)]
    string SecondName,
    
    [StringLength(40, MinimumLength = 3)]
    string LastName,

    [RegularExpression(@"^[a-zA-Z0-9а-яА-ЯёЁ _\-\.()]{8,30}\z")]
    string Password 
);
