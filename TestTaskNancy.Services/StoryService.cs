using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestTaskNancy.Contracts;
using TestTaskNancy.Sources;

namespace TestTaskNancy.Services
{
    public class StoryService : IStoryService
    {
        readonly ILogger<IStoryService> logger;
        readonly String outputGroupedByDataFormat = "yyyy-MM-dd";
        readonly IServiceProvider serviceProvider;

        public StoryService(ILogger<IStoryService> logger, IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        public async Task<List<ArticleGroupByDateView>> GetGroupedStoriesByDate(StoriesSourceEnum source, String section)
        {
            logger.LogInformation($"{nameof(GetGroupedStoriesByDate)}. {nameof(source)}:{source}, {nameof(section)}:{section}");

            var stories = await GetStoriesList(source, section);
            var groupedResult = stories.GroupBy(x => x.Updated.Date).ToList();
            var result = groupedResult.Select(x => new ArticleGroupByDateView()
            {
                Date = x.Key.ToString(outputGroupedByDataFormat),
                Total = x.ToList().Count
            }).ToList();
            return result;
        }

        public async Task<List<ArticleView>> GetStoriesList(StoriesSourceEnum source, String section, Int32? topCount, 
            DateTime? dateFilter, String shortUrlFilter)
        {
            logger.LogInformation($@"{nameof(GetStoriesList)}. {nameof(source)}:{source}, {nameof(section)}:{section}, {
                nameof(topCount)}:{topCount}, {nameof(dateFilter)}:{dateFilter}, {nameof(shortUrlFilter)}:{shortUrlFilter}");

            var stories = await GetStoriesList(source, section);

            if (dateFilter.HasValue)
                stories = stories.Where(st=> st.Updated.Date >= dateFilter.Value.Date).ToList();

            if (!String.IsNullOrWhiteSpace(shortUrlFilter))
                stories = stories.Where(st=>st.Link.EndsWith(shortUrlFilter)).ToList();

            if (topCount.HasValue)
                stories = stories.Take(topCount.Value).ToList();

            return stories;
        }

        Task<List<ArticleView>> GetStoriesList(StoriesSourceEnum source, String section) =>
            GetSourceService(source).GetStoriesList(section);

        IStorySourceService GetSourceService(StoriesSourceEnum source)
        {
            if (source == StoriesSourceEnum.Nytimes)
                return serviceProvider.GetService<INytimesSourceService>();

            throw new NotSupportedException($"{nameof(GetSourceService)}. {source} not supported.");
        }

        public static void RegisterSourceServices(IServiceCollection services)
        {
            services.AddTransient<INytimesSourceService, NytimesSourceService>();
        }
    }
}
