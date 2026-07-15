using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SyncSyntax.Models;

namespace SyncSyntax.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Post> Posts { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Comment> Comments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Force correct Identity column lengths for SQL Server
            modelBuilder.Entity<IdentityUser>(entity =>
            {
                entity.Property(x => x.Id)
                    .HasMaxLength(450);

                entity.Property(x => x.NormalizedUserName)
                    .HasMaxLength(256);

                entity.Property(x => x.NormalizedEmail)
                    .HasMaxLength(256);
            });

            modelBuilder.Entity<IdentityRole>(entity =>
            {
                entity.Property(x => x.Id)
                    .HasMaxLength(450);

                entity.Property(x => x.NormalizedName)
                    .HasMaxLength(256);
            });

            modelBuilder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.Property(x => x.UserId)
                    .HasMaxLength(450);

                entity.Property(x => x.RoleId)
                    .HasMaxLength(450);
            });

            modelBuilder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.Property(x => x.UserId)
                    .HasMaxLength(450);
            });

            modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.Property(x => x.LoginProvider)
                    .HasMaxLength(128);

                entity.Property(x => x.ProviderKey)
                    .HasMaxLength(128);

                entity.Property(x => x.UserId)
                    .HasMaxLength(450);
            });

            modelBuilder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.Property(x => x.UserId)
                    .HasMaxLength(450);

                entity.Property(x => x.LoginProvider)
                    .HasMaxLength(128);

                entity.Property(x => x.Name)
                    .HasMaxLength(128);
            });

            modelBuilder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.Property(x => x.RoleId)
                    .HasMaxLength(450);
            });

            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id = 1,
                    Name = "Technology"
                },
                new Category
                {
                    Id = 2,
                    Name = "Health"
                },
                new Category
                {
                    Id = 3,
                    Name = "Lifestyle"
                }
            );

            modelBuilder.Entity<Post>().HasData(
                new Post
                {
                    Id = 1,
                    Title = "Tech Post 1",
                    Content = "Content of Tech Post 1",
                    Author = "John Doe",
                    PublishedDate = new DateTime(
                             2026, 7, 15, 0, 0, 0,
                                   DateTimeKind.Utc),
                    CategoryId = 1,
                    FeatureImagePath = "tech_image.jpg"
                },
                new Post
                {
                    Id = 2,
                    Title = "Health Post 1",
                    Content = "Content of Health Post 1",
                    Author = "Jane Doe",
                    PublishedDate = new DateTime(
    2026, 7, 15, 0, 0, 0,
    DateTimeKind.Utc),
                    CategoryId = 2,
                    FeatureImagePath = "health_image.jpg"
                },
                new Post
                {
                    Id = 3,
                    Title = "Lifestyle Post 1",
                    Content = "Content of Lifestyle Post 1",
                    Author = "Alex Smith",
                    PublishedDate = new DateTime(
    2026, 7, 15, 0, 0, 0,
    DateTimeKind.Utc),
                    CategoryId = 3,
                    FeatureImagePath = "lifestyle_image.jpg"
                }
            );
        }
    }
}