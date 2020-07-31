using Microsoft.Extensions.Logging;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Responses.Negotiation;
using Nancy.TinyIoc;
using System;
using System.Linq;
using TestTaskNancy.Services;

namespace TestTaskNancy
{
    public class NancyResponseProcessorBootstrapper : DefaultNancyBootstrapper
    {
        readonly IServiceProvider _serviceProvider;

        public NancyResponseProcessorBootstrapper(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            container.Register(typeof(IStoryService), (c, p) => _serviceProvider.GetService(typeof(IStoryService)));
            container.Register(typeof(ILogger<StoriesModule>), (c, p) => 
                ((ILoggerFactory)_serviceProvider.GetService(typeof(ILoggerFactory))).CreateLogger<StoriesModule>());
        }

        protected override Func<ITypeCatalog, NancyInternalConfiguration> InternalConfiguration
        {
            get
            {
                return NancyInternalConfiguration.WithOverrides((c) =>
                {
                    // Support only response as JSON.
                    c.ResponseProcessors.ToList().ForEach(x => c.ResponseProcessors.Remove(x));                    
                    c.ResponseProcessors.Add(typeof(JsonProcessor));
                });
            }
        }
    }
}
