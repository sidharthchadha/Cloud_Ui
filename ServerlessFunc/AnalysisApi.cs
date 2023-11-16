using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ServerlessFunc
{
    public class AnalysisApi
    {
    
        private readonly DownloadApi _downloadApi;
        public AnalysisApi( DownloadApi downloadApi)
        {
            _downloadApi = downloadApi;
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
        public async Task<List<string>> GetFailedStudentsGivenTest(string hostname,string testName)
        {
            IReadOnlyList<SessionEntity> sessionEntities = await _downloadApi.GetSessionsByHostNameAsync(hostname);
            Dictionary<string, int> studentsScore = new Dictionary<string, int>();
            List<string> StudentList = new List<string>();

            foreach (SessionEntity sessionEntity in sessionEntities)
            {
                List<string> SessionTests = ByteToList(sessionEntity.Tests);
               
                if(!SessionTests.Contains(testName))
                {
                    continue;
                }
                // get all the analysis with that sessionId
                IReadOnlyList<AnalysisEntity> analysisEntities = await _downloadApi.GetAnalysisBySessionIdAsync(sessionEntity.SessionId);
                foreach(AnalysisEntity analysisEntity in analysisEntities)
                {
                    byte[] analysisfile = analysisEntity.AnalysisFile;
                    string studentName = analysisEntity.UserName;
                    string jsonString = Encoding.UTF8.GetString(analysisfile);
                    Dictionary<string, int> dictionary = JsonSerializer.Deserialize<Dictionary<string, int>>(jsonString);
                    if (dictionary[testName]>0)
                    {
                        if (studentsScore.ContainsKey(studentName))
                        {
                            studentsScore[studentName] += dictionary[testName];
                        }
                        else
                        {
                            studentsScore[studentName] = dictionary[testName];
                        }
                    }
                    else
                    {
                        if (!studentsScore.ContainsKey(studentName))
                        {
                            studentsScore[studentName] = 0;
                        }
                    }
                }
                foreach(var kvp in studentsScore)
                {
                    if(kvp.Value == 0)
                    {
                        StudentList.Add(kvp.Key);
                    }
                }
            }
            return StudentList;
        } 

    }
}
