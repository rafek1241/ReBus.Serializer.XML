using System;

namespace ReBus.Serializer.XML.Exceptions
{
    public sealed class NotSupportedArrayOfObjectsInBody : NotSupportedException
    {
        public const string ContentKey = "Content";

        public NotSupportedArrayOfObjectsInBody(string content)
            : base("Array of objects in a root element in XML serialization/deserialization is not supported.")
        {
            Data.Add(ContentKey, content);
        }
    }
}