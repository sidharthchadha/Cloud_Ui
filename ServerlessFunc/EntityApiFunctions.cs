

using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using static System.Net.Mime.MediaTypeNames;
using Azure.Storage.Blobs.Models;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ServerlessFunc
{
    public static class EntityApi
    {
        private const string SessionTableName = "SessionTable";
        private const string SubmissionTableName = "SubmissionTable";
        private const string ConnectionName = "AzureWebJobsStorage";
        private const string SessionRoute = "session";
        private const string SubmissionRoute = "submission";
        private const string DllContainerName = "dll";
        private const string AnalysisContainerName = "analysis";
        private const string connectionString = "UseDevelopmentStorage=true";
        private const string AnalysisRoute = "analysis";

        [FunctionName("CreateSession")]
        public static async Task<IActionResult> CreateSession(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = SessionRoute + "/{hostUserName}")] HttpRequest req,
        [Table(SessionTableName, Connection = ConnectionName)] IAsyncCollector<SessionEntity> entityTable,
        string hostUserName)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var requestData = System.Text.Json.JsonSerializer.Deserialize<SessionRequestData>(requestBody);

            // Extract sessionId and tests from the request data
            string sessionId = requestData.SessionId;
            List<string> tests = requestData.Tests;

            SessionEntity value = new SessionEntity(hostUserName, sessionId, tests);
            await entityTable.AddAsync(value);
            return new OkObjectResult(value);
        }

        [FunctionName("GetSessionsbyUsername")]
        public static async Task<IActionResult> GetSessionsByUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = SessionRoute + "/{username}")] HttpRequest req,
        [Table(SessionTableName, SessionEntity.PartitionKeyName, Connection = ConnectionName)] TableClient tableClient,
        string username)
        {
       
            var page = await tableClient.QueryAsync<SessionEntity>(filter: $"HostUserName eq '{username}'").AsPages().FirstAsync();
          
            return new OkObjectResult(page.Values);
        }

        [FunctionName("CreateSubmission")]
        public static async Task<IActionResult> CreateSubmission(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = SubmissionRoute+"/{sessionId}/{username}")] HttpRequest req,
        [Table(SubmissionTableName, Connection = ConnectionName)] IAsyncCollector<SubmissionEntity> entityTable,
        string sessionId,
        string username,
        ILogger log)
        {
            byte[] dllBytes;
            byte[] analysisBytes;
            var streamReader = new StreamReader(req.Body);
     
            var requestBody = await streamReader.ReadToEndAsync();
            var data = JsonSerializer.Deserialize<Submission>(requestBody); 
            dllBytes = data.Dll;
            analysisBytes = data.Analysis;
               
            
            await UploadSubmissionToBlob(sessionId+username,dllBytes, analysisBytes);

            SubmissionEntity value = new SubmissionEntity(sessionId, username);
            await entityTable.AddAsync(value);
            return new OkObjectResult(value);

        }

        [FunctionName("GetSubmissionsbyUsername")]
        public static async Task<IActionResult> GetSubmissionsByUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = SubmissionRoute + "/{sessionId}/{username}")] HttpRequest req,
        string username,string sessionId)
        {
            Submission submission = new Submission();
            submission.Dll = await GetBlobContentAsync(DllContainerName, sessionId + username, connectionString);
            submission.Analysis = await GetBlobContentAsync(AnalysisContainerName, sessionId + username, connectionString);
            return new OkObjectResult(submission);
        }

        [FunctionName("DeleteAllSessions")]
        public static async Task<IActionResult> DeleteAllSessions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = SessionRoute)] HttpRequest req,
        [Table(SessionTableName, ConnectionName)] TableClient entityClient)
        {
            try
            {
                await entityClient.DeleteAsync();
            }
            catch (RequestFailedException e) when (e.Status == 404)
            {
                return new NotFoundResult();
            }

            return new OkResult();
        }

        [FunctionName("GetUsersbyTestname")]
        public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = AnalysisRoute + "/{sessionid}/{testname}")] HttpRequest req,
        string sessionid,
        string testname,
        ILogger log)
        {
            try
            {
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(AnalysisContainerName);

                List<string> results = new List<string>();

                await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
                {
                    if (blobItem.Name.StartsWith(sessionid))
                    {
                        BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);
                        Response<BlobDownloadInfo> response = await blobClient.DownloadAsync();
                        BlobDownloadInfo blobInfo = response.Value;

                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            await blobInfo.Content.CopyToAsync(memoryStream);
                            string jsonContent = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
                            Dictionary<string, int> resultData = JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonContent);

                            if (resultData.ContainsKey(testname) && resultData[testname] == 1)
                            {
                                results.Add(blobItem.Name);
                            }
                        }
                    }
                }

                return new OkObjectResult(results);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while processing the request.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        public static async Task UploadSubmissionToBlob(string blobname, byte[] dll, byte[] analysis)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient DllcontainerClient = blobServiceClient.GetBlobContainerClient(DllContainerName);

            // Create the container if it doesn't exist
            await DllcontainerClient.CreateIfNotExistsAsync();

            // Upload the file to Azure Blob Storage
            BlobClient DllblobClient = DllcontainerClient.GetBlobClient(blobname);
            await DllblobClient.UploadAsync(new MemoryStream(dll), true);

            BlobContainerClient AnalysiscontainerClient = blobServiceClient.GetBlobContainerClient(AnalysisContainerName);

            // Create the container if it doesn't exist
            await AnalysiscontainerClient.CreateIfNotExistsAsync();

            // Upload the file to Azure Blob Storage
            BlobClient AnalysisblobClient = DllcontainerClient.GetBlobClient(blobname);
            await AnalysisblobClient.UploadAsync(new MemoryStream(analysis), true);
        }

        public static async Task<byte[]> GetBlobContentAsync(string containerName, string blobName, string connectionString)
        {
            try
            {
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                // Check if the blob exists
                if (!await blobClient.ExistsAsync())
                {
                    return null; // Or throw an exception, depending on your requirements
                }

                // Download the blob content as a byte array
                Response<BlobDownloadInfo> response = await blobClient.DownloadAsync();
                BlobDownloadInfo blobInfo = response.Value;

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    await blobInfo.Content.CopyToAsync(memoryStream);
                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null; // Or throw an exception
            }
        }
    }
}
