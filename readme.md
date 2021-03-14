# XML Serializer for ReBus

[![.NET Core](https://github.com/rafek1241/ReBus.Serializer.XML/actions/workflows/dotnet.yml/badge.svg)](https://github.com/rafek1241/ReBus.Serializer.XML/actions/workflows/dotnet.yml)

## Why it was created?

To make transition from NServiceBus MSMQ to ReBus as much painless as possible.

## Getting started

```c#
Configure.With(...)
    .Serialization(s => s.UseXmlSerializing(settings))
    //...
```

## Example:

_Message body sent to queues_

```json
//$type - TestMessage object
{
    "GuidProp": "0576a0cd-13c7-4dc2-a74b-4e7419213f5a",
    "EnumProp": 1,
    "DateTimeProp": "2020-12-11T00:20:11.8711501"
}
```
:arrow_down: :arrow_up:
```xml
<?xml version="1.0"?>
<Messages xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xmlns:xsd="http://www.w3.org/2001/XMLSchema"
    xmlns="http://tempuri.org/ReBus.Serializer.XML.UnitTests.Messages">
    <TestMessage>
        <GuidProp>0576a0cd-13c7-4dc2-a74b-4e7419213f5a</GuidProp>
        <EnumProp>Success</EnumProp>
        <DateTimeProp>2020-12-11T00:20:11.8711501</DateTimeProp>
    </TestMessage>
</Messages>
```

## Limitations

This serializer **does not** support serialization/deserialization of collections.

## Contribution

It would be great if you guys would help me improve this or bring up any issues. I will work on them in free time.
