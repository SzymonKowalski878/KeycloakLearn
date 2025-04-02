using KeycloakLearnIdentity.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KeycloakLearnIdentity.Api.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<User> Users { get; set; } = default!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasKey(u => u.Id);
        modelBuilder.Entity<User>().Property(u => u.KeycloakId).IsRequired();
        modelBuilder.Entity<User>().Property(u => u.Username).IsRequired();
        modelBuilder.Entity<User>().Property(u => u.FirstName).IsRequired();
        modelBuilder.Entity<User>().Property(u => u.LastName).IsRequired();
        modelBuilder.Entity<User>().Property(u => u.Email).IsRequired();
        modelBuilder.Entity<User>().Property(u => u.IsEnabled).IsRequired();
        modelBuilder.Entity<User>().Property(u => u.IsEmailConfirmed).IsRequired();
        modelBuilder.Entity<User>().Property(u => u.ConfirmationToken).IsRequired(false);

        modelBuilder.Entity<User>().HasIndex(u => u.KeycloakId).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.ConfirmationToken).IsUnique();
    }
}