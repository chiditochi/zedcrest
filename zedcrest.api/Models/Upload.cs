using System;
using System.ComponentModel.DataAnnotations;

namespace zedcrest.api.Models
{
    public class Upload
    {
        public Upload()
        {
            CreatedAt = new DateTime();
        }
        [Key]
        public long UploadId { get; set; }
        public long UserId { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string Token { get; set; }
        public DateTime CreatedAt { get; set; }


        public User User { get; set; }

    }
}