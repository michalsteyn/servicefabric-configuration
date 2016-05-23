using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Fabric.Description;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceFabric.Extensions.Configuration
{
    public class ServiceFabricConfigurationSource : IConfigurationSource
    {
        readonly ConfigurationSettings settings;

        public ServiceFabricConfigurationSource(ConfigurationSettings settings)
        {
            this.settings = settings;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new ServiceFabricConfigurationProvider(settings);
        }
    }
}
