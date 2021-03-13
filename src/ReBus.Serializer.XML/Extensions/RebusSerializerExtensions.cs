using System;
using Rebus.Config;
using Rebus.Serialization;

namespace ReBus.Serializer.XML.Extensions
{
    public static class RebusSerializerExtensions
    {
        public static XmlSerializer UseXmlSerializing(this StandardConfigurer<ISerializer> configurer, XmlSerializingOptions options = null)
        {
            if (configurer == null)
            {
                throw new ArgumentNullException(nameof(configurer));
            }

            var instance = new XmlSerializer(options ?? new XmlSerializingOptions());
            configurer.Register(r=> instance);
            
            return instance;
        }
    }
}