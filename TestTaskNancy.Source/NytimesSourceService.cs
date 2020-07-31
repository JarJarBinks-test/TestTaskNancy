using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TestTaskNancy.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TestTaskNancy.Sources
{
    public class NytimesSourceService : INytimesSourceService
    {
        readonly String apiKey;
        readonly String serviceDateFormat = "yyyy-MM-ddTHH:mm:sszzz";
        readonly ILogger<INytimesSourceService> logger;
        public NytimesSourceService(IConfiguration configuration, ILogger<INytimesSourceService> logger)
        {
            if (configuration == null)
                throw new ArgumentNullException($"{nameof(configuration)} is null.");
            
            apiKey = configuration.GetValue<String>("NytimesApiKey");
            this.logger = logger ?? throw new ArgumentNullException($"{nameof(logger)} is null."); ;
        }

        public async Task<String> GetRawStories(String section)
        {
            var url = $"https://api.nytimes.com/svc/topstories/v2/{section}.json?api-key={apiKey}";
            logger.LogInformation($"{nameof(GetRawStories)}. {nameof(url)}: {url}");
            var wr = (HttpWebRequest)HttpWebRequest.Create(url);
            wr.Accept = "application/json";

            using (var wrs = await wr.GetResponseAsync() as HttpWebResponse)
            using (var rs = wrs.GetResponseStream())
            using (var rr = new StreamReader(rs))
            {
                var result = await rr.ReadToEndAsync();
                logger.LogDebug($"{nameof(GetRawStories)}. {nameof(url)}: {url}. {nameof(result)}: {result}");
                return result;
            }
        }

        public async Task<List<ArticleView>> GetStoriesList(String section)
        {
            logger.LogInformation($"{nameof(GetStoriesList)}. {nameof(section)}: {section}");

            var result = await GetRawStories(section);
            if (String.IsNullOrWhiteSpace(result))
                return null;

            var document =  System.Text.Json.JsonDocument.Parse(result);            
            var retValue = document.RootElement.GetProperty("results").EnumerateArray().Select(x=> {
                var dateStr = x.GetProperty("updated_date").GetString();
                return new ArticleView()
                {
                    Heading = x.GetProperty("title").GetString(),
                    Link = x.GetProperty("short_url").GetString(),
                    Updated = DateTimeOffset.ParseExact(dateStr, serviceDateFormat, null, System.Globalization.DateTimeStyles.None).UtcDateTime
                };
            }).ToList();

            return retValue;
        }
    }
}
