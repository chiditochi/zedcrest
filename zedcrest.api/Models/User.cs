using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace zedcrest.api.Models
{
    public class User
    {
        public User()
        {
            CreatedAt = DateTime.Now;
            UserUploads = new List<Upload>();
        }
        [Key]
        public long UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }


        public virtual ICollection<Upload> UserUploads { get; set; }

    }
}