using System.Text;

namespace ReBus.Serializer.XML
{
    public class XmlSerializingOptions
    {
        public string RootName { get; set; } = "Messages";
        public bool IncludeNamespace { get; set; } = true;
        public bool IncludeBaseTypeNamespaces { get; set; } = true;
        public string DefaultNamespacePrefix { get; set; } = "http://tempuri.org";
        public string BaseTypeNamespaceAttributeName { get; set; } = "baseType";
        public Encoding Encoding { get; set; } = null;
    }
}