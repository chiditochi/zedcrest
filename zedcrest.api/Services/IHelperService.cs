
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using zedcrest.api.Models;
using zedcrest.api.Models.DTOs;
using zedcrest.api.Models.Shared;

namespace zedcrest.api.Services
{
    public interface IHelperService
    {
        Task<ActionResponseEntity<UploadFileDTO>> ProcessFiles(IFormFileCollection files);

        Task<ActionResponseEntity<string>> PersistUploads(List<UploadFileDTO> uploadedFiles, User newUser);

        Task PublishJob(AppJobEnum jobTitle, string token);
        Task JobSubscriber(AppJobEnum jobTitle);
        Task<VerifyUploadResponseDTO> GetUploadDetails(VerifyUploadRequestDTO requestData, string link);
    }
}