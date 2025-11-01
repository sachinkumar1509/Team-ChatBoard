using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChatBoard.Models
{
    public class ChatGroup
    {
        public int GroupId { get; set; }  // <-- primary key
        public string GroupName { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [ForeignKey(nameof(CreatedBy))]
        public User Creator { get; set; }
        public List<ChatGroupMember> Members { get; set; }
    }
    public class CreateGroupModel
    {
        public string GroupName { get; set; }
        public List<int> MemberIds { get; set; }
    }
}