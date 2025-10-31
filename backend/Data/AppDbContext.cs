using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<MessageTopic> MessageTopics => Set<MessageTopic>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasIndex(x => new { x.Email, x.Phone }).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(32).IsRequired();
        });

        modelBuilder.Entity<MessageTopic>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasData(
                new MessageTopic { Id = 1, Name = "Общий вопрос" },
                new MessageTopic { Id = 2, Name = "Предложение" },
                new MessageTopic { Id = 3, Name = "Жалоба" }
            );
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.Property(x => x.Text).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.Contact)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Topic)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.TopicId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
