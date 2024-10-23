using System.Text.Json;
using System.Text.Json.Serialization;

namespace Group6WebProject.Services
{
    public class ReCaptchaService : IReCaptchaService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ReCaptchaService> _logger;

        public ReCaptchaService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<ReCaptchaService> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<bool> VerifyToken(string token)
        {
            var secretKey = _configuration["ReCaptcha:SecretKey"];
            var client = _httpClientFactory.CreateClient();

            var values = new Dictionary<string, string>
            {
                { "secret", secretKey },
                { "response", token }
            };

            //Verify the reCAPTCHA response
            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("reCAPTCHA API returned an unsuccessful status code: " + response.StatusCode);
                return false;
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var reCaptchaResponse = JsonSerializer.Deserialize<ReCaptchaResponse>(jsonString);

            return reCaptchaResponse.Success;
        }
    }

    public class ReCaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("challenge_ts")]
        public string ChallengeTs { get; set; }

        [JsonPropertyName("hostname")]
        public string Hostname { get; set; }

        [JsonPropertyName("error-codes")]
        public string[] ErrorCodes { get; set; }
    }
}
