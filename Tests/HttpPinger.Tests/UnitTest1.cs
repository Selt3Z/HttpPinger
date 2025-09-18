using HttpPinger;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace HttpPinger.Tests
{
    public class ProgramTests
    {
        [Fact]
        public async Task TestPingUrlsAsync()
        {
            var urls = new List<string> { "https://www.google.com", "https://www.github.com" };
            await Pinger.PingUrlsAsync(urls);

            var result = await File.ReadAllTextAsync("ping_results.json");
            Assert.Contains("https://www.google.com", result);
            Assert.Contains("https://www.github.com", result);
        }
    }
}
