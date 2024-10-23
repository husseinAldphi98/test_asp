using System.ComponentModel.DataAnnotations;

namespace UserSystem.Dto.user
{
    public class UserDto
    {
        public Guid Id { get; set; }

        public string FullName { get; set; }

        [Required]
        public string UserName { get; set; }
        public string Image { get; set; }

        public required string Role { get; set; }

        public required string Password { get; set; }
    }
}
