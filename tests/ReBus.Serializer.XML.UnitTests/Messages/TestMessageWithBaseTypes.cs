namespace ReBus.Serializer.XML.UnitTests.Messages.BaseTypes
{
    internal class RootClass
    {
        public string RootProp { get; set; }
    }
    
    internal class SubClass : RootClass
    {
        public string SubClassProp { get; set; }
    }

    internal interface IAmImplementedInterface
    {
    }
    
    internal class TestMessageWithBaseTypes : SubClass, IAmImplementedInterface
    {
        public string Prop { get; set; }
    }
}