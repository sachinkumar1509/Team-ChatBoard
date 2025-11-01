using System;

namespace ChatBoard.Models
{
    public class ChatGroupMember
    {
        public int GroupMemberId { get; set; }
        public int GroupId { get; set; }
        public int UserId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.Now;

        public ChatGroup Group { get; set; }
        public User User { get; set; }
    }
}
