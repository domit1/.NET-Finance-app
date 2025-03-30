using Microsoft.AspNetCore.Identity;

namespace Project_dotnet.Models
{
    public class Users : IdentityUser
    {
        public string FullName { get; set; }
    }
}
