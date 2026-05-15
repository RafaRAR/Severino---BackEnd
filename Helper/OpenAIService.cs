using DotNetEnv;
using System.Text;
using System.Text.Json;

namespace APIseverino.Helpers
{
    public class OpenAIService
    {
        private readonly HttpClient _httpClient;

        public OpenAIService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            if (File.Exists(".env.test"))
                Env.Load(".env.test");
            else
                Env.Load(".env");
        }

        public async Task<string> GerarTexto(string prompt)
        {
            var body = new
            {
                model = "gpt-4.1-mini",
                messages = new[]
                {
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    }
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.openai.com/v1/chat/completions"
            );

            var apiKey = Environment.GetEnvironmentVariable("OPENAI_SECRET_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("A variável de ambiente 'OPENAI_SECRET_KEY' não está definida.");
            }

            request.Headers.Add(
                "Authorization",
                $"Bearer {apiKey}"
            );

            request.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);

            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
    }
}