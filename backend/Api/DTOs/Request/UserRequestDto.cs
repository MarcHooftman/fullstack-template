using System.ComponentModel.DataAnnotations;

namespace Api.DTOs
{
    public class UserCreateDto
    {
        [Required]
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
    }
}