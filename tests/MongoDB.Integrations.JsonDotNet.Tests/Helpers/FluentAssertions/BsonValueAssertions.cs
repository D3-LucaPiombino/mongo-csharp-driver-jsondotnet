/* Copyright 2015 MongoDB Inc.
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

using FluentAssertions;
using FluentAssertions.Common;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System;

namespace MongoDB.Integrations.JsonDotNet.Tests.Helpers.FluentAssertions
{
    public static class ObjectExtensions
    {
        public static bool IsSameOrEqualTo(this object actual, object expected)
        {
            if (actual is null && expected is null)
            {
                return true;
            }

            if (actual is null)
            {
                return false;
            }

            if (expected is null)
            {
                return false;
            }

            if (actual.Equals(expected))
            {
                return true;
            }

            var expectedType = expected.GetType();
            var actualType = actual.GetType();

            return actualType != expectedType
                && (actual.IsNumericType() || actualType.IsEnumType())
                && (expected.IsNumericType() || expectedType.IsEnumType())
                && CanConvert(actual, expected, actualType, expectedType)
                && CanConvert(expected, actual, expectedType, actualType);
        }

        private static bool CanConvert(object source, object target, Type sourceType, Type targetType)
        {
            try
            {
                var converted = source.ConvertTo(targetType);

                return source.Equals(converted.ConvertTo(sourceType))
                     && converted.Equals(target);
            }
            catch
            {
                // ignored
                return false;
            }
        }

        private static object ConvertTo(this object source, Type targetType)
        {
            return IsEnumType(targetType)
                ? Enum.ToObject(targetType, source)
                : Convert.ChangeType(source, targetType, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static bool IsNumericType(this object obj)
        {
            switch (obj)
            {
                case int _:
                case long _:
                case float _:
                case double _:
                case decimal _:
                case sbyte _:
                case byte _:
                case short _:
                case ushort _:
                case uint _:
                case ulong _:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsEnumType(this Type type) => type.IsEnum;
    }
    public class BsonValueAssertions : ReferenceTypeAssertions<BsonValue, BsonValueAssertions>
    {
        // constructors
        public BsonValueAssertions(BsonValue value) : base(value) { }

        // methods
        public AndConstraint<BsonValueAssertions> Be(BsonValue expected, string because = "", params object[] reasonArgs)
        {
            
            Execute.Assertion
                .BecauseOf(because, reasonArgs)
                .ForCondition(Subject.IsSameOrEqualTo(expected))
                .FailWith("Expected {context:object} to be {0}{reason}, but found {1}.", expected,
                    Subject);

            return new AndConstraint<BsonValueAssertions>(this);
        }

        public AndConstraint<BsonValueAssertions> Be(string json, string because = "", params object[] reasonArgs)
        {
            var expected = json == null ? null : BsonSerializer.Deserialize<BsonValue>(json);
            return Be(expected, because, reasonArgs);
        }

        public AndConstraint<BsonValueAssertions> NotBe(BsonValue unexpected, string because = "", params object[] reasonArgs)
        {
            Execute.Assertion
                .BecauseOf(because, reasonArgs)
                .ForCondition(!Subject.IsSameOrEqualTo(unexpected))
                .FailWith("Did not expect {context:object} to be equal to {0}{reason}.", unexpected);

            return new AndConstraint<BsonValueAssertions>(this);
        }

        public AndConstraint<BsonValueAssertions> NotBe(string json, string because = "", params object[] reasonArgs)
        {
            var expected = json == null ? null : BsonSerializer.Deserialize<BsonValue>(json);
            return NotBe(expected, because, reasonArgs);
        }

        
        protected override string Identifier => "BsonValue";
    }
}
