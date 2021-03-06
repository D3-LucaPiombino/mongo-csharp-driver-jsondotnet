﻿/* Copyright 2015-2016 MongoDB Inc.
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
using System.Linq;

namespace MongoDB.Integrations.JsonDotNet
{

    /// <summary>
    /// A map between a <see cref="Type"/> and a (scoped) discriminator 
    /// (defined as AssemblyName(scope) and TypeName(discriminator) ).
    /// </summary>
    public class TypeNameMapping
    {
        /// <summary>
        /// 
        /// </summary>
        [Flags]
        public enum Direction
        {
            /// <summary>
            /// 
            /// </summary>
            All = In | Out,
            /// <summary>
            /// 
            /// </summary>
            In = 2,
            /// <summary>
            /// 
            /// </summary>
            Out = 4
        }
        /// <summary>
        /// <see langword="true"/> if this is the name 
        /// to use when serializing the associated <see cref="Type"/>.
        /// </summary>
        public Direction Dir { get; set; }
        
        /// <summary>
        /// The assembly name or Scope. 
        /// Can be empty.
        /// </summary>
        public string AssemblyName { get; set; }
        
        /// <summary>
        /// The type name or discriminator. 
        /// Cannot be empty
        /// </summary>
        public string TypeName { get; set; }

        

        /// <summary>
        /// 
        /// </summary>
        public static implicit operator TypeNameMapping((string assemblyName, string typeName, Direction direction) t)
        {
            return new TypeNameMapping
            {
                Dir = t.direction,
                AssemblyName = t.assemblyName,
                TypeName = t.typeName
            };
        }

        /// <summary>
        /// 
        /// </summary>
        public static implicit operator TypeNameMapping((string assemblyName, string typeName) t)
        {
            return new TypeNameMapping
            {
                Dir = Direction.In,
                AssemblyName = t.assemblyName,
                TypeName = t.typeName
            };
        }

        /// <summary>
        /// 
        /// </summary>
        public static implicit operator TypeNameMapping((string typeName, Direction direction) t)
        {
            return new TypeNameMapping
            {
                Dir = t.direction,
                TypeName = t.typeName
            };
        }
        /// <summary>
        /// 
        /// </summary>
        public static implicit operator TypeNameMapping(string typeName)
        {
            return new TypeNameMapping
            {
                Dir = Direction.All,
                TypeName = typeName
            };
        }
    }

    
    /// <summary>
    /// Allow to map each type with multiple names <see cref="TypeNameMapping"/>
    /// </summary>
    public class TypeNameMap : Dictionary<Type, List<TypeNameMapping>>
    {
        /// <summary>
        /// Optional function that if provided is invoked to resolve the scope.
        /// Will override the value of AssemblyName
        /// </summary>
        public Func<(string assemblyName, string name), (string assemblyName, string name)> ResolveDeserializationTypeName { get; set; }

        /// <inheritdoc/>
        public new List<TypeNameMapping> this[Type type]
        {
            get
            {
                if(!TryGetValue(type, out var mappings))
                {
                    mappings = new List<TypeNameMapping>();
                    base[type] = mappings; 
                }
                return mappings;
            }
            set
            {
                base[type] = value;
            }
        }

        
    }

    /// <summary>
    /// Allows users to control class loading and mandate what class to load,
    /// by providing a custom mapping that is used to lookup the correct type/name.
    /// Each type can be mapped to one or more names (map multiple discriminators
    /// to a single type) but only one discriminator can be used for serialization
    /// (the one where <see cref="TypeNameMapping.Dir"/> is set to <see langword="true"/>.
    /// If the mapping is not found(or no type map is provided) the request if delegated to the
    /// inner <see cref="Newtonsoft.Json.Serialization.ISerializationBinder"/> instance provided 
    /// in the constructor.
    /// 
    /// This does also implement <see cref="IPropertyNamesAdapter"/> by forwarding any call 
    /// to methods to the <see cref="IPropertyNamesAdapter"/> instance provided in the constructor.
    /// </summary>
    public class JsonCompositeSerializationBinderAdapter :
        Newtonsoft.Json.Serialization.ISerializationBinder,
        IPropertyNamesAdapter
    {
        private readonly Newtonsoft.Json.Serialization.ISerializationBinder _inner;

        private readonly Dictionary<Type, (string assemblyName, string typeName)> _typeLookup;
        private readonly Dictionary<(string assemblyName, string typeName), Type> _reverseTypeLookup;
       
        private readonly IPropertyNamesAdapter _propertyNameAdapter;
        private readonly TypeNameMap _typeMap;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nameBinder"></param>
        /// <param name="inner"></param>
        /// <param name="typeMap"></param>
        public JsonCompositeSerializationBinderAdapter(
            IPropertyNamesAdapter nameBinder,
            TypeNameMap typeMap = null,
            Newtonsoft.Json.Serialization.ISerializationBinder inner = null
        )
        {
            _inner = inner ?? new Newtonsoft.Json.Serialization.DefaultSerializationBinder();

            if(typeMap != null)
            {
                _reverseTypeLookup = typeMap
                    .SelectMany(item => item
                        .Value
                        .Where(tm => tm.Dir.HasFlag(TypeNameMapping.Direction.In))
                        .Select(tm => (key: (tm.AssemblyName, tm.TypeName), value: item.Key))
                        .Take(1)
                    )
                    .ToDictionary(item => item.key, item => item.value);

                _typeLookup = typeMap
                    .SelectMany(item => item
                        .Value
                        .Where(tm => tm.Dir.HasFlag(TypeNameMapping.Direction.Out))
                        .Select(tm => (key: item.Key, value: (tm.AssemblyName, tm.TypeName)))
                        .Take(1)
                    )
                    .ToDictionary(item => item.key, item => item.value);
            }

            _propertyNameAdapter = nameBinder ?? new DefaultPropertyNamesAdapter();
            _typeMap = typeMap;
        }

        /// <inheritdoc/>
        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            if (_reverseTypeLookup != null &&
               _typeLookup.TryGetValue(serializedType, out var name))
            {
                (assemblyName, typeName) = name;
                return;
            }
            _inner.BindToName(serializedType, out assemblyName, out typeName);
        }

        /// <inheritdoc/>
        public Type BindToType(string assemblyName, string typeName)
        {
            var key = (assemblyName, typeName);
            if(_typeMap.ResolveDeserializationTypeName != null)
            {
                key = _typeMap.ResolveDeserializationTypeName(key);
            }

            if (_reverseTypeLookup != null && 
               _reverseTypeLookup.TryGetValue(key, out var type))
            {
                return type;
            }

            return _inner.BindToType(assemblyName, typeName);
        }
        
        /// <inheritdoc/>
        public string ReadName(string propertyName) =>
            _propertyNameAdapter?.ReadName(propertyName);

        /// <inheritdoc/>
        public string WriteName(string propertyName) =>
            _propertyNameAdapter?.WriteName(propertyName);
    }
}
