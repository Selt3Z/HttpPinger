using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace HttpPinger
{
    public class Pinger
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("HttpPinger CLI");
            Console.WriteLine("Enter URLs to ping, type 's' to start.");

            var urls = new List<string>();
            while (true)
            {
                Console.Write("> ");
                var input = Console.ReadLine();
                if (input?.ToLower() == "s" || input?.ToLower() == "ы") break;
                if (Uri.IsWellFormedUriString(input, UriKind.Absolute))
                {
                    if (urls.Contains(input))
                    {
                        Console.WriteLine("URL is already added.");
                        continue;
                    }
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

        public static async Task PingUrlsAsync(List<string> urls)
        {
            var results = new List<PingResult>();
            var traceResults = new List<string>();  // Для хранения результатов трассировки / For storing trace results

            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(10); // Установим тайм-аут в 10 секунд / Set timeout to 10 seconds

                int totalUrls = urls.Count;
                int completedUrls = 0;

                foreach (var url in urls)
                {
                    var result = new PingResult
                    {
                        Url = url,
                        StatusCode = 0,
                        ResponseTime = 0,
                        Error = string.Empty
                    };

                    // Пинг URL / Ping URL
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

                    // Выводим результат пинга сразу / Output the ping result immediately as soon as it's ready
                    Console.WriteLine(JsonConvert.SerializeObject(new[] { result }, Formatting.Indented));  // This prints the ping result in JSON format

                    // Запуск трассировки в фоне / Start traceroute in the background
                    var traceTask = TraceRouteAsync(url, traceResults); // TraceRoute running asynchronously

                    // Обновляем прогресс / Update progress after each ping
                    completedUrls++;
                    Console.Write($"\rProgress: {completedUrls}/{totalUrls} URLs completed.");  // Display progress

                    await traceTask;  // Ожидаем завершения трассировки перед переходом к следующему пингу
                    results.Add(result);  // Добавляем результат пинга в список
                }

                // Сериализуем результаты пинга в JSON / Serialize ping results to JSON
                string json = JsonConvert.SerializeObject(results, Formatting.Indented);

                // Сохраняем результаты в файл / Save the results to a file
                await System.IO.File.WriteAllTextAsync("ping_results.json", json);
                Console.WriteLine("\nPing results saved to ping_results.json");

                // Запись результатов трассировки в файл / Save traceroute results to file
                if (traceResults.Any())
                {
                    string traceJson = JsonConvert.SerializeObject(traceResults, Formatting.Indented);
                    await System.IO.File.WriteAllTextAsync("traceroute_results.json", traceJson);
                    Console.WriteLine("Traceroute results saved to traceroute_results.json");

                    // Выводим результаты трассировки в консоль / Print traceroute results to console
                    foreach (var traceResult in traceResults)
                    {
                        Console.WriteLine(traceResult); // Вывод результатов трассировки / Output traceroute results
                    }
                }
            }
        }

        public static async Task TraceRouteAsync(string url, List<string> traceResults)
        {
            Uri uri = new Uri(url);
            string domain = uri.Host;

            var startInfo = new ProcessStartInfo
            {
                FileName = "tracert",
                Arguments = domain,  // Передаем только домен / Pass only the domain
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (var process = Process.Start(startInfo))
                {
                    if (process?.StandardOutput != null)
                    {
                        using (var reader = process.StandardOutput)
                        {
                            string result = await reader.ReadToEndAsync();
                            // Добавляем результаты трассировки в список / Add trace results to the list
                            traceResults.Add($"Traceroute for {domain}:\n{result}");
                        }

                        process.WaitForExit();  // Дожидаемся завершения процесса / Wait for the process to exit
                    }
                    else
                    {
                        Console.WriteLine($"Failed to start tracert for {domain}."); // Ошибка при запуске / Error starting tracert
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing tracert for {domain}: {ex.Message}");  // Ошибка выполнения трассировки / Error executing tracert
            }
        }


    }


    public class PingResult
    {
        public required string Url { get; set; }
        public required int StatusCode { get; set; }
        public required double ResponseTime { get; set; }
        public required string Error { get; set; }
    }

}
