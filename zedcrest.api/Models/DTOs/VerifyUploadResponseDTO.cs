using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace zedcrest.api.Models.DTOs
{
    public class VerifyUploadResponseDTO
    {
        public VerifyUploadResponseDTO()
        {
            Downloads = new List<DownloadFile>();
        }
        public string UserName { get; set; }
        public string Email { get; set; }
        public int UploadCount { get; set; }

        public IEnumerable<DownloadFile> Downloads { get; set; }

    }

    public class DownloadFile 
    {
        public long FileSize { get; set; }
        public string DownloadLink { get; set; }
    }
}