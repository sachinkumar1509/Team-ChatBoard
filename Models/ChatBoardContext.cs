using Microsoft.EntityFrameworkCore;
namespace ChatBoard.Models
{
    public class ChatBoardContext : DbContext
    {
        public ChatBoardContext(DbContextOptions<ChatBoardContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<File> Files { get; set; }
        public DbSet<ChatGroup> ChatGroups { get; set; }  // <-- make sure DbSet exists
        public DbSet<ChatGroupMember> ChatGroupMembers { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Optional: explicitly configure primary keys
            modelBuilder.Entity<User>().HasKey(u => u.UserId);
            modelBuilder.Entity<Chat>().HasKey(c => c.ChatId);
            modelBuilder.Entity<File>().HasKey(f => f.FileId);
            modelBuilder.Entity<ChatGroup>().HasKey(g => g.GroupId);
            modelBuilder.Entity<ChatGroupMember>().HasKey(m => m.GroupMemberId);
            modelBuilder.Entity<ChatMessage>().HasKey(m => m.MessageId);
            // Relationships
            modelBuilder.Entity<Chat>().HasOne(c => c.Sender).WithMany().HasForeignKey(c => c.SenderId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Chat>().HasOne(c => c.Receiver).WithMany().HasForeignKey(c => c.ReceiverId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
