using Microsoft.Extensions.Logging;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Responses.Negotiation;
using Nancy.TinyIoc;
using System;
using System.Linq;

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

            container.Register(typeof(IServiceProvider), _serviceProvider);            
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
