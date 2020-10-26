using System;
using Xunit;

namespace JsonMergePatch.UnitTests
{
    public class JsonMergePatchUnitTests
    {
        [Theory]
        [InlineData("{ \"firstName\": \"Test\"} ", true, "Test")] //test actual values
        [InlineData("{ \"firstName\": \"\" }", true, "")] //test empty string
        [InlineData("{ \"firstName\": null }", true, null)] //test nulls
        [InlineData("{ }", false, null)] //extraction should fail if property isn't present
        public void TryGetValue_ExtractsValue(string json, bool expectedSuccess, string expectedValue)
        {
            var patch = JsonMergePatch<Person>.Create(json);

            var success = patch.TryGetValue(x => x.FirstName, out var firstName);

            Assert.Equal(expectedSuccess, success);
            Assert.Equal(expectedValue, firstName);
        }

        [Theory]
        [InlineData("{ \"address\" : { \"city\" : \"TesterVille\" } }", true, "TesterVille")] //should extract a nested value
        [InlineData("{ \"address\" : { } }", false, null)] //should fail if parent is defined, but property is not
        [InlineData("{ }", false, null)] //should fail extraction if parent is undefined
        public void TryGetValue_ExtractsNestedValue(string json, bool expectedSuccess, string expectedValue)
        {
            var patch = JsonMergePatch<Person>.Create(json);

            var success = patch.TryGetValue(x => x.Address.City, out var city);

            Assert.Equal(expectedSuccess, success);
            Assert.Equal(expectedValue, city);
        }

        [Theory]
        [InlineData("{ \"cars\" : [ { \"make\" : \"Tesla\" } ] }", true)] //a filled array should extract
        [InlineData("{ \"cars\" : [ ] }", true)] //an empty array should extract
        [InlineData("{ \"cars\" : null }", true)] //a null array should extract
        [InlineData("{ }", false)] //missing property should not extract
        public void TryGetArray_ExtractsCollections(string json, bool expectedSuccess)
        {
            var patch = JsonMergePatch<Person>.Create(json);
            var success = patch.TryGetArray(x => x.Cars, out var cars);

            Assert.Equal(expectedSuccess, success);
        }

        [Theory]
        [InlineData("{ \"address\" : { } }", true)] //doesn't really matter what the content is, as long as it's defined
        [InlineData("{ }", false)] //extraction should fail if object isn't present
        public void TryGetObject_ExtractsObject(string json, bool expectedSuccess)
        {
            var patch = JsonMergePatch<Person>.Create(json);
            var success = patch.TryGetObject(x => x.Address, out var address);

            Assert.Equal(expectedSuccess, success);
        }

        [Fact]
        public void Set_SetsValue()
        {
            var patch = JsonMergePatch<Person>.Create();

            patch.Set(x => x.FirstName, "Test");
            var model = patch.ToModel();

            Assert.Equal("Test", model.FirstName);
        }

    }
}
