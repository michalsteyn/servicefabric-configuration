using ServiceFabric.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Configuration
{
    public static class ServiceFabricConfigurationExtensions
    {
        public static IConfigurationBuilder AddFabricConfiguration(this IConfigurationBuilder configurationBuilder, ConfigurationSettings fabricConfig)
        {
            configurationBuilder.Add(new ServiceFabricConfigurationSource(fabricConfig));
            return configurationBuilder;
        }

        public static IConfigurationBuilder AddFabricConfiguration(this IConfigurationBuilder configurationBuilder, string configurationPackageName)
        {
            var config = FabricRuntime.GetActivationContext()?.GetConfigurationPackageObject(configurationPackageName);
            if (config == null)
                throw new ArgumentException($"Configuration package '{configurationPackageName}' was not found.", nameof(configurationPackageName));

            return AddFabricConfiguration(configurationBuilder, config.Settings);
        }
    }
}
