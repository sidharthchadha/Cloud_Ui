using ServerlessFunc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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
        private AnalysisApi _analysisClient;

        public Class1()
        {
            _downloadClient = new DownloadApi(sessionUrl,submissionUrl,analysisUrl);
            _uploadClient = new UploadApi(sessionUrl,submissionUrl, analysisUrl);
            _analysisClient = new AnalysisApi(_downloadClient);
        }

        public byte[] ListToByte(List<string> list)
        {
            string concatenatedString = string.Join(Environment.NewLine, list);

            byte[] byteArray = Encoding.UTF8.GetBytes(concatenatedString);

            return byteArray;
        }

        public List<string> ByteToList(byte[] byteArray)
        {
            string concatenatedString = Encoding.UTF8.GetString(byteArray);

            // Split the concatenated string back into individual strings
            string[] stringArray = concatenatedString.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            // Convert the string array to a List<string>
            List<string> stringList = new List<string>(stringArray);

            return stringList;
        }

        public SessionData GetDummySessionData()
        {
            SessionData sessionData = new SessionData();
            sessionData.HostUserName = "name1";
            sessionData.SessionId = "1";
            List<string> Test = new List<string>
            {
                "Test1",
                "Test2"
            };
            sessionData.Tests = ListToByte(Test);
            List<string> Student = new List<string>
            {
                "Student1",
                "Student2"
            };
            sessionData.Students = ListToByte(Student);
            return sessionData;
        }

        public AnalysisData GetDummyAnalysisData(string sessionId,string studentName)
        {
            AnalysisData analysisData = new AnalysisData();
            analysisData.SessionId = sessionId;
            analysisData.UserName = studentName;
            Dictionary<string,int> map = new Dictionary<string,int>();
            map["Test1"] = 0;
            map["Test2"] = 0;
            string json = JsonSerializer.Serialize(map);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            analysisData.AnalysisFile = byteArray;
            return analysisData;
        }

        [TestMethod()]

        public async Task PostAndGetTestSession()
        {
            await _downloadClient.DeleteAllSessionsAsync();
            SessionData sessionData = GetDummySessionData();
            await _uploadClient.PostSessionAsync(sessionData);
            IReadOnlyList<SessionEntity> sessionEntity = await _downloadClient.GetSessionsByHostNameAsync("name1");
            Assert.AreEqual(1, sessionEntity.Count);
            CollectionAssert.AreEqual(sessionData.Students, sessionEntity[0].Students, "Students list mismatch");
            CollectionAssert.AreEqual(sessionData.Tests, sessionEntity[0].Tests, "Tests list mismatch");

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

        [TestMethod()]
        public async Task AnalysisTest()
        {
            SessionData sessionData = GetDummySessionData();
            await _uploadClient.PostSessionAsync(sessionData);
            
            AnalysisData analysisData1 = GetDummyAnalysisData("1", "Student1");
            AnalysisData analysisData2 = GetDummyAnalysisData("1", "Student2");
            await _uploadClient.PostAnalysisAsync(analysisData1);
            await _uploadClient.PostAnalysisAsync(analysisData2);
            List<string> students = await _analysisClient.GetFailedStudentsGivenTest("name1", "Test1");
            students.Sort();
            List<string> expectedStudents = new List<string>
            {
                "Student1",
                "Student2"
            };
            CollectionAssert.AreEqual(expectedStudents,students);
            await _downloadClient.DeleteAllAnalysisAsync();
            await _downloadClient.DeleteAllSessionsAsync();
        }
    }
}
