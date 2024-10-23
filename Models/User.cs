using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserSystem.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public string FullName { get; set; }

        [Required(ErrorMessage = "Username is required")]
        public string UserName { get; set; }
        public string? Image { get; set; }
        public required string Role { get; set; } = "User";
        [Required(ErrorMessage = "Password is required")]
        public required string Password { get; set; } = "";
    }
}
