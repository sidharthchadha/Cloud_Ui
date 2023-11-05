using ServerlessFunc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace UnitTests
{
    [TestClass()]
    public class Class1
    {
        private string analysisUrl = "http://localhost:7074/api/analysis";
        private string submissionUrl = "http://localhost:7074/api/submission";
        private string sessionUrl = "http://localhost:7074/api/session";
        private DownloadApi _downloadClient;
        private UploadApi _uploadClient;

        public Class1()
        {
            _downloadClient = new DownloadApi(sessionUrl,submissionUrl,analysisUrl);
            _uploadClient = new UploadApi(sessionUrl,submissionUrl, analysisUrl);
        }
        [TestMethod()]

        public async Task PostAndGetTestSession()
        {
            await _downloadClient.DeleteAllSessionsAsync();
            
            SessionData sessionData = new SessionData();
            sessionData.HostUserName = "name1";
            sessionData.SessionId = "1";
            List<string> Test = new List<string>
            {
                "Test1",
                "Test2"
            };
            sessionData.Tests = Test;
            List<string> Student = new List<string>
            {
                "Student1",
                "Student2"
            };
            sessionData.Students = Student;

            await _uploadClient.PostSessionAsync(sessionData);
            IReadOnlyList<SessionEntity> sessionEntity = await _downloadClient.GetSessionsByHostNameAsync("name1");
            Assert.AreEqual(1, sessionEntity.Count);
            Assert.AreEqual(sessionEntity[0].SessionId, sessionData.SessionId);
            Assert.AreEqual(sessionEntity[0].Tests, sessionData.Tests);
            
        }
        [TestMethod()]
        public async Task PostAndGetTestSubmission()
        {
            
            SubmissionData submission = new SubmissionData();
            submission.SessionId = "1";
            submission.UserName = "Student1";
            submission.ZippedDllFiles = Encoding.ASCII.GetBytes("demotext");
            SubmissionEntity postEntity = await _uploadClient.PostSubmissionAsync(submission);
            
            byte[] submissionFile = await _downloadClient.GetSubmissionByUserNameAndSessionIdAsync(submission.UserName, submission.SessionId);
            string text = Encoding.ASCII.GetString(submissionFile);
            await _downloadClient.DeleteAllSubmissionsAsync();
            Assert.AreEqual(text, "demotext");
          
        }
        [TestMethod()]
        public async Task PostAndGetTestAnalysis()
        {
            AnalysisData analysis = new AnalysisData();
            analysis.SessionId = "1";
            analysis.UserName = "Student1";
            analysis.AnalysisFile = Encoding.ASCII.GetBytes("demotext");
            AnalysisEntity postEntity = await _uploadClient.PostAnalysisAsync(analysis);
            IReadOnlyList<AnalysisEntity> entities = await _downloadClient.GetAnalysisByUserNameAndSessionIdAsync(analysis.UserName, analysis.SessionId);
            await _downloadClient.DeleteAllAnalysisAsync();
            Assert.AreEqual(1, entities.Count);
            Assert.AreEqual(entities[0].SessionId, postEntity.SessionId);
            Assert.AreEqual(entities[0].UserName, postEntity.UserName);
            string text = Encoding.ASCII.GetString(entities[0].AnalysisFile);
            Assert.AreEqual("demotext", text);
            
        }
    }
}
