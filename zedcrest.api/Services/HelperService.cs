using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using zedcrest.api.Data;
using zedcrest.api.Models;
using zedcrest.api.Models.DTOs;
using zedcrest.api.Models.Shared;

namespace zedcrest.api.Services
{
    public class HelperService : IHelperService
    {
        private readonly ILogger<HelperService> _logger;
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public HelperService(
            ILogger<HelperService> logger,
            AppDbContext context,
            IConfiguration config,
            IEmailService emailService

            )
        {
            _logger = logger;
            _context = context;
            _config = config;
            _emailService = emailService;
        }

        public async Task<ActionResponseEntity<string>> PersistUploads(List<UploadFileDTO> uploadedFiles, User newUser)
        {
            var result = new ActionResponseEntity<string>();
            try
            {
                string token = await GenerateUploadToken();
                List<Upload> files = new List<Upload>();
                foreach (var itemFile in uploadedFiles)
                {
                    files.Add(new Upload
                    {
                        UserId = newUser.UserId,
                        FileName = itemFile.FileName,
                        FileSize = itemFile.FileSize,
                        Token = token
                    });
                }
                _context.Uploads.AddRange(files);
                await _context.SaveChangesAsync();
                result.Status = true;
                result.Data.Add(token);

            }
            catch (System.Exception e)
            {
                _logger.LogError($"PersistUploads: {e.Message}");
                result.Status = false;
                await RemoveNewUser(newUser);
            }
            return result;
        }

        private async Task RemoveNewUser(User newUser)
        {
            _context.Entry<User>(newUser).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
            await _context.SaveChangesAsync();
        }

        private Task<string> GenerateUploadToken()
        {
            string result = string.Empty;
            try
            {
                var newToken = Guid.NewGuid().ToString();
                var tokenExists = _context.Uploads.Where(u => u.Token.Equals(newToken)).Any();
                while (tokenExists)
                {
                    newToken = Guid.NewGuid().ToString();
                    tokenExists = _context.Uploads.Where(u => u.Token.Equals(newToken)).Any();
                }
                result = newToken;
            }
            catch (System.Exception e)
            {
                _logger.LogError($"GenerateUploadToken: {e.Message}");
            }
            return Task.FromResult(result);
        }

        public async Task<ActionResponseEntity<UploadFileDTO>> ProcessFiles(IFormFileCollection files)
        {
            var result = new ActionResponseEntity<UploadFileDTO>();
            try
            {
                bool filesLessThanMaxSize = await EnsureFilesLessThanMaxSize(files);
                if (filesLessThanMaxSize) throw new Exception($"One or more uploaded files is > 2MB");
                result = await UploadFiles(files, result);
                result.Status = true;
            }
            catch (System.Exception e)
            {
                _logger.LogError($"ProcessFiles: Error processing file, {e.Message} - {e.StackTrace}");
                result.Message = e.Message;
                result.Status = false;
            }
            return result;
        }

        private async Task<ActionResponseEntity<UploadFileDTO>> UploadFiles(IFormFileCollection files, ActionResponseEntity<UploadFileDTO> result)
        {
            foreach (var itemFile in files)
            {
                var folderName = "Uploads";
                var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
                var fileName = ContentDispositionHeaderValue.Parse(itemFile.ContentDisposition).FileName.Trim('"');
                var fullPath = Path.Combine(pathToSave, fileName);
                var dbPath = Path.Combine(folderName, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await itemFile.CopyToAsync(stream);
                }
                result.Data.Add(new UploadFileDTO { FileName = fileName, FileSize = itemFile.Length });
            }
            return result;
        }

        private Task<bool> EnsureFilesLessThanMaxSize(IFormFileCollection files)
        {
            var result = false;
            var maxFileSize = Convert.ToInt64(_config["AppDetails:UploadMaxFileSize"]);
            result = files.Where(f => f.Length > maxFileSize).Any();
            return Task.FromResult(result);
        }

        public async Task PublishJob(AppJobEnum jobTitle, string token)
        {
            try
            {
                string job = jobTitle.ToString();
                var factory = new ConnectionFactory() { HostName = "localhost" };
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(exchange: job, type: ExchangeType.Fanout);

                    var message = token;
                    var body = Encoding.UTF8.GetBytes(message);
                    channel.BasicPublish(exchange: job,
                                        routingKey: job,
                                        basicProperties: null,
                                        body: body);
                    _logger.LogInformation($"Published {message} data to {job} channel");
                }
                await ProcessEmailJob(token);

            }
            catch (System.Exception e)
            {
                _logger.LogError($"PublishJob: {e.Message}, {e.StackTrace}");
            }

            // return Task.CompletedTask;
        }
        public Task JobSubscriber(AppJobEnum jobTitle)
        {
            //await Task.Delay(2000);
            try
            {

                string job = jobTitle.ToString();

                var factory = new ConnectionFactory() { HostName = "localhost" };
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(exchange: job, type: ExchangeType.Fanout);

                    var queueName = channel.QueueDeclare().QueueName;
                    channel.QueueBind(queue: queueName,
                                    exchange: job,
                                    routingKey: job);

                    _logger.LogInformation("Receiver Waiting for EmailJob...");

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += async (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        await ProcessEmailJob(message);
                        _logger.LogInformation($"Job with Token {message} processed");
                    };
                    channel.BasicConsume(queue: queueName,
                                        autoAck: true,
                                        consumer: consumer);
                }

            }
            catch (System.Exception e)
            {
                _logger.LogError($"PublishJob: {e.Message}, {e.StackTrace}");
            }

            return Task.CompletedTask;
        }

        private async Task ProcessEmailJob(string message)
        {
            try
            {
                EmailFilesDTO emailObj = await GetJobToEmail(message);
                await SendEmailToRecipient(emailObj);
                _logger.LogInformation($"Email Job for Toke {message} was Successful!");
            }
            catch (System.Exception e)
            {
                _logger.LogError($"PublishJob: {e.Message}, {e.StackTrace}");
            }

        }

        private Task SendEmailToRecipient(EmailFilesDTO emailObj)
        {
            var emailMessage = new EmailMessage(new string[] { emailObj.Email }, "Data Uploaded to Zedcrest UploadData App", $"<p>Dear {emailObj.UserName},</p><p>Please find the uploaded attachments.</p>", emailObj.FilesNames);
            _emailService.SendEmail(emailMessage);
            return Task.CompletedTask;
        }

        private Task<EmailFilesDTO> GetJobToEmail(string message)
        {
            var result = new EmailFilesDTO();
            var uploadInstance = _context.Uploads.Include(u => u.User)
                    .AsEnumerable()
                    .Where(u => u.Token == message)
                    .ToList();
            if (uploadInstance == null) throw new Exception($"Token {message} does not exist");
            result.UserName = uploadInstance.First().User.UserName;
            result.Email = uploadInstance.First().User.Email;
            result.FilesNames = uploadInstance.Select(u => u.FileName).ToList();
            return Task.FromResult(result);
        }

        public Task<VerifyUploadResponseDTO> GetUploadDetails(VerifyUploadRequestDTO requestData, string link)
        {
            var result = new VerifyUploadResponseDTO();
            var uploads = _context.Uploads.Include(u => u.User)
                                  .AsEnumerable()
                                  .Where(u => u.User.Email == requestData.Email && u.Token == requestData.Token)
                                  .ToList();
            if (uploads.Count() == 0) throw new Exception($"No upload found for submitted data");
            var user = uploads.First().User;
            result.UserName = user.UserName;
            result.Email = user.Email;
            result.UploadCount = uploads.Count();
            result.Downloads = uploads.Select(u => new DownloadFile
            {
                FileSize = u.FileSize,
                DownloadLink = $"{link}{u.FileName}"

            }).ToList();

            return Task.FromResult(result);
        }


    }
}