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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson;
using System.Reflection;
using System.Linq;
using Newtonsoft.Json.Serialization;
using System.Threading;


namespace MongoDB.Integrations.JsonDotNet
{
    using JsonSerializer = Newtonsoft.Json.JsonSerializer;

    /// <summary>
    /// 
    /// </summary>
    public class JsonSerializerAdapter
    {
        class Node<T> : IDisposable
            where T : class
        {
            /// <summary>
            /// This is not shared for different T.
            /// but we only instantiate this clss internally for 
            /// <see cref="JsonSerializer"/> anyway.
            /// </summary>
            private static AsyncLocal<Node<T>> _storage = new AsyncLocal<Node<T>>();

            private readonly Node<T> _prev;
            private readonly T _item;

            protected Node() {  }

            protected Node(Node<T> prev, T item)
            {
                _prev = prev;
                _item = item;
            }

            public static IDisposable Push(T item)
            {
                var res = new Node<T>(_storage.Value, item);
                _storage.Value = res;
                return res;
            }

            public static T Current => _storage.Value?._item;

            public void Dispose()
            {
                _storage.Value = _prev;
            }
        }

        class Node : Node<JsonSerializer> { }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static JsonSerializer GetCurrent() => Node.Current;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jsonSerializer"></param>
        public static IDisposable Push(JsonSerializer jsonSerializer)
        {
            return Node.Push(jsonSerializer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static JsonSerializer CreateDefaultJsonSerializer()
        {
            return new JsonSerializer().WithBsonAdapterConfiguration();
        }
    }


    /// <summary>
    /// Represents an adapter that adapts a Json.NET serializer for use with the MongoDB driver.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <seealso cref="SerializerBase{TValue}" />
    /// <seealso cref="IBsonArraySerializer" />
    /// <seealso cref="IBsonDocumentSerializer" />
    public class JsonSerializerAdapter<TValue> : 
        SerializerBase<TValue>, 
        IBsonArraySerializer, 
        IBsonDocumentSerializer
    {
        private readonly JsonSerializer _wrappedSerializer;

        // private fields
        private JsonSerializer WrappedSerializer => JsonSerializerAdapter.GetCurrent() ?? _wrappedSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSerializerAdapter{TValue}"/> class.
        /// </summary>
        public JsonSerializerAdapter() 
            : this(JsonSerializerAdapter.CreateDefaultJsonSerializer())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSerializerAdapter{TValue}"/> class.
        /// </summary>
        /// <param name="wrappedSerializer">The wrapped serializer.</param>
        /// <exception cref="ArgumentNullException">wrappedSerializer</exception>
        public JsonSerializerAdapter(
            JsonSerializer wrappedSerializer
        )
        {
            _wrappedSerializer = wrappedSerializer ?? throw new ArgumentNullException("wrappedSerializer");
        }

        // public methods
        /// <inheritdoc/>
        public override TValue Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            
            var serializer = WrappedSerializer;

            var readerAdapter = new BsonReaderAdapter(
                context.Reader,
                serializer.SerializationBinder as IPropertyNamesAdapter
            );
            return (TValue)serializer.Deserialize(readerAdapter, args.NominalType);
        }

        /// <inheritdoc/>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TValue value)
        {
            var serializer = WrappedSerializer;
            var writerAdapter = new BsonWriterAdapter(
                context.Writer,
                serializer.SerializationBinder as IPropertyNamesAdapter
            );
            serializer.Serialize(writerAdapter, value, args.NominalType);
        }

        /// <inheritdoc/>
        public bool TryGetItemSerializationInfo(out BsonSerializationInfo serializationInfo)
        {
            var serializer = WrappedSerializer;
            


            var valueType = typeof(TValue);

            

            var contract = serializer.ContractResolver.ResolveContract(valueType);
            var arrayContract = contract as JsonArrayContract;
            if (arrayContract == null)
            {
                serializationInfo = null;
                return false;
            }
            if (arrayContract.Converter != null)
            {
                throw new BsonSerializationException($"The Json.NET contract for type \"{valueType.Name}\" has a Converter and JsonConverters are opaque.");
            }
            if (arrayContract.IsReference ?? false)
            {
                throw new BsonSerializationException($"The Json.NET contract for type \"{valueType.Name}\" is serialized as a reference.");
            }
            if (arrayContract.ItemConverter != null)
            {
                throw new BsonSerializationException($"The Json.NET contract for type \"{valueType.Name}\" has an ItemConverter and JsonConverters are opaque.");
            }

            

            var itemType = arrayContract.CollectionItemType;
            //var itemSerializerType = typeof(JsonSerializerAdapter<>).MakeGenericType(itemType);
            //var itemSerializer = (IBsonSerializer)Activator.CreateInstance(itemSerializerType, _wrappedSerializer);

            var itemSerializer = serializer.GetOrCreateAdapter(itemType);
            serializationInfo = new BsonSerializationInfo(null, itemSerializer, nominalType: itemType);
            return true;
        }

        /// <inheritdoc/>
        public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo)
        {
            serializationInfo = null;

            var serializer = WrappedSerializer;
            
            
            var valueType = typeof(TValue);
            var contract = serializer.ContractResolver.ResolveContract(valueType);
            var objectContract = contract as JsonObjectContract;
            if (objectContract == null)
            {
                serializationInfo = null;
                return false;
            }
            if (objectContract.Converter != null)
            {
                throw new BsonSerializationException($"The Json.NET contract for type \"{valueType.Name}\" has a JsonConverter and JsonConverters are opaque.");
            }
            if (objectContract.IsReference ?? false)
            {
                throw new BsonSerializationException($"The Json.NET contract for type \"{valueType.Name}\" is serialized as a reference.");
            }

            var property = objectContract.Properties.FirstOrDefault(p => p.UnderlyingName == memberName);
            if (property == null)
            {
                return false;
            }
            var elementName = property.PropertyName;

            Type memberType;
            if (!TryGetMemberType(valueType, memberName, out memberType))
            {
                return false;
            }
            
            //var memberSerializerType = _jsonSerializerAdapterTypes.GetOrAdd(memberType, _get);
            //var memberSerializerType = typeof(JsonSerializerAdapter<>).MakeGenericType(memberType);
            //var memberSerializer = (IBsonSerializer)Activator.CreateInstance(memberSerializerType, _wrappedSerializer);

            var memberSerializer = serializer.GetOrCreateAdapter(memberType);
            serializationInfo = new BsonSerializationInfo(elementName, memberSerializer, nominalType: memberType);
            return true;
        }

        private static bool TryGetMemberType(Type type, string memberName, out Type memberType)
        {
            memberType = null;

            var memberInfos = type.GetMember(memberName);
            if (memberInfos.Length != 1)
            {
                return false;
            }
            var memberInfo = memberInfos[0];

            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field: memberType = ((FieldInfo)memberInfo).FieldType; break;
                case MemberTypes.Property: memberType = ((PropertyInfo)memberInfo).PropertyType; break;
                default: throw new BsonSerializationException($"Unsupported member type \"{memberInfo.MemberType}\" for member: {memberName}.");
            }

            return true;
        }
    }
}
