using System;
namespace ChatBoard.Models
{
    public class Chat
    {
        public int ChatId { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string? Message { get; set; }
        public string MessageType { get; set; } = "text"; // text, file, image, video
        public string? FilePath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
        public User Sender { get; set; }
        public User Receiver { get; set; }
    }
    public class SendMessageModel
    {
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Message { get; set; }
    }
}
