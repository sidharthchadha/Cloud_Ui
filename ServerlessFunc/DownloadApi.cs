using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ServerlessFunc
{
    public class DownloadApi
    {
        private readonly HttpClient _entityClient;
        private readonly string _sessionRoute;
        private readonly string _submissionRoute;

        public DownloadApi(string sessionRoute, string submissionRoute)
        {
            _entityClient = new HttpClient();
            _sessionRoute = sessionRoute;
            _submissionRoute = submissionRoute;
        }

        public async Task<IReadOnlyList<SessionEntity>> GetSessionsByUserAsync(string hostUsername)
        {
            var response = await _entityClient.GetAsync(_sessionRoute + $"/{hostUsername}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,

            };

            IReadOnlyList<SessionEntity> entities = JsonSerializer.Deserialize<IReadOnlyList<SessionEntity>>(result, options);
            return entities;
        }

        public async Task<Submission> GetSubmissionByUserAsync(string username,string sessionId)
        {
            var response = await _entityClient.GetAsync(_submissionRoute + $"/{sessionId}/{username}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,

            };

            Submission submission = JsonSerializer.Deserialize<Submission>(result, options);
            return submission;
        }

        public async Task DeleteAllSessionsAsync()
        {
            try
            {
                using HttpResponseMessage response = await _entityClient.DeleteAsync(_sessionRoute);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("[cloud] Network Error Exception " + ex);
            }
        }
    }
}
