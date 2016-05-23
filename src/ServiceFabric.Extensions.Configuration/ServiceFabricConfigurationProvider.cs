using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Fabric.Description;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace ServiceFabric.Extensions.Configuration
{
    public class ServiceFabricConfigurationProvider : ConfigurationProvider
    {
        readonly ConfigurationSettings fabricConfig;

        public ServiceFabricConfigurationProvider(ConfigurationSettings fabricConfig)
        {
            if (fabricConfig == null)
                throw new ArgumentNullException(nameof(fabricConfig));

            this.fabricConfig = fabricConfig;
        }

        public override void Load()
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var section in fabricConfig.Sections)
            {
                var sectionPrefix = section.Name + ConfigurationPath.KeyDelimiter;

                foreach (var parameter in section.Parameters)
                {
                    var key = sectionPrefix + parameter.Name;
                    var value = parameter.IsEncrypted ? SecureStringToString(parameter.DecryptValue()) : parameter.Value;

                    if (data.ContainsKey(key))
                        throw new FormatException($"Key '{key}' already exists");
                    data[key] = value;
                }
            }

            Data = data;
        }

        static string SecureStringToString(SecureString value)
        {
            IntPtr bstr = Marshal.SecureStringToBSTR(value);
            try
            {
                return Marshal.PtrToStringBSTR(bstr);
            }
            finally
            {
                Marshal.FreeBSTR(bstr);
            }
        }
    }
}
