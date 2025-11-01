using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatBoard.Models
{
    public class ChatMessage
    {
        [Key] // ✅ Explicitly tell EF this is the primary key
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MessageId { get; set; }

        [ForeignKey("ChatGroup")]
        public int GroupId { get; set; }

        [ForeignKey("User")]
        public int SenderId { get; set; }

        //[Required]
        [MaxLength(500)]
        public string? MessageText { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;

        public bool IsSystemMessage { get; set; } = false;
        public string? MessageType { get; set; } // text or file
        public string? FilePath { get; set; }

        // Navigation properties (optional)
        public ChatGroup ChatGroup { get; set; }
        public User User { get; set; }
    }
    public class SendGroupMessageModel
    {
        public int GroupId { get; set; }
        public int SenderId { get; set; }
        public string MessageText { get; set; }
    }

}
