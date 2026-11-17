using System.ComponentModel.DataAnnotations;

namespace Revizor_Service.DTO;

public record CreateFacility
(
    [Required, StringLength(100, MinimumLength = 3)]
    string Title,

    [Required, StringLength(150, MinimumLength = 10)]
    string Location
);
