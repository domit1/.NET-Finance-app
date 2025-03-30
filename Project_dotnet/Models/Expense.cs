using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project_dotnet.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public decimal Value { get; set; }

        [Required(ErrorMessage = "Description is required. Please provide a value.")]
        public string? Description { get; set; }

        [Column(TypeName = "text")]
        public DateTime Date { get; set; } = DateTime.Now;

        // Foreign Key to link Expense with a specific user
        public string UserId { get; set; } // Stores the user ID from Identity
    }
}
