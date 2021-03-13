using System;
using Rebus.Config;
using Rebus.Serialization;

namespace ReBus.Serializer.XML.Extensions
{
    public static class RebusSerializerExtensions
    {
        public static void UseXmlSerializing(this StandardConfigurer<ISerializer> configurer)
        {
            if (configurer == null)
            {
                throw new ArgumentNullException(nameof(configurer));
            }

            configurer.Register(r=> new XmlSerializer());
        }
    }
}