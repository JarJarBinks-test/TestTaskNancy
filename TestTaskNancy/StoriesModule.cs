using Microsoft.Extensions.Logging;
using Nancy;
using System;
using System.Linq;
using TestTaskNancy.Services;

namespace TestTaskNancy
{
    public class StoriesModule : NancyModule
    {
        // We can select source for load stories. For now supported only Nytimes.
        readonly String headerSourceName = "ForSource";
        readonly String homeSection = "home";
        readonly StoriesSourceEnum defaultSource = StoriesSourceEnum.Nytimes;
        public StoriesModule(IStoryService storyService, ILogger<StoriesModule> logger)
        {
            StoriesSourceEnum GetSourceOrDefault() => 
                Enum.TryParse<StoriesSourceEnum>(Request.Headers[headerSourceName].FirstOrDefault(), out StoriesSourceEnum res) ? 
                    res : defaultSource;

            Get("/list/{section:alpha}/first", async args =>
                Response.AsJson((await storyService.GetStoriesList(GetSourceOrDefault(), (String)args.section, 1, null, null)).FirstOrDefault()));

            Get("/list/{section:alpha}/{updatedDate:datetime(yyyy-MM-dd)}", async args =>
                Response.AsJson(await storyService.GetStoriesList(GetSourceOrDefault(), (String)args.section, null, (DateTime)args.updatedDate, null)));

            Get("/list/{section:alpha}", async args =>
                Response.AsJson(await storyService.GetStoriesList(GetSourceOrDefault(), (String)args.section, null, null, null)));

            Get("/article/(?<shortUrl>[a-zA-Z0-9]{7})", async args =>
                Response.AsJson((await storyService.GetStoriesList(GetSourceOrDefault(), homeSection, 1, null, (String)args.shortUrl)).FirstOrDefault()));

            Get("/group/{section:alpha}", async args => {
                var result = await storyService.GetGroupedStoriesByDate(GetSourceOrDefault(), (String)args.section);
                if (result.Count == 1)
                    return Response.AsJson(result.FirstOrDefault());

                return Response.AsJson(result);
            });

            Get("/", args => Response.AsJson(new { message = "Hello world!" }));


            Before += (ctx) => {
                logger.LogInformation($"Request: {ctx.Request.Url}");

                return null;
            };

            OnError += (ctx, ex) => {
                logger.LogError(ex.Message);

                return HttpStatusCode.InternalServerError;
            };

            After += ctx =>
            {
                logger.LogDebug($"Response data: {ctx.Response}");
            };
        }
    }
}
