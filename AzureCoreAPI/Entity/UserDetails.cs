using System.ComponentModel.DataAnnotations;

namespace AzureCoreAPI.Entity
{
    public class UserDetails
    {
        [Key]
        public int UserId { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
