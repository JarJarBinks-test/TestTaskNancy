using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Nancy;
using Nancy.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestTaskNancy.Contracts;
using TestTaskNancy.Services;

namespace TestTaskNancy.Tests
{
    [TestClass]
    public class TestHost
    {
        Browser browser;
        Mock<IStoryService> storyServiceMock;
        readonly String correctContentType = "application/json; charset=utf-8";

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

            var serviceProvider = new Mock<IServiceProvider>();

            serviceProvider
                .Setup(x => x.GetService(typeof(ILoggerFactory)))
                .Returns(mqILoggerFactory.Object);

            storyServiceMock = new Mock<IStoryService>();
             serviceProvider
                .Setup(x => x.GetService(typeof(IStoryService)))
                .Returns(storyServiceMock.Object);

            var serviceScope = new Mock<IServiceScope>();
            serviceScope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

            var serviceScopeFactory = new Mock<IServiceScopeFactory>();
            serviceScopeFactory
                .Setup(x => x.CreateScope())
                .Returns(serviceScope.Object);

            serviceProvider
                .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
                .Returns(serviceScopeFactory.Object);          

            var bootstrapper = new NancyResponseProcessorBootstrapper(serviceProvider.Object);
            browser = new Browser(bootstrapper);
        }

        [TestCleanup]
        public void CleanUp()
        {
            
        }

        [TestMethod]
        public void If_Default_OK_And_ExpectedResult()
        {
            // Arrange

            // Action
            var response = BrowserGet("/").Result;

            // Assert
            Assert.AreEqual(correctContentType, response.ContentType);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var document = System.Text.Json.JsonDocument.Parse(response.Body.AsString());
            var retValue = document.RootElement.GetProperty("message");
            Assert.IsTrue(retValue.GetString().Equals("Hello world!"));
        }

        [TestMethod]
        public void If_Group_Home_Called_With_One_Result_Then_Return_OK_And_ExpectedResult()
        {
            // Arrange
            var section = "home";
            var currDate = DateTime.UtcNow;
            var cutrTotal = new Random().Next(Int32.MaxValue);
            var resultFromService = new List<ArticleGroupByDateView>()
            {
                new ArticleGroupByDateView()
                {
                    Total = cutrTotal,
                    Date = currDate.ToString("yyyy-MM-dd")
                }
            };
            storyServiceMock.Setup(ssm => ssm.GetGroupedStoriesByDate(StoriesSourceEnum.Nytimes, section))
                .Returns(Task.Run(() => resultFromService));

            // Action
            var response = BrowserGet($"/group/{section}").Result;

            // Assert
            Assert.AreEqual(correctContentType, response.ContentType);

            var document = System.Text.Json.JsonDocument.Parse(response.Body.AsString());
            var retValueDate = document.RootElement.GetProperty("date").GetString();
            var retValueTotal = document.RootElement.GetProperty("total").GetInt32();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(currDate.ToString("yyyy-MM-dd"), retValueDate);
            Assert.AreEqual(cutrTotal, retValueTotal);
        }

        [TestMethod]
        public void If_Group_Home_Called_With_Multiple_Results_Then_Return_OK_And_ExpectedResult()
        {
            // Arrange
            var section = "home";
            var currDate = DateTime.UtcNow;
            var cutrTotal = new Random().Next(Int32.MaxValue);
            var resultFromService = new List<ArticleGroupByDateView>()
            {
                new ArticleGroupByDateView()
                {
                    Total = cutrTotal,
                    Date = currDate.ToString("yyyy-MM-dd")
                },
                new ArticleGroupByDateView()
                {
                    Total = cutrTotal-1,
                    Date = currDate.AddHours(-1).ToString("yyyy-MM-dd")
                }
            };
            storyServiceMock.Setup(ssm => ssm.GetGroupedStoriesByDate(StoriesSourceEnum.Nytimes, section))
                .Returns(Task.Run(() => resultFromService));

            // Action
            var response = BrowserGet($"/group/{section}").Result;

            // Assert
            Assert.AreEqual(correctContentType, response.ContentType);

            var document = System.Text.Json.JsonDocument.Parse(response.Body.AsString());
            var arrayresult = document.RootElement.EnumerateArray();
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(resultFromService.Count, arrayresult.Count());
            foreach (var currItem in arrayresult)
            {
                var retValueDate = currItem.GetProperty("date").GetString();
                var retValueTotal = currItem.GetProperty("total").GetInt32();
                Assert.AreEqual(currDate.ToString("yyyy-MM-dd"), retValueDate);
                Assert.AreEqual(cutrTotal, retValueTotal);
                cutrTotal--;
                currDate = currDate.AddHours(-1);
            }
        }

        [TestMethod]
        public void If_Bad_Url_Called_Then_Return_404()
        {
            // Arrange

            // Action
            var response = BrowserGet("/bad/url").Result;

            // Assert
            Assert.AreEqual(correctContentType, response.ContentType);
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            var document = System.Text.Json.JsonDocument.Parse(response.Body.AsString());
            var retValueMessage = document.RootElement.GetProperty("message").GetString();
            var retValueCode = document.RootElement.GetProperty("statusCode").GetInt32();
            Assert.AreEqual("The resource you have requested cannot be found.", retValueMessage);
            Assert.AreEqual((Int32)HttpStatusCode.NotFound, retValueCode);
        }

        [TestMethod]
        public void If_List_Section_Called_With_Multiple_Results_Then_Return_OK_And_ExpectedResult()
        {
            // Arrange
            var section = "home";
            var currDate = DateTime.UtcNow;
            var resultFromService = new List<ArticleView>()
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
                    Updated = currDate.AddHours(-1),
                }
            };
            storyServiceMock.Setup(ssm => ssm.GetStoriesList(StoriesSourceEnum.Nytimes, section, null, null, null))
                .Returns(Task.Run(() => resultFromService));

            // Action
            var response = BrowserGet($"/list/{section}").Result;

            // Assert
            Assert.AreEqual(correctContentType, response.ContentType);

            var document = System.Text.Json.JsonDocument.Parse(response.Body.AsString());
            var arrayResult = document.RootElement.EnumerateArray();
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(resultFromService.Count, arrayResult.Count());
            var i = 0;
            foreach (var currItem in arrayResult)
            {
                var retValueHeading = currItem.GetProperty("heading").GetString();
                var retValueLink = currItem.GetProperty("link").GetString();
                var retValueUpdated = currItem.GetProperty("updated").GetDateTime();
                
                Assert.AreEqual(resultFromService[i].Heading, retValueHeading);
                Assert.AreEqual(resultFromService[i].Link, retValueLink);
                Assert.AreEqual(resultFromService[i].Updated, retValueUpdated);
                i++;
            }
        }

        [TestMethod]
        public void If_List_Section_Called_With_First_Results_Then_Return_OK_And_ExpectedResult()
        {
            // Arrange
            var section = "home";
            var currDate = DateTime.UtcNow;
            var resultFromService = new List<ArticleView>()
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
                    Updated = currDate.AddHours(-1),
                }
            };
            storyServiceMock.Setup(ssm => ssm.GetStoriesList(StoriesSourceEnum.Nytimes, section, 1, null, null))
                .Returns(Task.Run(() => resultFromService));

            // Action
            var response = BrowserGet($"/list/{section}/first").Result;

            // Assert
            Assert.AreEqual(correctContentType, response.ContentType);

            var document = System.Text.Json.JsonDocument.Parse(response.Body.AsString());
            var retValueHeading = document.RootElement.GetProperty("heading").GetString();
            var retValueLink = document.RootElement.GetProperty("link").GetString();
            var retValueUpdated = document.RootElement.GetProperty("updated").GetDateTime();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(resultFromService[0].Heading, retValueHeading);
            Assert.AreEqual(resultFromService[0].Link, retValueLink);
            Assert.AreEqual(resultFromService[0].Updated, retValueUpdated);
        }

        [TestMethod]
        public void If_List_Section_Called_With_Filter_By_Date_Results_Then_Return_OK_And_ExpectedResult()
        {
            // Arrange
            var section = "home";
            var currDate = DateTime.UtcNow.Date;
            var resultFromService = new List<ArticleView>()
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
                    Updated = currDate.AddDays(1),
                }
            };
            storyServiceMock.Setup(ssm => ssm.GetStoriesList(StoriesSourceEnum.Nytimes, section, null, currDate, null))
                .Returns(Task.Run(() => resultFromService));

            // Action
            var response = BrowserGet($"/list/{section}/{currDate:yyyy-MM-dd}").Result;

            // Assert
            Assert.AreEqual(correctContentType, response.ContentType);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var document = System.Text.Json.JsonDocument.Parse(response.Body.AsString());
            var arrayResult = document.RootElement.EnumerateArray();
            Assert.AreEqual(resultFromService.Count, arrayResult.Count());
            // Must 2 items because service is stub.
            var i = 0;
            foreach (var currItem in arrayResult)
            {
                var retValueHeading = currItem.GetProperty("heading").GetString();
                var retValueLink = currItem.GetProperty("link").GetString();
                var retValueUpdated = currItem.GetProperty("updated").GetDateTime();

                Assert.AreEqual(resultFromService[i].Heading, retValueHeading);
                Assert.AreEqual(resultFromService[i].Link, retValueLink);
                Assert.AreEqual(resultFromService[i].Updated, retValueUpdated);
                i++;
            }
        }

        [TestMethod]
        public void If_Article_Called_With_ShortUrl_Filter_Results_Then_Return_OK_And_ExpectedResult()
        {
            // Arrange
            var section = "home";
            var currDate = DateTime.UtcNow;
            var ourLink = new Random().Next(1000000, 9000000).ToString();
            var resultFromService = new List<ArticleView>()
            {
                new ArticleView()
                {
                    Heading = new Random().Next(Int32.MaxValue).ToString(),
                    Link = ourLink,
                    Updated = currDate
                },
                new ArticleView()
                {
                    Heading = new Random().Next(Int32.MaxValue).ToString(),
                    Link = new Random().Next(1000000, 9000000).ToString(),
                    Updated = currDate.AddHours(-1),
                }
            };
            storyServiceMock.Setup(ssm => ssm.GetStoriesList(StoriesSourceEnum.Nytimes, section, 1, null, ourLink))
                .Returns(Task.Run(() => resultFromService));

            // Action
            var response = BrowserGet($"/article/{ourLink}").Result;

            // Assert
            Assert.AreEqual(correctContentType, response.ContentType);

            var document = System.Text.Json.JsonDocument.Parse(response.Body.AsString());
            var retValueHeading = document.RootElement.GetProperty("heading").GetString();
            var retValueLink = document.RootElement.GetProperty("link").GetString();
            var retValueUpdated = document.RootElement.GetProperty("updated").GetDateTime();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(resultFromService[0].Heading, retValueHeading);
            Assert.AreEqual(resultFromService[0].Link, retValueLink);
            Assert.AreEqual(resultFromService[0].Updated, retValueUpdated);
        }

        [TestMethod]
        public void If_Article_Called_With_ShortUrl_Bad_Filter_Results_Then_Return_OK_And_ExpectedResult()
        {
            // Arrange
            var section = "home";
            var currDate = DateTime.UtcNow;
            var ourLink = new Random().Next(1000000, 9000000).ToString();
            var resultFromService = new List<ArticleView>();
            storyServiceMock.Setup(ssm => ssm.GetStoriesList(StoriesSourceEnum.Nytimes, section, 1, null, ourLink))
                .Returns(Task.Run(() => resultFromService));

            // Action
            var response = BrowserGet($"/article/{ourLink}").Result;

            // Assert
            Assert.AreEqual(correctContentType, response.ContentType);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("", response.Body.AsString());
        }

        Task<BrowserResponse> BrowserGet(String url) =>
            browser.Get(url, with =>
            {
                with.HttpRequest();
            });
    }
}
