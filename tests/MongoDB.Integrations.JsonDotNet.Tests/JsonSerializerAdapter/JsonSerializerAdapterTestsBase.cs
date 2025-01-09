/* Copyright 2015-2016 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Integrations.JsonDotNet.Tests.JsonSerializerAdapter
{
    public abstract class JsonSerializerAdapterTestsBase
    {
        protected T Deserialize<T>(IBsonSerializer<T> serializer, byte[] bson, bool mustBeNested = false)
        {
            using (var memoryStream = new MemoryStream(bson))
            using (var reader = new BsonBinaryReader(memoryStream))
            {
                if (mustBeNested)
                {
                    reader.ReadStartDocument();
                    reader.ReadName();
                }

                var context = BsonDeserializationContext.CreateRoot(reader);
                var value = serializer.Deserialize(context);

                if (mustBeNested)
                {
                    reader.ReadEndDocument();
                }

                return value;
            }
        }

        protected T DeserializeUsingNewtonsoftReader<T>(byte[] bson, bool mustBeNested = false)
        {
            using (var memoryStream = new MemoryStream(bson))
            //using (var newtonsoftReader = new Newtonsoft.Json.Bson.BsonDataReader(memoryStream))
            using (var reader = new BsonBinaryReader(memoryStream))
            using (var newtonsoftReader = new BsonReaderAdapter(reader))
            {
                //newtonsoftReader.DateTimeKindHandling = System.DateTimeKind.Utc;
                newtonsoftReader.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;

                if (mustBeNested)
                {
                    newtonsoftReader.Read(); // StartObject
                    newtonsoftReader.Read(); // PropertyName
                }
                newtonsoftReader.Read(); // Json.NET serializers expect caller to have already called Read

                var newtonsoftSerializer = new Newtonsoft.Json.JsonSerializer();
                var value = newtonsoftSerializer.Deserialize<T>(newtonsoftReader);

                if (mustBeNested)
                {
                    newtonsoftReader.Read(); // EndObject
                }

                return value;
            }
        }

        protected byte[] Serialize<T>(IBsonSerializer<T> serializer, T value, bool mustBeNested = false, GuidRepresentation guidRepresentation = GuidRepresentation.Unspecified)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BsonBinaryWriter(memoryStream, new BsonBinaryWriterSettings { GuidRepresentation = guidRepresentation }))
            {
                if (mustBeNested)
                {
                    writer.WriteStartDocument();
                    writer.WriteName("x");
                }

                var context = BsonSerializationContext.CreateRoot(writer);
                serializer.Serialize(context, value);

                if (mustBeNested)
                {
                    writer.WriteEndDocument();
                }

                return memoryStream.ToArray();
            }
        }

        
        protected byte[] SerializeUsingNewtonsoftWriter<T>(T value, bool mustBeNested = false, GuidRepresentation guidRepresentation = GuidRepresentation.Unspecified)
        {
            using (var memoryStream = new MemoryStream())
            /*
            
            The Newtonsoft.Json.Bson.BsonDataWriter cannot be used for Guids with `GuidRepresentation.CSharpLegacy` 
            because it will always write Guids with with BinaryData subtype `0x04` that map to `BsonBinaryType.Uuid`. 
            `BsonBinaryType.OldUuid` would write the required BinaryData subtype (`0x03`), but there is no way to 
            force its use through the public API surface (the subtype it is actually hardcoded to `BsonBinaryType.Uuid`
            in the JsonNet codebase).

            The MongoDb BSON writer instead use BsonBinarySubType.UuidLegacy (`0x03`) when the type 
            is `GuidRepresentation.CSharpLegacy`.
            The BinaryData subtype `0x04` map to `BsonBinarySubType.UuidStandard`, but it cannot be used
            because then the `Guid` bytes are written in network order (BigEndian) introducting more differences.
            
            This code has been modified to use the `BsonWriterAdapter` and to verify that Guids roundtrip
            correctly when using the adapter.
            
            */

            //using (var newtonsoftWriter = new Newtonsoft.Json.Bson.BsonDataWriter(memoryStream))

            using (var writer = new BsonBinaryWriter(memoryStream, new BsonBinaryWriterSettings { GuidRepresentation = GuidRepresentation.CSharpLegacy }))
            using (var newtonsoftWriter = new BsonWriterAdapter(writer))
            //
            {
                if (mustBeNested)
                {
                    newtonsoftWriter.WriteStartObject();
                    newtonsoftWriter.WritePropertyName("x");
                }

                var newtonsoftSerializer = new Newtonsoft.Json.JsonSerializer();
                newtonsoftSerializer.Serialize(newtonsoftWriter, value);

                if (mustBeNested)
                {
                    newtonsoftWriter.WriteEndObject();
                }

                return memoryStream.ToArray();
            }
        }

        protected byte[] ToBson(string json, GuidRepresentation guidRepresentation = GuidRepresentation.Unspecified)
        {
            BsonDocument document;
            using (var reader = new JsonReader(json, new JsonReaderSettings { GuidRepresentation = guidRepresentation }))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                document = BsonDocumentSerializer.Instance.Deserialize(context);
            }

            using (var memoryStream = new MemoryStream())
            using (var writer = new BsonBinaryWriter(memoryStream, new BsonBinaryWriterSettings { GuidRepresentation = guidRepresentation }))
            {
                var context = BsonSerializationContext.CreateRoot(writer);
                BsonDocumentSerializer.Instance.Serialize(context, document);
                return memoryStream.ToArray();
            }
        }
    }
}


