using System;
using System.ComponentModel.DataAnnotations;

namespace zedcrest.api.Models.DTOs
{
    public class VerifyUploadRequestDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Token { get; set; }

    }
}