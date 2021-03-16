using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Xunit;
using Newtonsoft.Json;
using Rebus.Messages;
using ReBus.Serializer.XML.UnitTests.Messages;
using ReBus.Serializer.XML.UnitTests.Messages.BaseTypes;
using Xunit;
using Xunit.Abstractions;

namespace ReBus.Serializer.XML.UnitTests
{
    public class XmlSerializerTests
    {
        private readonly Fixture _fixture;
        private XmlSerializer Sut
        {
            get
            {
                var serializer = new XmlSerializer(_options);
                serializer.WithLogging(_logger);
                return serializer;
            }
        }

        private XmlSerializingOptions _options;
        private readonly ILogger _logger;

        public XmlSerializerTests(ITestOutputHelper outputHelper)
        {
            _fixture = new Fixture();
            _options = new XmlSerializingOptions();
            _logger = new XunitLoggerProvider(outputHelper)
                .CreateLogger(nameof(XmlSerializerTests));

        }

        [Fact]
        public async Task when_message_passed_with_protected_setters__maps_that_to_proper_xml()
        {
            var testMessage = _fixture.Create<TestMessageWithProtectedSetters>();
            var message = new Message(new Dictionary<string, string>(), testMessage);
            _options = new XmlSerializingOptions()
            {
                IncludeBaseTypeNamespaces = false
            };
            
            var transportMessage = await Sut
                .Serialize(message)
                .ConfigureAwait(false);

            var result = transportMessage.Body;
            var resultAsString = Encoding.UTF8.GetString(result);

            resultAsString
                .Should()
                .Be(
                    $"<?xml version=\"1.0\"?>" +
                    $"<Messages xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"{new Uri(_options.DefaultNamespacePrefix) + typeof(TestMessageWithProtectedSetters).Namespace}\">" +
                    $"<{nameof(TestMessageWithProtectedSetters)}>" +
                    $"<GuidProp>{testMessage.GuidProp}</GuidProp>" +
                    $"<EnumProp>{testMessage.EnumProp.ToString()}</EnumProp>" +
                    $"<DateTimeProp>{JsonConvert.SerializeObject(testMessage.DateTimeProp).Replace("\"", "")}</DateTimeProp>" +
                    $"</{nameof(TestMessageWithProtectedSetters)}>" +
                    $"</Messages>"
                );
        }

        [Fact]
        public async Task when_passed_message_with_base_types__maps_to_xml_object_with_base_types_in_root_container()
        {
            var testMessage = _fixture.Create<TestMessageWithBaseTypes>();
            var message = new Message(new Dictionary<string, string>(), testMessage);
            _options = new XmlSerializingOptions();
            
            var transportMessage = await Sut
                .Serialize(message)
                .ConfigureAwait(false);

            var result = transportMessage.Body;
            var resultAsString = Encoding.UTF8.GetString(result);

            resultAsString.Should()
                .Contain(
                    "xmlns=\"http://tempuri.org/ReBus.Serializer.XML.UnitTests.Messages.BaseTypes\" xmlns:baseType=\"ReBus.Serializer.XML.UnitTests.Messages.BaseTypes.IAmImplementedInterface\" xmlns:baseType1=\"ReBus.Serializer.XML.UnitTests.Messages.BaseTypes.SubClass\" xmlns:baseType2=\"ReBus.Serializer.XML.UnitTests.Messages.BaseTypes.RootClass\""
                );
        }

        [Fact]
        public async Task when_transport_message_passed_with_specified_type__maps_to_specific_object()
        {
            var guid = Guid.NewGuid();
            var testEnum = TestEnum.Success;
            var dateAsString = "2021-01-09T02:41:42.3036507";
            var input = "<?xml version=\"1.0\"?>" +
                "<Messages xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns=\"http://tempuri.org/ReBus.Serializer.XML.UnitTests.Messages\">" +
                $"<{nameof(TestMessageWithProtectedSetters)}>" +
                $"<GuidProp>{guid}</GuidProp>" +
                $"<EnumProp>{testEnum.ToString()}</EnumProp>" +
                $"<DateTimeProp>{dateAsString}</DateTimeProp>" +
                $"</{nameof(TestMessageWithProtectedSetters)}>" +
                "</Messages>";

            var headers = new Dictionary<string, string>()
            {
                { Rebus.Messages.Headers.Type, typeof(TestMessageWithProtectedSetters).AssemblyQualifiedName }
            };
            var message = new TransportMessage(headers, Encoding.Default.GetBytes(input));

            var result = await Sut
                .Deserialize(message)
                .ConfigureAwait(false);

            var resultMessage = result.Body;

            var typedMessageResult = resultMessage
                .Should()
                .BeOfType<TestMessageWithProtectedSetters>()
                .Subject;
            
            typedMessageResult.EnumProp
                .Should()
                .Be(testEnum);
            
            typedMessageResult.GuidProp
                .Should()
                .Be(guid);

            typedMessageResult.DateTimeProp
                .Should()
                .Be(DateTime.Parse(dateAsString));
        }
    }
}