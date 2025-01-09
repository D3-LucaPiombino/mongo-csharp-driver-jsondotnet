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


using Newtonsoft.Json;
using System;

namespace MongoDB.Integrations.JsonDotNet
{
    /// <summary>
    /// Allow to customized property names when an 
    /// object is serialized or deserialized.
    /// Note that this is called erlier thant converters, 
    /// and so it allow to do stuff that is impossible with a 
    /// converter.
    /// An example is changing the name of the property 
    /// used to read/write the discriminator ('$type') that 
    /// provide polimorphism support.
    /// the <see cref="MongoDB.Bson.Serialization.BsonSerializer"/> does 
    /// use '_t' instead of '$type'.
    /// 
    /// This cannot be changed with a <see cref="JsonConverter"/> 
    /// because the <see cref="JsonConverter.CanConvert(System.Type)"/>
    /// require the <see cref="Type"/> that is only known after the
    /// discriminator has been read and mapped.
    /// </summary>
    public interface IPropertyNamesAdapter
    {
        /// <summary>
        /// Return a custom mapping for <paramref name="propertyName"/>  when 
        /// deserializing an object.
        /// Returns <see langword="null"/> if no mapping is performed.
        /// </summary>
        /// <param name="propertyName">The source original property name.</param>
        /// <returns></returns>
        string ReadName(string propertyName);

        /// <summary>
        /// Return a custom mapping for <paramref name="propertyName"/> when serializing an object.
        /// Returns <see langword="null"/> if no mapping is performed.
        /// </summary>
        /// <param name="propertyName">The original property name.</param>
        /// <returns></returns>
        string WriteName(string propertyName);
    }
}
