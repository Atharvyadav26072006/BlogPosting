using System.ComponentModel.DataAnnotations;

namespace SyncSyntax.Models
{
    public class Post
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public string Author { get; set; } = string.Empty;

        public string? FeatureImagePath { get; set; }

        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        public List<Comment> Comments { get; set; } = new();
    }
}