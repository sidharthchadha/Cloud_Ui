

using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private const string InsightsRoute = "insights";

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

            await BlobUtility.UploadSubmissionToBlob(data.SessionId + '/' + data.UserName, dllBytes, connectionString, DllContainerName);

            SubmissionEntity value = new SubmissionEntity(data.SessionId, data.UserName);
            await entityTable.AddAsync(value);
            return new OkObjectResult(value);

        }


        [FunctionName("GetSessionsbyHostname")]
        public static async Task<IActionResult> GetSessionsbyHostname(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = SessionRoute + "/{hostname}")] HttpRequest req,
        [Table(SessionTableName, SessionEntity.PartitionKeyName, Connection = ConnectionName)] TableClient tableClient,
        string hostname, ILogger log)
        {
            try
            {
                var page = await tableClient.QueryAsync<SessionEntity>(filter: $"HostUserName eq '{hostname}'").AsPages().FirstAsync();
                return new OkObjectResult(page.Values);
            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }



        [FunctionName("GetSubmissionbyUsernameAndSessionId")]
        public static async Task<IActionResult> GetSubmissionbyUsernameAndSessionId(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = SubmissionRoute + "/{sessionId}/{username}")] HttpRequest req,
        string username, string sessionId)
        {
            byte[] zippedDlls = await BlobUtility.GetBlobContentAsync(DllContainerName, sessionId + '/' + username, connectionString);
            return new OkObjectResult(zippedDlls);
        }

        [FunctionName("GetAnalysisFilebyUsernameAndSessionId")]
        public static async Task<IActionResult> GetAnalysisFilebyUsernameAndSessionId(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = AnalysisRoute + "/{sessionId}/{username}")] HttpRequest req,
        [Table(AnalysisTableName, AnalysisEntity.PartitionKeyName, Connection = ConnectionName)] TableClient tableClient,
        string username, string sessionId)
        {
            var page = await tableClient.QueryAsync<AnalysisEntity>(filter: $"UserName eq '{username}' and SessionId eq '{sessionId}'").AsPages().FirstAsync();
            return new OkObjectResult(page.Values);
        }

        [FunctionName("GetAnalysisFilebySessionId")]
        public static async Task<IActionResult> GetAnalysisFilebySessionId(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = AnalysisRoute + "/{sessionId}")] HttpRequest req,
        [Table(AnalysisTableName, AnalysisEntity.PartitionKeyName, Connection = ConnectionName)] TableClient tableClient,
        string sessionId)
        {
            var page = await tableClient.QueryAsync<AnalysisEntity>(filter: $"SessionId eq '{sessionId}'").AsPages().FirstAsync();
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

        [FunctionName("CompareTwoSessions")]
        public static async Task<IActionResult> CompareTwoSessions(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = InsightsRoute + "/compare/{sessionId1}/{sessionId2}")] HttpRequest req,
        [Table(AnalysisTableName, AnalysisEntity.PartitionKeyName, Connection = ConnectionName)] TableClient tableClient,
        string sessionId1, string sessionId2)
        {
            var page1 = await tableClient.QueryAsync<AnalysisEntity>(filter: $"SessionId eq '{sessionId1}'").AsPages().FirstAsync();
            var page2 = await tableClient.QueryAsync<AnalysisEntity>(filter: $"SessionId eq '{sessionId2}'").AsPages().FirstAsync();
            List<AnalysisEntity> analysisEntities1 = page1.Values.ToList();
            List<AnalysisEntity> analysisEntities2 = page2.Values.ToList();
            Dictionary<string, int> dictionary1 = new Dictionary<string, int>();
            Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
            foreach (AnalysisEntity analysisEntity in analysisEntities1)
            {
                Dictionary<string, int> temp = InsightsUtility.ConvertAnalysisFileToDictionary(analysisEntity.AnalysisFile);
                foreach (KeyValuePair<string, int> pair in temp)
                {
                    if (dictionary1.ContainsKey(pair.Key))
                    {
                        dictionary1[pair.Key] += pair.Value;
                    }
                    else
                    {
                        dictionary1[pair.Key] = pair.Value;
                    }
                }
            }
            foreach (AnalysisEntity analysisEntity in analysisEntities2)
            {
                Dictionary<string, int> temp = InsightsUtility.ConvertAnalysisFileToDictionary(analysisEntity.AnalysisFile);
                foreach (KeyValuePair<string, int> pair in temp)
                {
                    if (dictionary2.ContainsKey(pair.Key))
                    {
                        dictionary2[pair.Key] += pair.Value;
                    }
                    else
                    {
                        dictionary2[pair.Key] = pair.Value;
                    }
                }
            }
            List<Dictionary<string, int>> list = new List<Dictionary<string, int>>();
            list.Add(dictionary1);
            list.Add(dictionary2);
            return new OkObjectResult(list);
        }

        [FunctionName("GetFailedStudentsGivenTest")]
        public static async Task<IActionResult> GetFailedStudentsGivenTest(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = InsightsRoute + "/failed/{hostname}/{testname}")] HttpRequest req,
        [Table(SessionTableName, SessionEntity.PartitionKeyName, Connection = ConnectionName)] TableClient tableClient1,
        [Table(AnalysisTableName, AnalysisEntity.PartitionKeyName, Connection = ConnectionName)] TableClient tableClient2,
        string hostname, string testname)
        {
            var page = await tableClient1.QueryAsync<SessionEntity>(filter: $"HostUserName eq '{hostname}'").AsPages().FirstAsync();
            List<SessionEntity> sessionEntities = page.Values.ToList();
            List<string> studentList = new List<string>();
            foreach (SessionEntity sessionEntity in sessionEntities)
            {
                if (sessionEntity.Tests == null)
                {
                    continue;
                }
                List<string> tests = InsightsUtility.ByteToList(sessionEntity.Tests);
                if (!tests.Contains(testname))
                {
                    continue;
                }
                var page2 = await tableClient2.QueryAsync<AnalysisEntity>(filter: $"SessionId eq '{sessionEntity.SessionId}'").AsPages().FirstAsync();
                List<AnalysisEntity> analysisEntities = page2.Values.ToList();
                foreach (AnalysisEntity analysisEntity in analysisEntities)
                {
                    Dictionary<string, int> dictionary = InsightsUtility.ConvertAnalysisFileToDictionary(analysisEntity.AnalysisFile);
                    if (dictionary[testname] == 0)
                    {
                        studentList.Add(analysisEntity.UserName);
                    }
                }
            }
            return new OkObjectResult(studentList);
        }
        [FunctionName("RunningAverageOnGivenTest")]
        public static async Task<IActionResult> RunningAverageOnGivenTest(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = InsightsRoute + "/testaverage/{hostname}/{testname}")] HttpRequest req,
        [Table(SessionTableName, SessionEntity.PartitionKeyName, Connection = ConnectionName)] TableClient tableClient1,
        [Table(AnalysisTableName, AnalysisEntity.PartitionKeyName, Connection = ConnectionName)] TableClient tableClient2,
        string hostname, string testname)
        {
            var page = await tableClient1.QueryAsync<SessionEntity>(filter: $"HostUserName eq '{hostname}'").AsPages().FirstAsync();
            List<SessionEntity> sessionEntities = page.Values.ToList();
            List<double> averageList = new List<double>();
            sessionEntities.Sort((x, y) => DateTime.Compare(x.Timestamp.Value.DateTime, y.Timestamp.Value.DateTime));
            foreach (SessionEntity sessionEntity in sessionEntities)
            {
                if (sessionEntity.Tests == null)
                {
                    continue;
                }
                List<string> tests = InsightsUtility.ByteToList(sessionEntity.Tests);
                if (!tests.Contains(testname))
                {
                    continue;
                }
                var page2 = await tableClient2.QueryAsync<AnalysisEntity>(filter: $"SessionId eq '{sessionEntity.SessionId}'").AsPages().FirstAsync();
                List<AnalysisEntity> analysisEntities = page2.Values.ToList();
                double sum = 0;
                foreach (AnalysisEntity analysisEntity in analysisEntities)
                {
                    Dictionary<string, int> dictionary = InsightsUtility.ConvertAnalysisFileToDictionary(analysisEntity.AnalysisFile);
                    sum += dictionary[testname];
                }
                if (analysisEntities.Count == 0)
                {
                    averageList.Add(0);
                }
                else
                {
                    averageList.Add((sum / analysisEntities.Count) * 100);
                }
            }

            return new OkObjectResult(averageList);
        }

        [FunctionName("RunningAverageOnGivenStudent")]
        public static async Task<IActionResult> RunningAverageOnGivenStudent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = InsightsRoute + "/studentaverage/{hostname}/{studentname}")] HttpRequest req,
        [Table(SessionTableName, SessionEntity.PartitionKeyName, Connection = ConnectionName)] TableClient tableClient1,
        [Table(AnalysisTableName, AnalysisEntity.PartitionKeyName, Connection = ConnectionName)] TableClient tableClient2,
        string hostname, string studentname)
        {
            var page = await tableClient1.QueryAsync<SessionEntity>(filter: $"HostUserName eq '{hostname}'").AsPages().FirstAsync();
            List<SessionEntity> sessionEntities = page.Values.ToList();
            List<double> averageList = new List<double>();
            sessionEntities.Sort((x, y) => DateTime.Compare(x.Timestamp.Value.DateTime, y.Timestamp.Value.DateTime));
            foreach (SessionEntity sessionEntity in sessionEntities)
            {
                var page2 = await tableClient2.QueryAsync<AnalysisEntity>(filter: $"SessionId eq '{sessionEntity.SessionId}' and UserName eq '{studentname}'").AsPages().FirstAsync();
                List<AnalysisEntity> analysisEntities = page2.Values.ToList();
                double sum = 0;
                int numberOfTests = 0;
                foreach (AnalysisEntity analysisEntity in analysisEntities)
                {
                    Dictionary<string, int> dictionary = InsightsUtility.ConvertAnalysisFileToDictionary(analysisEntity.AnalysisFile);
                    foreach (KeyValuePair<string, int> pair in dictionary)
                    {
                        sum += pair.Value;
                        numberOfTests++;
                    }
                }
                if (numberOfTests == 0)
                {
                    averageList.Add(0);
                }
                else
                {
                    averageList.Add((sum / numberOfTests) * 100);
                }
            }

            return new OkObjectResult(averageList);
        }

        [FunctionName("RunningAverageAcrossSessoins")]
        public static async Task<IActionResult> RunningAverageAcrossSessoins(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = InsightsRoute + "/sessionsaverage/{hostname}")] HttpRequest req,
        [Table(SessionTableName, SessionEntity.PartitionKeyName, Connection = ConnectionName)] TableClient tableClient1,
        [Table(AnalysisTableName, AnalysisEntity.PartitionKeyName, Connection = ConnectionName)] TableClient tableClient2,
        string hostname)
        {
            var page = await tableClient1.QueryAsync<SessionEntity>(filter: $"HostUserName eq '{hostname}'").AsPages().FirstAsync();
            List<SessionEntity> sessionEntities = page.Values.ToList();
            List<double> averageList = new List<double>();
            sessionEntities.Sort((x, y) => DateTime.Compare(x.Timestamp.Value.DateTime, y.Timestamp.Value.DateTime));
            foreach (SessionEntity sessionEntity in sessionEntities)
            {
                var page2 = await tableClient2.QueryAsync<AnalysisEntity>(filter: $"SessionId eq '{sessionEntity.SessionId}'").AsPages().FirstAsync();
                List<AnalysisEntity> analysisEntities = page2.Values.ToList();
                double sum = 0;
                int numberOfTests = 0;
                foreach (AnalysisEntity analysisEntity in analysisEntities)
                {
                    Dictionary<string, int> dictionary = InsightsUtility.ConvertAnalysisFileToDictionary(analysisEntity.AnalysisFile);
                    foreach (KeyValuePair<string, int> pair in dictionary)
                    {
                        sum += pair.Value;
                        numberOfTests++;
                    }
                }
                if (numberOfTests == 0)
                {
                    averageList.Add(0);
                }
                else
                {
                    averageList.Add((sum / numberOfTests) * 100);
                }
            }

            return new OkObjectResult(averageList);
        }

        [FunctionName("GetUsersWithoutAnalysisGivenSession")]
        public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = InsightsRoute + "/StudentsWithoutAnalysis/{sessionid}")] HttpRequest req,
        [Table(SessionTableName, SessionEntity.PartitionKeyName, Connection = ConnectionName)] TableClient tableClient1,
        [Table(AnalysisTableName, AnalysisEntity.PartitionKeyName, Connection = ConnectionName)] TableClient tableClient2,
        string sessionid,
        ILogger log
            )
        {
            var page = await tableClient1.QueryAsync<SessionEntity>(filter: $"SessionId eq '{sessionid}'").AsPages().FirstAsync();
            List<SessionEntity> sessionEntities = page.Values.ToList();
            SessionEntity sessionEntity = sessionEntities[0];
            List<string> students = InsightsUtility.ByteToList(sessionEntity.Students);
            var page2 = await tableClient2.QueryAsync<AnalysisEntity>(filter: $"SessionId eq '{sessionid}'").AsPages().FirstAsync();
            List<AnalysisEntity> analysisEntities = page2.Values.ToList();
            foreach (AnalysisEntity analysisEntity in analysisEntities)
            {
                students.Remove(analysisEntity.UserName);
            }
            return new OkObjectResult(students);
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
