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
using MongoDB.Integrations.JsonDotNet.Converters;
using MongoDB.Bson.Serialization;
using System.Collections.Concurrent;
using System.Linq.Expressions;


namespace MongoDB.Integrations.JsonDotNet
{
    using JsonSerializer = Newtonsoft.Json.JsonSerializer;

    /// <summary>
    /// 
    /// </summary>
    public static class JsonSerializerAdapterExtensions
    {
        private static ConcurrentDictionary<Type, Func<JsonSerializer, object>> _jsonSerializerAdapterTypes =
            new ConcurrentDictionary<Type, Func<JsonSerializer, object>>();

        private static Func<Type, Func<JsonSerializer, object>> _makeAdapterType => type =>
        {
            var adapterType = typeof(JsonSerializerAdapter<>).MakeGenericType(type);
            return Expression.Lambda<Func<JsonSerializer, object>>(
                Expression.New(adapterType),
                Expression.Parameter(typeof(JsonSerializer))
            )
            .Compile();
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="jsonSerializer"></param>
        /// <returns></returns>
        public static IBsonSerializer GetOrCreateAdapter(
            this JsonSerializer jsonSerializer,
            Type type
        )
        {
            var constructor = _jsonSerializerAdapterTypes.GetOrAdd(type, _makeAdapterType);
            return (IBsonSerializer)constructor(jsonSerializer);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public static IDisposable Push(this JsonSerializer serializer)
        {
            return JsonSerializerAdapter.Push(serializer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static JsonSerializer WithBsonAdapterConfiguration(this JsonSerializer serializer)
        {
            serializer.Converters.Add(BsonValueConverter.Instance);
            serializer.Converters.Add(ObjectIdConverter.Instance);
            return serializer;
        }
    }
}
