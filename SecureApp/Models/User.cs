using System.ComponentModel.DataAnnotations;

namespace SecureApp.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public int RoleId { get; set; }

        public Role Role { get; set; }

        public int FailedLoginAttempts { get; set; } = 0; 
    }
}
