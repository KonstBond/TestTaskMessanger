using TestTaskMessanger.Dbl.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace TestTaskMessanger.Dbl.Data
{
    public class MessangerDbContext : DbContext
    {
        private readonly IConfiguration _configuration;
        public MessangerDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public DbSet<UserEntity> Users { get; set; }
        public DbSet<ChatEntity> Chats { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<MessageEntity> Messages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = _configuration.GetConnectionString("default");
                optionsBuilder.UseNpgsql(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Member>().HasKey(member => new {member.UserId, member.ChatId });
            modelBuilder.Entity<MessageEntity>().Property(message => message.SendingTime).HasColumnType("timestamp");
        }
    }
}
