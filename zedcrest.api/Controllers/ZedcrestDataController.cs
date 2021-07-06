using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using zedcrest.api.Data;
using zedcrest.api.Models;
using zedcrest.api.Models.DTOs;
using zedcrest.api.Models.Shared;
using zedcrest.api.Services;

namespace zedcrest.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ZedcrestDataController : ControllerBase
    {

        private readonly ILogger<ZedcrestDataController> _logger;
        private readonly AppDbContext _context;
        private readonly IHelperService _helperService;

        public ZedcrestDataController(
            ILogger<ZedcrestDataController> logger,
            AppDbContext context,
            IHelperService helperService
            )
        {
            _logger = logger;
            _context = context;
            _helperService = helperService;
        }

        [HttpPost("UploadData")]
        //[Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadData([FromForm]UploadDTO upload)
        {
            string token = string.Empty;
            try
            {
                var formCollection = await Request.ReadFormAsync();
                if (!ModelState.IsValid) throw new Exception($"Please provide the required data");
                List<UploadFileDTO> uploadedFiles = new List<UploadFileDTO>();
                if (formCollection.Files.Count() > 0)
                {
                    var uploadedFilesResult = await _helperService.ProcessFiles(formCollection.Files);
                    if (!uploadedFilesResult.Status) throw new Exception("Unable to process uploaded Files. Ensure each file is less than 2MB");
                    uploadedFiles = uploadedFilesResult.Data;
                }

                var newUser = new User();
                newUser.UserName = formCollection["username"];
                newUser.Email = formCollection["email"];
                //persist User
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                //process and persist Uploads
                var persitUploadsResult = await _helperService.PersistUploads(uploadedFiles, newUser);
                if (!persitUploadsResult.Status) throw new Exception("Unable to process uploaded Files");
                token = persitUploadsResult.Data.FirstOrDefault();
                //register Job Processor
                await _helperService.JobSubscriber(AppJobEnum.EmailJob);
                //add rabbitmq logic or method calls
                await _helperService.PublishJob(AppJobEnum.EmailJob, token);

            }
            catch (System.Exception e)
            {
                _logger.LogError($"UploadData: {e.Message} - {e.StackTrace}");
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
            return Ok(new { UploadToken = token });
        }


        [HttpPost("VerifyUploadData")]
        public async Task<IActionResult> VerifyUploadData([FromBody]VerifyUploadRequestDTO requestData)
        {
            VerifyUploadResponseDTO result = new VerifyUploadResponseDTO();
            //expects token and email
            try
            {
                if (!ModelState.IsValid) throw new Exception($"Please provide the required parameters");
                var link = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.ToString()}/api/ZedcrestData/Uploads/";
                result = await _helperService.GetUploadDetails(requestData, link);

            }
            catch (System.Exception e)
            {
                _logger.LogError($"UploadData: {e.Message} - {e.StackTrace}");
                ModelState.AddModelError("Error", e.Message);
                return BadRequest(ModelState);
            }
            return Ok(result);
        }

        [HttpGet("Uploads/{fileName}")]
        public async Task<ActionResult> DownloadFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) throw new Exception($"Please provide fileName");
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), @"Uploads", fileName);
            var fileContentType = string.Empty;
            byte[] bytes = null;
            try
            {
                var provider = new FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(filePath, out var contentType))
                {
                    contentType = "application/octet-stream";
                }
                fileContentType = contentType;
                bytes = await System.IO.File.ReadAllBytesAsync(filePath);

            }
            catch (System.Exception e)
            {
                return BadRequest(e.Message);
            }
            return File(bytes, fileContentType, Path.GetFileName(filePath));
        }




    }
}
