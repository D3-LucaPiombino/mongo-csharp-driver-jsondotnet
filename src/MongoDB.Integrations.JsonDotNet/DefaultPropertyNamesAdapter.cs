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


namespace MongoDB.Integrations.JsonDotNet
{
    /// <inheritdoc/>
    public class DefaultPropertyNamesAdapter : IPropertyNamesAdapter
    {
        private static class PropertyNames
        {
            public static class TypeDiscriminator
            {
                public const string MongoDb = "_t";
                public const string JsonNet = "$type";
            }
            public static class Id
            {
                public const string MongoDb = "_id";
                public const string JsonNet = "id";
            }
        }

        /// <inheritdoc/>
        public string ReadName(string propertyName)
        {
            if (propertyName == PropertyNames.TypeDiscriminator.MongoDb)
                return PropertyNames.TypeDiscriminator.JsonNet;
            if (propertyName == PropertyNames.Id.MongoDb)
                return PropertyNames.Id.JsonNet;

            return null;
        }

        /// <inheritdoc/>
        public string WriteName(string propertyName)
        {
            if (propertyName == PropertyNames.TypeDiscriminator.JsonNet)
                return PropertyNames.TypeDiscriminator.MongoDb;

            if (propertyName == PropertyNames.Id.JsonNet)
                return PropertyNames.Id.MongoDb;

            return null;
        }

    }
}
