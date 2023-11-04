using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ServerlessFunc
{
    public class UploadApi
    {
        private readonly HttpClient _entityClient;
        private readonly string _sessionRoute;
        private readonly string _submissionRoute;
        
        private const string connectionString = "UseDevelopmentStorage=true";

        public UploadApi(string sessionRoute, string submissionRoute)
        {
            _entityClient = new HttpClient();
            _sessionRoute = sessionRoute;
            _submissionRoute = submissionRoute;
        }

        public async Task<SessionEntity> PostSessionAsync(string sessionId, string userName, List<string> tests)
        {
            var requestData = new SessionRequestData
            {
                SessionId = sessionId,
                Tests = tests
            };

            using HttpResponseMessage response = await _entityClient.PostAsJsonAsync<SessionRequestData>(_sessionRoute + $"/{userName}", requestData);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            SessionEntity entity = JsonSerializer.Deserialize<SessionEntity>(result, options);
            return entity;
        }

        public async Task<SubmissionEntity> PostSubmissionAsync(string sessionId,string userName, byte[] dll, byte[] analysis)
        {
            var requestData = new Submission
            {
                Dll = dll,
                Analysis = analysis
            };

            using HttpResponseMessage response = await _entityClient.PostAsJsonAsync<Submission>(_submissionRoute + $"/{sessionId}/{userName}", requestData);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            SubmissionEntity entity = JsonSerializer.Deserialize<SubmissionEntity>(result, options);
            return entity;
        }

        
    }
}
