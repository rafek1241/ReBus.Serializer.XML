using System.Text;

namespace ReBus.Serializer.XML
{
    public class XmlSerializingOptions
    {
        public string RootName { get; set; } = "Messages";
        public bool IncludeNamespace { get; set; }
        public string DefaultNamespacePrefix { get; set; } = "http://tempuri.org";

        public Encoding Encoding { get; set; } = null;
    }
}