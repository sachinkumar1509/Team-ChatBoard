using System;

namespace ChatBoard.Models
{
    public class File
    {
        public int FileId { get; set; }
        public int UploadedBy { get; set; }
        public int? ChatId { get; set; }  // optional, related chat
        public string FileName { get; set; }
        public string FileType { get; set; } // document, photo, video
        public long FileSize { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public User Uploader { get; set; }
        public Chat Chat { get; set; }
    }
}
