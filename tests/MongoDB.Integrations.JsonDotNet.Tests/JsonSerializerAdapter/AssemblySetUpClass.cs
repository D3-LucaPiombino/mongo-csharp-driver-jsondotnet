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

using MongoDB.Integrations.JsonDotNet;
using NUnit.Framework;

[SetUpFixture]
public class AssemblySetUpClass
{
    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        // We need to this before running the tests that use 
        // Bson.Serialization.BsonSerializer.LookupSerializer otherwise the
        // test are non deterministic (since the damn serializer registry is static).
        // This will install the provider with the default serializer and will also give
        // us the ability to plug custom serializers for the current logical context.
        MongoDB.Bson.Serialization.BsonSerializer.RegisterSerializationProvider(
            new JsonDotNetSerializationProvider(t => true)
        );
    }

    [OneTimeTearDown]
    public void RunAfterAnyTests()
    {
        // ...
    }
}

