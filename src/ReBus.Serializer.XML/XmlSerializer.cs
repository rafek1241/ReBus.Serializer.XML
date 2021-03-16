using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Serialization;
using ReBus.Serializer.XML.Exceptions;
using Formatting = Newtonsoft.Json.Formatting;

namespace ReBus.Serializer.XML
{
    public class XmlSerializer : ISerializer
    {
        private readonly XmlSerializingOptions _options;
        private ILogger _logger;
        private readonly JsonSerializerSettings _jsonSettings;

        public XmlSerializer()
        :this(new XmlSerializingOptions())
        { }
        
        public XmlSerializer(XmlSerializingOptions options)
        {
            _options = options;
            _jsonSettings = new JsonSerializerSettings()
            {
                Converters = new List<JsonConverter>()
                {
                    new StringEnumConverter()
                }
            };
        }
        
        public Task<TransportMessage> Serialize(Message message)
        {
            using var ms = new MemoryStream();
            using var xmlWriter = new XmlTextWriter(ms, _options.Encoding);

            var messageType = Type.GetType(message.GetMessageType());
            ComposeXmlMessage(message, xmlWriter, messageType);
            xmlWriter.Flush();

            var result = Encoding.ASCII.GetString(ms.ToArray());
            _logger?.LogDebug("Serialized '{Type}' message to XML content: {Result}", messageType?.Name, result);

            return Task.FromResult(new TransportMessage(message.Headers, ms.ToArray()));
        }

        private void ComposeXmlMessage(
            Message message,
            XmlTextWriter xmlWriter,
            Type messageType
        )
        {
            xmlWriter.WriteStartDocument();

            WriteRootContainerElement(message, xmlWriter, messageType);
            SerializeAndWriteXmlMessage(message, xmlWriter, messageType);

            xmlWriter.WriteEndDocument();
        }

        private void SerializeAndWriteXmlMessage(
            Message message,
            XmlTextWriter xmlWriter,
            Type messageType
        )
        {
            var messageBody = JsonConvert.SerializeObject(message.Body, _jsonSettings);
            var xmlObj = JsonConvert.DeserializeXmlNode(
                messageBody,
                messageType?.Name ?? throw new InvalidOperationException()
            );

            xmlWriter.WriteRaw(xmlObj.InnerXml);
        }

        private void WriteRootContainerElement(
            Message message,
            XmlTextWriter xmlWriter,
            Type messageType
        )
        {
            var namespaceOfMessage = DefineNamespaceOfMessage(message, messageType);
            xmlWriter.WriteStartElement(_options.RootName);

            xmlWriter.WriteAttributeString("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
            xmlWriter.WriteAttributeString("xmlns:xsd", "http://www.w3.org/2001/XMLSchema");
            xmlWriter.WriteAttributeString("xmlns", namespaceOfMessage);

            if (_options.IncludeBaseTypeNamespaces)
            {
                IncludeBaseTypesInElement(xmlWriter, messageType);
            }
        }

        private void IncludeBaseTypesInElement(XmlTextWriter xmlWriter, Type messageType)
        {
            var baseTypes = messageType
                .GetBaseTypes()
                .Where(x => x != typeof(object))
                .ToArray();

            var firstBaseType = baseTypes.FirstOrDefault();
            if (firstBaseType == null)
            {
                return;
            }

            xmlWriter.WriteAttributeString($"xmlns:{_options.BaseTypeNamespaceAttributeName}", firstBaseType.FullName!);

            for (var i = 1; i < baseTypes.Length; i++)
            {
                xmlWriter.WriteAttributeString($"xmlns:{_options.BaseTypeNamespaceAttributeName}{i}", baseTypes[i].FullName!);
            }
        }

        private string DefineNamespaceOfMessage(Message message, Type messageType)
        {
            if (_options.IncludeNamespace == false)
            {
                return string.Empty;
            }
            
            var headers = message.Headers;
            if (headers.ContainsKey(Headers.Namespace))
            {
                return headers[Headers.Namespace];
            }

            var @namespace = new Uri(
                headers.ContainsKey(Headers.NamespacePrefix)
                    ? headers[Headers.NamespacePrefix]
                    : _options.DefaultNamespacePrefix
            );

            return @namespace + messageType.Namespace;
        }

        public Task<Message> Deserialize(TransportMessage transportMessage)
        {
            var xmlBody = transportMessage.Body;
            var messageType = transportMessage.GetMessageType();

            var xmlString = Encoding.Default.GetString(xmlBody);
            _logger?.LogDebug("Incoming transport message: {Message}", xmlString);

            if (transportMessage.Headers.ContainsKey(Rebus.Messages.Headers.Type) == false)
            {
                throw new InvalidOperationException(
                    $"{typeof(XmlSerializer).FullName} deserialization requires `{Rebus.Messages.Headers.Type}` in message headers"
                );
            }

            var document = new XmlDocument();
            document.LoadXml(xmlString);
            var rootElement = document.DocumentElement;
            rootElement?.Attributes.RemoveAll();
            
            if (rootElement != null && rootElement.ChildNodes.Count > 1)
            {
                throw new NotSupportedArrayOfObjectsInBody(xmlString);
            }
            
            var typeFromAssembly = Type.GetType(messageType);
            
            var rootElementChild = rootElement?.FirstChild;
            
            if (rootElementChild?.Name != typeFromAssembly?.Name)
            {
                throw new InvalidOperationException(
                    $"Attempt to deserialize `{rootElementChild?.Name}` object to the type `{typeFromAssembly?.Name}` which is not equal."
                );
            }
            
            var jsonObject = JsonConvert.SerializeXmlNode(rootElementChild, Formatting.Indented, true);
            var jobj = (JObject)JsonConvert.DeserializeObject(jsonObject, _jsonSettings);
            var @object = jobj.ToObject(typeFromAssembly);
            if (@object == null)
            {
                throw new NotSupportedException(
                    "Message body was unrecognized and because of that we can't deserialize that"
                );
            }
            return Task.FromResult(new Message(transportMessage.Headers, @object));
        }

        public void WithLogging(ILogger logger)
        {
            _logger = logger;
        }
    }
}