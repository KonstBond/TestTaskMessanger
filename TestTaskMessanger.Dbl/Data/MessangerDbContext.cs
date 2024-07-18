using TestTaskMessanger.Dbl.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace TestTaskMessanger.Dbl.Data
{
    public class MessangerDbContext : DbContext
    {
        public MessangerDbContext(DbContextOptions<MessangerDbContext> options)
            : base(options)
        { }

        public DbSet<UserEntity> Users { get; set; }
        public DbSet<ChatEntity> Chats { get; set; }
        public DbSet<MessageEntity> Messages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MessageEntity>().Property(message => message.SendingTime).HasColumnType("timestamp");
        }
    }
}
