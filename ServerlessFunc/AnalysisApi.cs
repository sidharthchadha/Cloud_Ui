using System;
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
        private readonly HttpClient _entityClient;
        private readonly string _analysisRoute;

        public AnalysisApi(string analysisRoute)
        {
            _entityClient = new HttpClient();
            _analysisRoute = analysisRoute;
        }

        public async Task<List<string>> GetPassedUsersofTest(string sessionId,string testName)
        {
            var response = await _entityClient.GetAsync(_analysisRoute + $"/{sessionId}/{testName}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,

            };

            List<string> PassedUsers = JsonSerializer.Deserialize<List<string>>(result, options);
            return PassedUsers;
        } 

    }
}
