using System;
using System.ComponentModel.DataAnnotations;

namespace zedcrest.api.Models.DTOs
{
    public class UploadFileDTO
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }

    }
}