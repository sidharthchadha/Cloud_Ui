

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
        private const string AnalysisTableName = "AnalysisTable";
        private const string ConnectionName = "AzureWebJobsStorage";
        private const string SessionRoute = "session";
        private const string SubmissionRoute = "submission";
        private const string DllContainerName = "dll";
        private const string connectionString = "UseDevelopmentStorage=true";
        private const string AnalysisRoute = "analysis";

        [FunctionName("CreateSessionEntity")]
        public static async Task<IActionResult> CreateSessionEntity(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = SessionRoute)] HttpRequest req,
        [Table(SessionTableName, Connection = ConnectionName)] IAsyncCollector<SessionEntity> entityTable,
        ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            SessionData requestData = System.Text.Json.JsonSerializer.Deserialize<SessionData>(requestBody);
            SessionEntity value = new SessionEntity(requestData);
            await entityTable.AddAsync(value);
            return new OkObjectResult(value);
        }

        [FunctionName("CreateAnalysisEntity")]
        public static async Task<IActionResult> CreateAnalysisEntity(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = AnalysisRoute)] HttpRequest req,
        [Table(AnalysisTableName, Connection = ConnectionName)] IAsyncCollector<AnalysisEntity> entityTable,
        ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            AnalysisData requestData = System.Text.Json.JsonSerializer.Deserialize<AnalysisData>(requestBody);
            AnalysisEntity value = new AnalysisEntity(requestData);
            await entityTable.AddAsync(value);
            return new OkObjectResult(value);
        }

        [FunctionName("CreateSubmissionEntity")]
        public static async Task<IActionResult> CreateSubmissionEntity(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = SubmissionRoute)] HttpRequest req,
        [Table(SubmissionTableName, Connection = ConnectionName)] IAsyncCollector<SubmissionEntity> entityTable,
        ILogger log)
        {
            byte[] dllBytes;
           
            var streamReader = new StreamReader(req.Body);

            var requestBody = await streamReader.ReadToEndAsync();
            SubmissionData data = JsonSerializer.Deserialize<SubmissionData>(requestBody);
            dllBytes = data.ZippedDllFiles;
            
            await BlobUtility.UploadSubmissionToBlob(data.SessionId +'/'+ data.UserName, dllBytes,connectionString,DllContainerName);

            SubmissionEntity value = new SubmissionEntity(data.SessionId, data.UserName);
            await entityTable.AddAsync(value);
            return new OkObjectResult(value);

        }


        [FunctionName("GetSessionsbyHostname")]
        public static async Task<IActionResult> GetSessionsbyHostname(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = SessionRoute + "/{hostname}")] HttpRequest req,
        [Table(SessionTableName, SessionEntity.PartitionKeyName, Connection = ConnectionName)] TableClient tableClient,
        string hostname,ILogger log)
        {
            try
            {
                var page = await tableClient.QueryAsync<SessionEntity>(filter: $"HostUserName eq '{hostname}'").AsPages().FirstAsync();
                return new OkObjectResult(page.Values);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while processing the request.");
                // Log the error message
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }



        [FunctionName("GetSubmissionbyUsernameAndSessionId")]
        public static async Task<IActionResult> GetSubmissionbyUsernameAndSessionId(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = SubmissionRoute + "/{sessionId}/{username}")] HttpRequest req,
        string username,string sessionId)
        {
            byte[] zippedDlls = await BlobUtility.GetBlobContentAsync(DllContainerName, sessionId + '/' + username, connectionString);
            return new OkObjectResult(zippedDlls);
        }

        [FunctionName("GetAnalysisFilebyUsernameAndSessionId")]
        public static async Task<IActionResult> GetAnalysisFilebyUsernameAndSessionId(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = AnalysisRoute + "/{sessionId}/{username}")] HttpRequest req,
        [Table(AnalysisTableName, AnalysisEntity.PartitionKeyName, Connection = ConnectionName)] TableClient tableClient,
        string username,string sessionId)
        { 
            var page = await tableClient.QueryAsync<AnalysisEntity>(filter: $"UserName eq '{username}' and SessionId eq '{sessionId}'").AsPages().FirstAsync();
            return new OkObjectResult(page.Values);
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

        [FunctionName("DeleteAllSubmissions")]
        public static async Task<IActionResult> DeleteAllSubmissions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = SubmissionRoute)] HttpRequest req,
        [Table(SubmissionTableName, ConnectionName)] TableClient entityClient)
        {
            try
            {
                await BlobUtility.DeleteContainer(DllContainerName, connectionString);
                await entityClient.DeleteAsync();
            }
            catch (RequestFailedException e) when (e.Status == 404)
            {
                return new NotFoundResult();
            }

            return new OkResult();
        }

        [FunctionName("DeleteAllAnalysis")]
        public static async Task<IActionResult> DeleteAllAnalysis(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = AnalysisRoute)] HttpRequest req,
        [Table(AnalysisTableName, ConnectionName)] TableClient entityClient)
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







        /*[FunctionName("GetUsersbyTestname")]
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
        }*/

        

        
    }
}
