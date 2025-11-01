using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChatBoard.Models
{
    [Index(nameof(Email), IsUnique = true)]
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string DisplayName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string? EmpCode { get; set; }
        public string PasswordHash { get; set; }
        public string ProfileImage { get; set; }
        [Required]
        public string Password { get; set; }
    }
    public class ChatUserViewModel
    {
        public int? UserId { get; set; }      // For 1:1 users
        public string DisplayName { get; set; }
        public string EmpCode { get; set; }
        public string Email { get; set; }
        public string ProfileImage { get; set; }

        public int? GroupId { get; set; }     // For group chats
        public string GroupName { get; set; }
        public string GroupImage { get; set; }
        public bool IsGroup { get; set; }
    }

}