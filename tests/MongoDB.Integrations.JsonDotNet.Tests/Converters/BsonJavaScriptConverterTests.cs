﻿/* Copyright 2015 MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Integrations.JsonDotNet.Converters;
using NUnit.Framework;

namespace MongoDB.Integrations.JsonDotNet.Tests.Converters
{
    [TestFixture]
    public class BsonJavaScriptConverterTests : JsonConverterTestsBase
    {
        [Test]
        public void Instance_get_returns_cached_result()
        {
            var result1 = BsonJavaScriptConverter.Instance;
            var result2 = BsonJavaScriptConverter.Instance;

            result2.Should().BeSameAs(result1);
        }

        [Test]
        public void Instance_get_returns_expected_result()
        {
            var result = BsonJavaScriptConverter.Instance;

            result.Should().NotBeNull();
            result.Should().BeOfType<BsonJavaScriptConverter>();
        }

        [TestCase("{ x : null }", null)]
        [TestCase("{ x : \"abc\" }", "abc")]
        [TestCase("{ x : \"def\" }", "def")]
        [TestCase("{ x : { $code : \"abc\" } }", "abc")]
        [TestCase("{ x : { $code : \"def\" } }", "def")]
        [TestCase("{ x : { $$code : \"abc\" } }", "abc")]
        [TestCase("{ x : { $$code : \"def\" } }", "def")]
        public void ReadJson_should_return_expected_result_when_using_native_bson_reader(string json, string nullableCode)
        {
            var subject = new BsonJavaScriptConverter();
            var expectedResult = nullableCode == null ? null : new BsonJavaScript(nullableCode);

            var result = ReadJsonUsingNativeBsonReader<BsonJavaScript>(subject, ToBson(json), mustBeNested: true);

            result.Should().Be(expectedResult);
        }

        [TestCase("null", null)]
        [TestCase("\"abc\"", "abc")]
        [TestCase("\"def\"", "def")]
        [TestCase("{ $code : \"abc\" }", "abc")]
        [TestCase("{ $code : \"def\" }", "def")]
        public void ReadJson_should_return_expected_result_when_using_native_json_reader(string json, string nullableCode)
        {
            var subject = new BsonJavaScriptConverter();
            var expectedResult = nullableCode == null ? null : new BsonJavaScript(nullableCode);

            var result = ReadJsonUsingNativeJsonReader<BsonJavaScript>(subject, json);

            result.Should().Be(expectedResult);
        }

        [TestCase("{ x : null }", null)]
        [TestCase("{ x : \"abc\" }", "abc")]
        [TestCase("{ x : \"def\" }", "def")]
        [TestCase("{ x : { $code : \"abc\" } }", "abc")]
        [TestCase("{ x : { $code : \"def\" } }", "def")]
        [TestCase("{ x : { $$code : \"abc\" } }", "abc")]
        [TestCase("{ x : { $$code : \"def\" } }", "def")]
        public void ReadJson_should_return_expected_result_when_using_wrapped_bson_reader(string json, string nullableCode)
        {
            var subject = new BsonJavaScriptConverter();
            var expectedResult = nullableCode == null ? null : new BsonJavaScript(nullableCode);

            var result = ReadJsonUsingWrappedBsonReader<BsonJavaScript>(subject, ToBson(json), mustBeNested: true);

            result.Should().Be(expectedResult);
        }

        [TestCase("null", null)]
        [TestCase("\"abc\"", "abc")]
        [TestCase("\"def\"", "def")]
        [TestCase("{ $code : \"abc\" }", "abc")]
        [TestCase("{ $code : \"def\" }", "def")]
        public void ReadJson_should_return_expected_result_when_using_wrapped_json_reader(string json, string nullableCode)
        {
            var subject = new BsonJavaScriptConverter();
            var expectedResult = nullableCode == null ? null : new BsonJavaScript(nullableCode);

            var result = ReadJsonUsingWrappedJsonReader<BsonJavaScript>(subject, json);

            result.Should().Be(expectedResult);
        }

        [Test]
        public void ReadJson_should_throw_when_token_type_is_invalid()
        {
            var subject = new BsonJavaScriptConverter();
            var json = "undefined";

            Action action = () => { var _ = ReadJsonUsingNativeJsonReader<BsonJavaScript>(subject, json); };

            action.Should().Throw<Newtonsoft.Json.JsonReaderException>();
        }

        [TestCase(null, "{ x : null }")]
        [TestCase("abc", "{ x : { $$code : \"abc\" } }")]
        [TestCase("def", "{ x : { $$code : \"def\" } }")]
        public void WriteJson_should_have_expected_result_when_using_native_bson_writer(string nullableCode, string expectedResult)
        {
            var subject = new BsonJavaScriptConverter();
            var value = nullableCode == null ? null : new BsonJavaScript(nullableCode);

            var result = WriteJsonUsingNativeBsonWriter(subject, value, mustBeNested: true);

            result.Should().Equal(ToBson(expectedResult));
        }

        [TestCase(null, "null")]
        [TestCase("abc", "{\"$code\":\"abc\"}")]
        [TestCase("def", "{\"$code\":\"def\"}")]
        public void WriteJson_should_have_expected_result_when_using_native_json_writer(string nullableCode, string expectedResult)
        {
            var subject = new BsonJavaScriptConverter();
            var value = nullableCode == null ? null : new BsonJavaScript(nullableCode);

            var result = WriteJsonUsingNativeJsonWriter(subject, value);

            result.Should().Be(expectedResult);
        }

        [TestCase(null, "{ x : null }")]
        [TestCase("abc", "{ x : { $code : \"abc\" } }")]
        [TestCase("def", "{ x : { $code : \"def\" } }")]
        public void WriteJson_should_have_expected_result_when_using_wrapped_bson_writer(string nullableCode, string expectedResult)
        {
            var subject = new BsonJavaScriptConverter();
            var value = nullableCode == null ? null : new BsonJavaScript(nullableCode);

            var result = WriteJsonUsingWrappedBsonWriter(subject, value, mustBeNested: true);

            result.Should().Equal(ToBson(expectedResult));
        }

        [TestCase(null, "null")]
        [TestCase("abc", "{ \"$code\" : \"abc\" }")]
        [TestCase("def", "{ \"$code\" : \"def\" }")]
        public void WriteJson_should_have_expected_result_when_using_wrapped_json_writer(string nullableCode, string expectedResult)
        {
            var subject = new BsonJavaScriptConverter();
            var value = nullableCode == null ? null : new BsonJavaScript(nullableCode);

            var result = WriteJsonUsingWrappedJsonWriter(subject, value);

            result.Should().Be(expectedResult);
        }
    }
}
