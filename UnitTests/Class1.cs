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

        public async Task PostAndGetTest()
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
            Assert.Equals(1, sessionEntity.Count);
            Assert.Equals(sessionEntity[0].SessionId, sessionData.SessionId);
            Assert.Equals(sessionEntity[0].Tests, sessionData.Tests);
        }

    }
}
