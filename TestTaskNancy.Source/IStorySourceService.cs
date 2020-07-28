using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestTaskNancy.Contracts;

namespace TestTaskNancy.Sources
{
    public interface IStorySourceService
    {
        public Task<List<ArticleView>> GetStoriesList(String section);
        public Task<String> GetRawStories(String section);
    }
}
