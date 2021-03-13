namespace ReBus.Serializer.XML
{
    /// <summary>
    /// Contains keys of headers known &amp; used by Rebus XML serializer
    /// </summary>
    public static class Headers
    {
        /// <summary>
        /// Define the namespace of message that will be used in XML representation of message.
        /// </summary>
        public const string Namespace = "rbs2-xml-ns";

        /// <summary>
        /// Namespace prefix of the message that is used if you want to specify site that will contain information about the message.
        /// </summary>
        public const string NamespacePrefix = "rbs2-xml-ns-prefix";
    }
}