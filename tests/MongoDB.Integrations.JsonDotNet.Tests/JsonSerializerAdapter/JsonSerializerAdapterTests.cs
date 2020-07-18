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
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Integrations.JsonDotNet.Tests.JsonSerializerAdapter
{
    [TestFixture]
    public class JsonSerializerAdapterTests
    {
        [Test]
        public void constructor_should_initialize_instance()
        {
            var result = new JsonSerializerAdapter<object>();

            result.ValueType.Should().Be(typeof(object));
        }

        [Test]
        public void constructor_with_wrappedSerializer_should_initialize_instance()
        {
            var wrappedSerializer = Substitute.For<Newtonsoft.Json.JsonSerializer>();
            var result = new JsonSerializerAdapter<object>(wrappedSerializer);

            result.ValueType.Should().Be(typeof(object));
        }

        [Test]
        public void constructor_with_wrappedSerializer_should_throw_when_wrappedSerializer_is_null()
        {
            Action action = () => { var result = new JsonSerializerAdapter<object>(null); };

            action.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("wrappedSerializer");
        }

        
    }

    [TestFixture]
    public class JsonSerializerAdapterClassWithBsonInt32Tests : JsonSerializerAdapterTestsBase
    {
        private class C
        {
            public BsonInt32 V { get; set; }
        }

        [TestCase("{ V : null }", null)]
        [TestCase("{ V : 1 }", 1)]
        public void Deserialize_should_return_expected_result(string json, int? nullableInt32)
        {
            var subject = new JsonSerializerAdapter<C>();
            var expectedResult = nullableInt32 == null ? null : (BsonInt32)nullableInt32.Value;

            var result = Deserialize<C>(subject, ToBson(json));

            result.V.Should().Be(expectedResult);
        }

        [TestCase(null, "{ \"V\" : null }")]
        [TestCase(1, "{ \"V\" : 1 }")]
        public void Serialize_should_have_expected_result(int? nullableInt32, string expectedResult)
        {
            var subject = new JsonSerializerAdapter<C>();
            var value = new C { V = nullableInt32 == null ? null : (BsonInt32)nullableInt32.Value };

            var result = Serialize(subject, value);

            result.Should().Equal(ToBson(expectedResult));
        }
    }

    [TestFixture]
    public class JsonSerializerAdapterClassWithBsonMaxKeyTests : JsonSerializerAdapterTestsBase
    {
        private class C
        {
            public BsonMaxKey V { get; set; }
        }

        [TestCase("{ V : null }", null)]
        [TestCase("{ V : { $maxKey : 1 } }", true)]
        public void Deserialize_should_return_expected_result(string json, bool? nullableMaxKey)
        {
            var subject = new JsonSerializerAdapter<C>();
            var expectedResult = nullableMaxKey == null ? null : BsonMaxKey.Value;

            var result = Deserialize<C>(subject, ToBson(json));

            result.V.Should().Be(expectedResult);
        }

        [TestCase(null, "{ \"V\" : null }")]
        [TestCase(true, "{ \"V\" : MaxKey }")]
        public void Serialize_should_have_expected_result(bool? nullableMaxKey, string expectedResult)
        {
            var subject = new JsonSerializerAdapter<C>();
            var value = new C { V = nullableMaxKey == null ? null : BsonMaxKey.Value };

            var result = Serialize(subject, value);

            result.Should().Equal(ToBson(expectedResult));
        }
    }

    [TestFixture]
    public class JsonSerializerAdapterClassWithIntTests : JsonSerializerAdapterTestsBase
    {
        private class C
        {
            public int X { get; set; }
        }

        [TestCase("{ X : 1 }", 1)]
        [TestCase("{ X : 2 }", 2)]
        public void Deserialize_should_return_expected_result(string json, int expectedResult)
        {
            var subject = new JsonSerializerAdapter<C>();

            var result = Deserialize<C>(subject, ToBson(json));

            result.X.Should().Be(expectedResult);
        }

        [TestCase(1, "{ X : 1 }")]
        [TestCase(2, "{ X : 2 }")]
        public void Serialize_should_have_expected_result(int x, string expectedResult)
        {
            var subject = new JsonSerializerAdapter<C>();
            var value = new C { X = x };

            var result = Serialize(subject, value);

            result.Should().Equal(ToBson(expectedResult));
        }
    }

    [TestFixture]
    public class JsonSerializerAdapterClassWithObjectIdTests : JsonSerializerAdapterTestsBase
    {
        private class C
        {
            [Newtonsoft.Json.JsonProperty("_id")]
            public ObjectId Id { get; set; }
        }

        [TestCase("{ _id : ObjectId(\"112233445566778899aabbcc\") }", "112233445566778899aabbcc")]
        [TestCase("{ _id : ObjectId(\"2233445566778899aabbccdd\") }", "2233445566778899aabbccdd")]
        public void Deserialize_should_return_expected_result(string json, string expectedResult)
        {
            var subject = new JsonSerializerAdapter<C>();

            var result = Deserialize<C>(subject, ToBson(json));

            result.Id.Should().Be(ObjectId.Parse(expectedResult));
        }

        [TestCase("112233445566778899aabbcc", "{ \"_id\" : ObjectId(\"112233445566778899aabbcc\") }")]
        [TestCase("2233445566778899aabbccdd", "{ \"_id\" : ObjectId(\"2233445566778899aabbccdd\") }")]
        public void Serialize_should_have_expected_result(string hexValue, string expectedResult)
        {
            var subject = new JsonSerializerAdapter<C>();
            var value = new C { Id = ObjectId.Parse(hexValue) };

            var result = Serialize(subject, value);

            result.Should().Equal(ToBson(expectedResult));
        }
    }


    namespace External
    {
        struct Struct
        {
            public string PropInStruct { get; set; }
        }

        class Base
        {
            public string Id { get; set; }
            public Struct Value { get; set; }
        }

        class Derived : Base
        {
            public string DerivedProp { get; set; }
        }
    }

    [TestFixture]
    public class JsonSerializerAdapterClassWithPolymorphicTypeTests : JsonSerializerAdapterTestsBase
    {
        public struct Struct
        {
            public string PropInStruct { get; set; }
        }

        public class Base
        {
            public string Id { get; set; }
            public Struct Value { get; set; }
        }

        public class Derived : Base
        {
            public string DerivedProp { get; set; }
        }

        class Container
        {
            public List<object> Items { get;set; }
        }
        
        [Test]
        public void Should_deserialize_a_list_of_heterogeneous_types()
        {
            var container = new Container
            {
                Items = new List<object>
                {
                    new Base{ Id = "BaseId 1" },
                    new Base{ Id = "BaseId 2", Value = new Struct{ PropInStruct = "42" } },
                    new Derived { Id = "Derived 1", DerivedProp = "Derived Prop 3" },
                    new Derived { Id = "Derived 2", DerivedProp = "Derived Prop 5" },
                }
            };


            var serializer = CreateSerializer<Container>(new TypeNameMap
            {
                [typeof(Base)] =
                {
                    ("Base", direction: TypeNameMapping.Direction.All),
                },
                [typeof(Derived)] =
                {
                    ("Derived", direction: TypeNameMapping.Direction.All),
                }
            });
            
            var serialized = Serialize(serializer, container);
            var deserialized = Deserialize(serializer, serialized);
            deserialized.Should().BeEquivalentTo(container);
        }

        [Test]
        public void Should_deserialize_different_types_with_same_name_when_overriding_the_type_key_during_deserialization()
        {
            var container = new Container
            {
                Items = new List<object>
                {
                    new Derived { Id = "BaseId 4", DerivedProp = "Derived Prop 5" },
                }
            };

            var container2 = new Container
            {
                Items = new List<object>
                {
                    new External.Derived { Id = "BaseId 4", DerivedProp = "Derived Prop 5" },
                }
            };

            var customSerializer = CreateSerializer(new TypeNameMap
            {
                [typeof(Derived)] =
                {
                    ("Derived", direction: TypeNameMapping.Direction.All),
                },
                [typeof(External.Derived)] =
                {
                    ("Derived", direction: TypeNameMapping.Direction.Out),
                    // Deserialize as External.Derived if the type name 
                    // is 'Derived' and the scope is 'CustomScope'
                    ("CustomScope", "Derived")
                },

                // Force the assemblyName to "CustomScope"
                ResolveDeserializationTypeName = key => (assemblyName: "CustomScope", key.name)
            });

            using (customSerializer.Push())
            {
                // Should work also if the serialized is retrieved through a lookup in the
                // mongodb bson serializer registry
                var serializer = Bson.Serialization.BsonSerializer.LookupSerializer<Container>();
                var serialized = Serialize(serializer, container);
                // serialized as { Items = [ _t:Derived , ... ] }
                var deserialized = Deserialize(serializer, serialized);
                deserialized.Should().BeEquivalentTo(container2);
            }
        }

        [Test]
        public void Should_deserialize_different_types_with_same_name_in_different_scopes_using_different_serializers()
        {
            var container = new Container
            {
                Items = new List<object>
                {
                    new Derived { Id = "BaseId 4", DerivedProp = "Derived Prop 5" },
                }
            };

            var container2 = new Container
            {
                Items = new List<object>
                {
                    new External.Derived { Id = "BaseId 4", DerivedProp = "Derived Prop 5" },
                }
            };

            // Get the serializer through the standard Bson serializer
            var serializer = Bson.Serialization.BsonSerializer.LookupSerializer<Container>();

            // Create a serializer for the local scope
            var localScopeSerializer = CreateSerializer(new TypeNameMap
            {
                [typeof(Derived)] =
                {
                    ("Derived", direction: TypeNameMapping.Direction.All),
                }
            });

            // Create a serializer for the external scope so that we can deserialize
            // was was previously serialized from the Derived type inot a External.Derived
            // instance.
            var externalScopeSerializer = CreateSerializer(new TypeNameMap
            {
                [typeof(External.Derived)] =
                {
                    ("Derived")
                }
            });

            // push the local serializer as current
            using (localScopeSerializer.Push())
            {
                // This will use localScopeSerializer
                var serialized = Serialize(serializer, container);
                
                // push the external serializer as current
                using (externalScopeSerializer.Push())
                {
                    // This will use externalScopeSerializer
                    var deserialized = Deserialize(serializer, serialized);
                    deserialized.Should().BeEquivalentTo(container2);
                }
            }
        }

        [Test]
        public void Should_roundtrip_structs()
        {
            var serializerAdapter = CreateSerializer<Struct>();
            var instance = new Struct { PropInStruct = "42" };
            var serialized = Serialize(serializerAdapter, instance);

            var deserialized = Deserialize(serializerAdapter, serialized);

            deserialized.Should().BeEquivalentTo(instance);
        }


        
        private Newtonsoft.Json.JsonSerializer CreateSerializer(TypeNameMap typeMap = null)
        {
            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                SerializationBinder = new JsonCompositeSerializationBinderAdapter(
                    new DefaultPropertyNamesAdapter(),
                    typeMap: typeMap
                )
            };

            var jsonSerializer = Newtonsoft.Json.JsonSerializer.Create(settings)
                .WithBsonAdapterConfiguration();
            
            return jsonSerializer;
        }

        private JsonSerializerAdapter<T> CreateSerializer<T>(TypeNameMap typeMap = null)
        {
            return new JsonSerializerAdapter<T>(CreateSerializer(typeMap));
        }
    }

    
}

