using System.ComponentModel.DataAnnotations;

namespace SyncSyntax.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "The category name is required ")]
        [MaxLength(100, ErrorMessage = " Category name Cannot exceed 200 charaters")]

        public string Name { get; set; }

        public string? Description { get; set; }

        public ICollection<Post> Posts { get; set; }
    }
}