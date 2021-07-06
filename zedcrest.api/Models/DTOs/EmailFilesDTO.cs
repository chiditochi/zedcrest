using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace zedcrest.api.Models.DTOs
{
    public class EmailFilesDTO
    {
        public EmailFilesDTO()
        {
            FilesNames = new List<string>();
        }
        public string UserName { get; set; }
        public string Email { get; set; }
        public IEnumerable<string> FilesNames { get; set; }

    }
}