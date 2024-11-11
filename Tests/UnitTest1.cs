using NUnit.Framework;
using RestSharp;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Tests
{
    public class Tests
    {
        private string _baseAddress;
        private string _logPath;
        private RestClient _restClient;
        private HttpClient _httpClient;
        private string _reportPath;

        [SetUp]
        public void Setup()
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            var configJson = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(configJson);

            _baseAddress = doc.RootElement.GetProperty("ApiEndpoints").GetProperty("PositionEndpointBase").GetString();
            _logPath = doc.RootElement.GetProperty("Logging").GetProperty("LogPath").GetString();
            _restClient = new RestClient(new RestClientOptions(_baseAddress) { ThrowOnAnyError = true });
            _httpClient = new HttpClient { BaseAddress = new Uri(_baseAddress) };

            Directory.CreateDirectory(Path.GetDirectoryName(_logPath));
            _reportPath = Path.Combine(AppContext.BaseDirectory, "Reports", "report.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(_reportPath));

            CreateEmptyReport();
        }

        private void CreateEmptyReport()
        {
            using var writer = new StreamWriter(_reportPath);
            writer.WriteLine("Výsledek testování pro stránku:");
            writer.WriteLine($"URL: {_baseAddress}/api/career/v2/positions/java-developer-\n");

            writer.WriteLine("• Popis pracovní pozice:");
            writer.WriteLine("  o Pracovní pozice má vyplněný popis: [čeká na hodnotu]");
            writer.WriteLine("  o Pracovní pozice je vhodná pro studenty: [čeká na hodnotu]\n");

            writer.WriteLine("• Kde budete pracovat:");
            writer.WriteLine("  o Jméno místa výkonu práce: [čeká na hodnotu]");
            writer.WriteLine("  o Stát: [čeká na hodnotu]");
            writer.WriteLine("  o Město: [čeká na hodnotu]");
            writer.WriteLine("  o Ulice a číslo: [čeká na hodnotu]");
            writer.WriteLine("  o PSČ: [čeká na hodnotu]\n");

            writer.WriteLine("• Nadřízený (executiveUser):");
            writer.WriteLine("  o Nadřízený je vyplněn: [čeká na hodnotu]");
            writer.WriteLine("  o Jméno nadřízeného: [čeká na hodnotu]");
            writer.WriteLine("  o Nadřízený má fotografii: [čeká na hodnotu]");
            writer.WriteLine("  o Nadřízený má vyplněný popis: [čeká na hodnotu]\n");
        }

        [Test]
        public async Task TestJobAdvertisementDetails()
        {
            var content = await GetContent($"{_baseAddress}/api/career/v2/positions/java-developer-");
            if (content == null)
            {
                LogMessage("Chyba při získávání obsahu stránky.");
                return;
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            UpdateReportWithData(root);
        }

        private async Task<string> GetContent(string url)
        {
            try
            {
                var request = new RestRequest(url, Method.Get);
                var response = await _restClient.ExecuteAsync(request);
                if (response.IsSuccessful)
                    return response.Content;
            }
            catch (Exception ex)
            {
                LogMessage($"RestSharp error: {ex.Message}");
            }

            try
            {
                var httpResponse = await _httpClient.GetAsync(url);
                if (httpResponse.IsSuccessStatusCode)
                    return await httpResponse.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                LogMessage($"Error with HttpClient: {ex.Message}");
            }

            return null;
        }

        private void UpdateReportWithData(JsonElement root)
        {
            var reportContent = File.ReadAllText(_reportPath);

            var hasDescription = root.TryGetProperty("description", out var descriptionElement) &&
                                 !string.IsNullOrEmpty(descriptionElement.GetString());
            reportContent = reportContent.Replace("[čeká na hodnotu]", hasDescription ? "✅" : "❌");

            var isForStudents = root.TryGetProperty("forStudents", out var forStudentsElement) &&
                                forStudentsElement.ValueKind == JsonValueKind.True;
            reportContent = reportContent.Replace("[čeká na hodnotu]", isForStudents ? "✅" : "❌");

            if (root.TryGetProperty("placeOfEmployment", out var placeElement))
            {
                var placeName = placeElement.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : "";
                var state = placeElement.TryGetProperty("state", out var stateElement) ? stateElement.GetString() : "";
                var city = placeElement.TryGetProperty("city", out var cityElement) ? cityElement.GetString() : "";
                var streetName = placeElement.TryGetProperty("streetName", out var streetElement) ? streetElement.GetString() : "";
                var postalCode = placeElement.TryGetProperty("postalCode", out var postalElement) ? postalElement.GetString() : "";

                reportContent = reportContent
                    .Replace("[čeká na hodnotu]", placeName)
                    .Replace("[čeká na hodnotu]", state)
                    .Replace("[čeká na hodnotu]", city)
                    .Replace("[čeká na hodnotu]", streetName)
                    .Replace("[čeká na hodnotu]", postalCode);
            }

            File.WriteAllText(_reportPath, reportContent);
            Console.WriteLine(reportContent);
        }

        private void LogMessage(string message)
        {
            Console.WriteLine(message);
            File.AppendAllText(_logPath, $"{DateTime.Now}: {message}\n");
        }
    }
}