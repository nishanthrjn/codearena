// Placeholder for AppDbContext.cs
using CodeArena.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeArena.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> opts) : DbContext(opts)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Snippet> Snippets { get; set; }
    public DbSet<TestCase> TestCases { get; set; }
    public DbSet<ExecutionJob> ExecutionJobs { get; set; }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>(e =>
        {
            e.HasIndex(u => u.GitHubId).IsUnique();
            e.Property(u => u.EncryptedToken).HasColumnType("text");
        });

        mb.Entity<Snippet>(e =>
        {
            e.HasIndex(s => new { s.UserId, s.Slug }).IsUnique();
            e.HasMany(s => s.TestCases).WithOne(t => t.Snippet)
             .HasForeignKey(t => t.SnippetId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<ExecutionJob>(e =>
        {
            e.Property(j => j.TestCasesJson).HasColumnType("jsonb");
            e.HasIndex(j => j.Status);
        });
    }
}