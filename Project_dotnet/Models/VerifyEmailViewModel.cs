using System.ComponentModel.DataAnnotations;

namespace Project_dotnet.Models
{
    public class VerifyEmailViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; }
    }
}
