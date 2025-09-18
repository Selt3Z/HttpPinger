using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HttpPinger
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("HttpPinger CLI");
            Console.WriteLine("Enter URLs to ping, type 'exit' to quit.");

            var urls = new List<string>();
            while (true)
            {
                Console.Write("> ");
                var input = Console.ReadLine();
                if (input?.ToLower() == "exit") break;
                if (Uri.IsWellFormedUriString(input, UriKind.Absolute))
                {
                    urls.Add(input);
                    Console.WriteLine($"Added: {input}");
                }
                else
                {
                    Console.WriteLine("Invalid URL. Please try again.");
                }
            }

            await PingUrlsAsync(urls);
        }

        static async Task PingUrlsAsync(List<string> urls)
        {
            var results = new List<PingResult>();
            using (var httpClient = new HttpClient())
            {
                foreach (var url in urls)
                {
                    var result = new PingResult { Url = url };
                    try
                    {
                        var response = await httpClient.GetAsync(url);
                        result.StatusCode = (int)response.StatusCode;
                        result.ResponseTime = response.Headers.Date.HasValue ?
                            (DateTimeOffset.UtcNow - response.Headers.Date.Value).TotalMilliseconds : 0;
                    }
                    catch (Exception ex)
                    {
                        result.StatusCode = 0;
                        result.Error = ex.Message;
                    }
                    results.Add(result);
                }
            }

            string json = JsonConvert.SerializeObject(results, Formatting.Indented);
            await System.IO.File.WriteAllTextAsync("ping_results.json", json);
            Console.WriteLine("Ping results saved to ping_results.json");
        }
    }

    public class PingResult
    {
        public string Url { get; set; }
        public int StatusCode { get; set; }
        public double ResponseTime { get; set; }
        public string Error { get; set; }
    }
}
