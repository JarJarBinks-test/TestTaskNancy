using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestTaskNancy.Contracts;
using TestTaskNancy.Services;
using TestTaskNancy.Sources;

namespace TestTaskNancy.Tests
{
    [TestClass]
    public class TestServices
    {
        Mock<IServiceProvider> serviceProvider;
        Mock<INytimesSourceService> sourceService;

        [TestInitialize]
        public void Setup()
        {
            var mockIConfigurationSection = new Mock<IConfigurationSection>();
            mockIConfigurationSection.Setup(mc => mc.Value).Returns("TEST_KEY");

            var mockIConfiguration = new Mock<IConfiguration>();
            mockIConfiguration.Setup(c => c.GetSection(It.IsAny<String>())).Returns(mockIConfigurationSection.Object);

            var mqILoggerFactory = new Mock<ILoggerFactory>();
            mqILoggerFactory
                .Setup(x => x.CreateLogger(It.IsAny<String>()))
                .Returns(new Mock<ILogger>().Object);

            sourceService = new Mock<INytimesSourceService>();

            serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(x => x.GetService(typeof(ILoggerFactory)))
                .Returns(mqILoggerFactory.Object);

            serviceProvider
                .Setup(x => x.GetService(typeof(INytimesSourceService)))
                .Returns(sourceService.Object);

            var serviceScope = new Mock<IServiceScope>();
            serviceScope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

            var serviceScopeFactory = new Mock<IServiceScopeFactory>();
            serviceScopeFactory
                .Setup(x => x.CreateScope())
                .Returns(serviceScope.Object);

            serviceProvider
                .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
                .Returns(serviceScopeFactory.Object);         
        }

        [TestCleanup]
        public void CleanUp()
        {
        }

        [TestMethod]
        public void Get_Grouped_Stories_By_Date()
        {
            // Arrange
            var section = "home";
            var dateTimeFormat = "yyyy-MM-dd";
            var currDate = DateTime.UtcNow;
            var articles = new List<ArticleView>()
            {
                new ArticleView()
                {
                    Heading = new Random().Next(Int32.MaxValue).ToString(),
                    Link = new Random().Next(Int32.MaxValue).ToString(),
                    Updated = currDate
                },
                new ArticleView()
                {
                    Heading = new Random().Next(Int32.MaxValue).ToString(),
                    Link = new Random().Next(Int32.MaxValue).ToString(),
                    Updated = currDate.AddDays(-2),
                },
                new ArticleView()
                {
                    Heading = new Random().Next(Int32.MaxValue).ToString(),
                    Link = new Random().Next(Int32.MaxValue).ToString(),
                    Updated = currDate.AddDays(-4),
                },
                new ArticleView()
                {
                    Heading = new Random().Next(Int32.MaxValue).ToString(),
                    Link = new Random().Next(Int32.MaxValue).ToString(),
                    Updated = currDate.AddDays(-4),
                }
            };
            var storyService = new StoryService(serviceProvider.Object);
            //sourceService.Setup(nt => nt.GetRawStories(section)).Returns(Task.Run(() => 
            //    "{\"results\": [{\"title\":\"title\", \"updated_date\": \"2020-07-26T09:21:19-04:00\", \"short_url\",\"https://nyti.ms/2OZOvs8\"}] }"));

            sourceService.Setup(nt => nt.GetStoriesList(section)).Returns(Task.Run(() => articles));

            // Action
            var results = storyService.GetGroupedStoriesByDate(StoriesSourceEnum.Nytimes, section).Result;

            // Assert
            Assert.AreEqual(3, results.Count);

            var i = 0;
            articles.GroupBy(x => x.Updated).ToList().ForEach(x =>
            {
                Assert.AreEqual(x.Key.ToString(dateTimeFormat), results[i].Date);
                Assert.AreEqual(x.Count(), results[i].Total);
                i++;
            });            
        }

        [TestMethod]
        public void Get_Grouped_Stories_By_Date_InvalidSource()
        {
            // Arrange
            var section = "home";
            var storyService = new StoryService(serviceProvider.Object);

            sourceService.Setup(nt => nt.GetStoriesList(section)).Returns(Task.Run(() => new List<ArticleView>()));

            // Action, Assert
            Assert.ThrowsException<AggregateException>(() => storyService.GetGroupedStoriesByDate(StoriesSourceEnum.None, section).Result);
        }

        [TestMethod]
        public void Get_Stories_List()
        {
            // Arrange
            var section = "home";
            var currDate = DateTime.UtcNow;
            var articles = new List<ArticleView>()
            {
                new ArticleView()
                {
                    Heading = new Random().Next(Int32.MaxValue).ToString(),
                    Link = new Random().Next(Int32.MaxValue).ToString(),
                    Updated = currDate
                },
                new ArticleView()
                {
                    Heading = new Random().Next(Int32.MaxValue).ToString(),
                    Link = new Random().Next(Int32.MaxValue).ToString(),
                    Updated = currDate.AddDays(-2),
                },
                new ArticleView()
                {
                    Heading = new Random().Next(Int32.MaxValue).ToString(),
                    Link = new Random().Next(Int32.MaxValue).ToString(),
                    Updated = currDate.AddDays(-4),
                },
                new ArticleView()
                {
                    Heading = new Random().Next(Int32.MaxValue).ToString(),
                    Link = new Random().Next(Int32.MaxValue).ToString(),
                    Updated = currDate.AddDays(-6),
                }
            };
            var storyService = new StoryService(serviceProvider.Object);
            sourceService.Setup(nt => nt.GetStoriesList(section)).Returns(Task.Run(() => articles));

            // Actions
            var resultsAll = storyService.GetStoriesList(StoriesSourceEnum.Nytimes, section, null, null, null).Result;
            var resultsTop2 = storyService.GetStoriesList(StoriesSourceEnum.Nytimes, section, 2, null, null).Result;
            var resultsFourDaysAgo = storyService.GetStoriesList(StoriesSourceEnum.Nytimes, section, null, currDate.AddDays(-4), null).Result;
            var resultsLastLink = storyService.GetStoriesList(StoriesSourceEnum.Nytimes, section, null, null, articles.Last().Link).Result;

            // Assert
            Assert.AreEqual(articles.Count, resultsAll.Count);
            Assert.AreEqual(2, resultsTop2.Count);
            Assert.AreEqual(3, resultsFourDaysAgo.Count);
            Assert.AreEqual(1, resultsLastLink.Count);
        }
    }
}
