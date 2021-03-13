using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Serialization;

namespace ReBus.Serializer.XML
{
    public class XmlSerializer : ISerializer
    {
        public Task<TransportMessage> Serialize(Message message) => throw new System.NotImplementedException();

        public Task<Message> Deserialize(TransportMessage transportMessage) => throw new System.NotImplementedException();
    }
}