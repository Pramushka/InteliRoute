// Services/Routing/RouterClient.cs
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InteliRoute.Services.Routing
{
    public sealed class RouterApiOptions
    {
        public string BaseUrl { get; set; } = "http://127.0.0.1:8011";
        public bool UseRules { get; set; } = true;
        public double MinConfidence { get; set; } = 0.10;
        public int TimeoutSec { get; set; } = 5;
    }

    // Keep JSON field names identical to the FastAPI schema
    public sealed record RouterPredictRequest(
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("body")] string Body,
        [property: JsonPropertyName("allowed_departments")] List<string> AllowedDepartments,
        [property: JsonPropertyName("use_rules")] bool UseRules,
        [property: JsonPropertyName("min_confidence")] double MinConfidence,
        [property: JsonPropertyName("return_candidates")] bool ReturnCandidates = true
    );

    public sealed record RouterPredictResponse(
        [property: JsonPropertyName("department")] string department,
        [property: JsonPropertyName("source")] string source,
        [property: JsonPropertyName("prob")] double? prob,
        [property: JsonPropertyName("candidates")] object? candidates
    );

    public interface IRouterClient
    {
        Task<RouterPredictResponse> PredictAsync(
            string subject,
            string body,
            IEnumerable<string> allowed,
            bool useRules,
            double minConfidence,
            CancellationToken ct);
    }

    /// <summary>
    /// Typed HTTP client (created by AddHttpClient) that talks to the FastAPI router.
    /// Program.cs config sets BaseAddress + Timeout; we just call relative paths here.
    /// </summary>
    public sealed class RouterClient : IRouterClient
    {
        private readonly HttpClient _http;
        private readonly RouterApiOptions _opt;
        private readonly ILogger<RouterClient> _log;

        public RouterClient(HttpClient http, IOptions<RouterApiOptions> opt, ILogger<RouterClient> log)
        {
            _http = http;
            _opt = opt.Value;
            _log = log;
        }

        public async Task<RouterPredictResponse> PredictAsync(
            string subject,
            string body,
            IEnumerable<string> allowed,
            bool useRules,
            double minConfidence,
            CancellationToken ct)
        {
            var payload = new RouterPredictRequest(
                Subject: subject ?? string.Empty,
                Body: body ?? string.Empty,
                AllowedDepartments: allowed?.Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new(),
                UseRules: useRules,
                MinConfidence: minConfidence,
                ReturnCandidates: true
            );

            _log.LogInformation("Router POST /predict | Allowed={Allowed} UseRules={UseRules} MinConf={MinConf}",
                string.Join(",", payload.AllowedDepartments), payload.UseRules, payload.MinConfidence);

            var resp = await _http.PostAsJsonAsync("/predict", payload, ct);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadFromJsonAsync<RouterPredictResponse>(cancellationToken: ct)
                       ?? throw new InvalidOperationException("Empty router response");

            _log.LogInformation("Router result: dept={Dept} prob={Prob:0.000} source={Source}",
                json.department, json.prob, json.source);

            return json;
        }
    }
}
