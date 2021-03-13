using System;
using Microsoft.Extensions.Logging;
using Rebus.Config;
using Rebus.Serialization;

namespace ReBus.Serializer.XML.Extensions
{
    public static class RebusSerializerExtensions
    {
        public static XmlSerializer UseXmlSerializing(
            this StandardConfigurer<ISerializer> configurer,
            XmlSerializingOptions options = null
        ) => UseXmlSerializing(configurer, null, options);

        public static XmlSerializer UseXmlSerializing(
            this StandardConfigurer<ISerializer> configurer,
            ILogger logger,
            XmlSerializingOptions options = null
        )
        {
            if (configurer == null)
            {
                throw new ArgumentNullException(nameof(configurer));
            }

            var instance = new XmlSerializer(options ?? new XmlSerializingOptions());
            instance.WithLogging(logger);
            
            configurer.Register(r => instance);

            return instance;
        }
    }
}