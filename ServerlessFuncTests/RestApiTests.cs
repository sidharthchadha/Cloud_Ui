using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerlessFunc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ServerlessFunc;

namespace ServerlessFunc.Tests
{
    [TestClass()]
    public class RestApiTests
    {
        private const string SubmissionUrl = "http://localhost:7074/api/submission";
        private const string SessionUrl = "http://localhost:7074/api/session";
        private const string AnalysisUrl = "http://localhost:7074/api/analysis";
        private const string ConnectionString = "UseDevelopmentStorage=true";
        private const string AnalysisContainerName = "analysis";
        private UploadApi _uploadClient;
        private DownloadApi _downloadClient;
        private AnalysisApi _analysisClient;
        public RestApiTests()
        {
            _uploadClient = new UploadApi(SessionUrl,SubmissionUrl);
            _downloadClient = new DownloadApi(SessionUrl,SubmissionUrl);
            _analysisClient = new AnalysisApi(AnalysisUrl);
        }

        [TestMethod()]
        public async Task CreateSessionTest()
        {

            List<string> tests = new List<string>();
            tests.Add("Test1");
            tests.Add("Test2");
            SessionEntity? postEntity = await _uploadClient.PostSessionAsync("sessionId", "hostuserName",tests);

            IReadOnlyList<SessionEntity>? getEntity = await _downloadClient.GetSessionsByUserAsync("hostuserName");

            Assert.AreEqual(1, getEntity?.Count);
            for (int i = 0; i < getEntity?.Count; i++)
            {
                Assert.AreEqual(postEntity?.SessionId, getEntity[i].SessionId);
            }
            _downloadClient.DeleteAllSessionsAsync().Wait();
        }

        [TestMethod()]
        public async Task AnalysisTest()
        {
            // create a session, create a submission, check analysis
            List<string> tests = new List<string>();
            tests.Add("Test1");
            SessionEntity? postEntity1 = await _uploadClient.PostSessionAsync("1", "host1", tests);
            byte[] dll = Encoding.ASCII.GetBytes("Hello");
            Dictionary<string,int> map = new Dictionary<string,int>();
            map["Test1"] = 1;

            string jsonString = JsonSerializer.Serialize(map);
            byte[] analysis = Encoding.UTF8.GetBytes(jsonString);

            SubmissionEntity? postEntity2 = await _uploadClient.PostSubmissionAsync("1", "student1",dll,analysis);
            List<String> studentsList = await _analysisClient.GetPassedUsersofTest("1", "Test1");
            Assert.AreEqual(1,studentsList.Count);
            Assert.AreEqual(studentsList[0], "student1");

            // delete the submission,session and also from blob
        }

        [TestMethod()]

        public async Task ContainerBlobTest()
        {
            byte[] Pdf = Encoding.ASCII.GetBytes("DemoText");
            await EntityApi.UploadSubmissionToBlob("name1",Pdf,Pdf);
         
            BlobServiceClient blobServiceClient = new BlobServiceClient(ConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(AnalysisContainerName);
            int blobCount = 0;

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                blobCount++;
            }
            Assert.AreEqual(1, blobCount);
        } 
    }
}