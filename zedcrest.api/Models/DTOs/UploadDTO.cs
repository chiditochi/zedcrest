using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace zedcrest.api.Models.DTOs
{
    public class UploadDTO
    {
        public UploadDTO()
        {
            Files = new List<IFormFile>();
        }
        [Required]
        public string UserName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public IEnumerable<IFormFile> Files { get; set; }

    }
}