using FinanceDashboardApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceDashboardApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasColumnName("id");
            entity.Property(u => u.Username).HasColumnName("username").IsRequired();
            entity.Property(u => u.Email).HasColumnName("email").IsRequired();
            entity.Property(u => u.PasswordHash).HasColumnName("password_hash");
            entity.Property(u => u.GoogleSub).HasColumnName("google_sub");
            entity.Property(u => u.CreatedAt).HasColumnName("created_at");
            entity.Property(u => u.UpdatedAt).HasColumnName("updated_at");

            // Case-insensitive uniqueness is enforced in Postgres via functional
            // indexes on LOWER(username)/LOWER(email) — see SchemaBootstrapper.
            // Repositories query with ToLower() so EF translates to the same
            // LOWER(...) comparison and actually hits those indexes.
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasColumnName("id");
            entity.Property(t => t.UserId).HasColumnName("user_id");
            entity.Property(t => t.Type).HasColumnName("type").IsRequired();
            entity.Property(t => t.Category).HasColumnName("category").IsRequired();
            entity.Property(t => t.Amount).HasColumnName("amount").HasColumnType("numeric(12,2)");
            entity.Property(t => t.TransactionDate).HasColumnName("transaction_date");
            entity.Property(t => t.CreatedAt).HasColumnName("created_at");

            entity.HasOne(t => t.User)
                  .WithMany(u => u.Transactions)
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
