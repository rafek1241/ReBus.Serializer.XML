using System;

namespace ReBus.Serializer.XML.UnitTests.Messages
{
    public class TestMessageWithProtectedSetters
    {
        public Guid GuidProp { get; protected set; }
        public TestEnum EnumProp { get; protected set; }
        public DateTime DateTimeProp { get; protected set; }

        protected TestMessageWithProtectedSetters()
        {
        }

        public TestMessageWithProtectedSetters(
            Guid guidProp,
            TestEnum enumProp,
            DateTime dateTimeProp
        )
        {
            GuidProp = guidProp;
            EnumProp = enumProp;
            DateTimeProp = dateTimeProp;
        }
    }
}