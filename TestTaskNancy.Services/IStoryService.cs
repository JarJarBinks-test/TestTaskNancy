using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestTaskNancy.Contracts;

namespace TestTaskNancy.Services
{
    public interface IStoryService
    {
        public Task<List<ArticleView>> GetStoriesList(StoriesSourceEnum source, String section, Int32? topCount, 
            DateTime? dateFilter, String shortUrlFilter);
        public Task<List<ArticleGroupByDateView>> GetGroupedStoriesByDate(StoriesSourceEnum source, String section);
    }
}
