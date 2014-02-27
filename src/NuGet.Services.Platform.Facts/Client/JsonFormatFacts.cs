using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Services.Client
{
    public class JsonFormatFacts
    {
        public class TheSerializeMethod
        {
            [Fact]
            public void CamelCasesPropertyNames()
            {
                Assert.Equal(
@"{
  ""fooBar"": 42
}",
                    JsonFormat.Serialize(new { FooBar = 42 }));
            }

            [Fact]
            public void PascalCasesPropertyNamesWhenRequested()
            {
                Assert.Equal(
@"{
  ""FooBar"": 42
}",
                    JsonFormat.Serialize(new { FooBar = 42 }, camelCase: false));
            }

            [Fact]
            public void SerializesFloats()
            {
                Assert.Equal(
@"{
  ""fooBar"": 4.2
}",
                    JsonFormat.Serialize(new { FooBar = 4.2 }));
            }

            [Fact]
            public void IgnoresNullFields()
            {
                Assert.Equal(
@"{}",
                    JsonFormat.Serialize(new { FooBar = (object)null }));
            }

            [Fact]
            public void IncludesDefaultValues()
            {
                Assert.Equal(
@"{
  ""fooBar"": 0
}",
                    JsonFormat.Serialize(new { FooBar = 0 }));
            }

            [Fact]
            public void UsesIsoDateFormat()
            {
                Assert.Equal(
@"{
  ""fooBar"": ""2014-02-27T00:30:42+00:00""
}",
                    JsonFormat.Serialize(new { FooBar = new DateTimeOffset(2014, 02, 27, 00, 30, 42, TimeSpan.Zero) }));
            }
        }

        public class TheDeserializeMethod
        {
            [Fact]
            public void DeserializesToExpectedObject()
            {
                // Act
                var result = JsonFormat.Deserialize<TestObj>(@"{
                    str: 'foo',
                    int: 42,
                    flt: 4.2,
                    date: ""2014-02-27T10:51+00:00"",
                    null: null
                }");

                // Assert
                Assert.Equal("foo", result.Str);
                Assert.Equal(42, result.Int);
                Assert.Equal(4.2, result.Flt, precision: 1);
                Assert.Equal(new DateTimeOffset(2014, 02, 27, 10, 51, 00, TimeSpan.Zero), result.Date);
                Assert.Null(result.Null);
                Assert.Null(result.Missing);
            }
        }

        public class TestObj
        {
            public string Str { get; set; }
            public int Int { get; set; }
            public float Flt { get; set; }
            public DateTimeOffset Date { get; set; }
            public object Null { get; set; }
            public object Missing { get; set; }
        }
    }
}
