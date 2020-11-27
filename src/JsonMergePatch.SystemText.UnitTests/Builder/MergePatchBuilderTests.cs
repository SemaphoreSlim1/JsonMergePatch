using JsonMergePatch.Core;
using JsonMergePatch.Core.Builder;
using JsonMergePatch.NewtonsoftJson;
using JsonMergePatch.UnitTests.ExampleModel;
using System;
using System.Collections.Generic;
using Xunit;

namespace JsonMergePatch.UnitTests.Builder
{

    public class NewtonsoftMergePatchBuilderTests : MergePatchBuilderTests
    {
        protected override IJsonMergePatch<T> Create<T>(string json)
        {
            return NewtonsoftJson.JsonMergePatch.CreateFromJson<T>(json);
        }

        protected override PatchBuilder<T> CreateBuilder<T>()
        {
            return NewtonsoftJson.JsonMergePatch.CreateBuilder<T>();
        }
    }
    public abstract class MergePatchBuilderTests
    {
        protected abstract IJsonMergePatch<T> Create<T>(string json);
        protected abstract PatchBuilder<T> CreateBuilder<T>();

        [Theory]
        [InlineData("{ \"FirstName\":\"Unit Test\"}")]
        public void Builder_ApplyMapping_ExecutesOnBuild(string sourceJson)
        {
            var source = Create<Person>(sourceJson);
            var mpb = CreateBuilder<SimplifiedPerson>();

            mpb.Set(x => x.FullName).To(source).Property(p => p.FirstName);

            var mergePatch = mpb.Build();

            var success = mergePatch.TryGetValue(x => x.FullName, out var fullName);
            Assert.True(success);
            Assert.Equal("Unit Test", fullName);
        }

        [Theory]
        [InlineData("{ \"FirstName\": \"Unit\" }", "Unit")]
        [InlineData("{ \"LastName\": \"Test\" }", "Test")]
        [InlineData("{ \"FirstName\": \"Unit\", \"LastName\": \"Test\" }", "Unit Test")]
        public void Builder_CustomMapping_ExecutesSourceFunction(string sourceJson, string expectedFullName)
        {
            var source = Create<Person>(sourceJson);
            var mpb = CreateBuilder<SimplifiedPerson>();

            //use a resolver function to provide the value
            mpb.Set(x => x.FullName).ToValue(() =>
            {
                var nameParts = new List<string>();

                if (source.TryGetValue(x => x.FirstName, out var fName))
                { nameParts.Add(fName); }

                if (source.TryGetValue(x => x.LastName, out var lName))
                { nameParts.Add(lName); }

                return string.Join(" ", nameParts);
            });
            var mergePatch = mpb.Build();

            var success = mergePatch.TryGetValue(x => x.FullName, out var fullName);
            Assert.True(success);
            Assert.Equal(expectedFullName, fullName);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Builder_ConvertValue_OnlyCalledIfIfReturnsTrue(bool shouldConvert)
        {
            var conversionCalled = false;

            var source = Create<Person>("{ }");
            var mpb = CreateBuilder<SimplifiedPerson>();

            source.Set(x => x.CreationDate, new DateTime(2020, 1, 1));

            mpb.Set(mp => mp.Creation).To(source).Property(p => p.CreationDate)
                                                    .OnlyIf(dt => shouldConvert)
                                                    .UsingConversion(dt =>
                                                    {
                                                        conversionCalled = true;
                                                        return dt?.ToString("yyyy-MM-dd");
                                                    });

            var mergePatch = mpb.Build();

            Assert.Equal(shouldConvert, conversionCalled);
            var success = mergePatch.TryGetValue(x => x.Creation, out var createDate);

            if (shouldConvert)
            {
                Assert.True(success);
                Assert.Equal("2020-01-01", createDate);
            }
            else
            {
                Assert.False(success);
                Assert.Null(createDate);
            }
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("2001-01-01", true)]
        public void Builder_ConvertsTypes(string creationDateStr, bool shouldConvert)
        {
            var source = Create<Person>("{ }");
            var mpb = CreateBuilder<SimplifiedPerson>();

            DateTime? creationDate = string.IsNullOrWhiteSpace(creationDateStr) ? null : (DateTime?)DateTime.Parse(creationDateStr);
            source.Set(x => x.CreationDate, creationDate);

            mpb.Set(mp => mp.Creation).To(source).Property(p => p.CreationDate)
                                                 .OnlyIf().NotNull()
                                                 .UsingConversion(dt => dt?.ToString("yyyy-MM-dd"));

            var mergePatch = mpb.Build();

            var success = mergePatch.TryGetValue(x => x.Creation, out var createDate);

            if (shouldConvert)
            {
                Assert.True(success);
                Assert.Equal(creationDateStr, createDate);
            }
            else
            {
                Assert.False(success);
                Assert.Null(createDate);
            }
        }

        [Fact]
        public void Builder_DoesNotHandleImplicitConversion()
        {
            var source = Create<Person>("{ }");
            var mpb = CreateBuilder<SimplifiedPerson>();

            source.Set(x => x.CreationDate, new DateTime(2020, 1, 1));

            mpb.Set(mp => mp.Creation).To(source).Property(p => p.CreationDate);

            Assert.Throws<InvalidCastException>(() =>
            {
                mpb.Build();
            });
        }

        [Fact]
        public void Builder_AutoExpandsUnsetPropertiesToSetValue()
        {
            var mpb = CreateBuilder<Person>();

            mpb.Set(x => x.Address.State.Abbreviation).ToValue("TX");

            var mergePatch = mpb.Build();

            var addressSuccess = mergePatch.TryGetValue(x => x.Address, out var addr);
            var stateSuccess = mergePatch.TryGetValue(x => x.Address.State, out var state);
            var abbrevSuccess = mergePatch.TryGetValue(x => x.Address.State.Abbreviation, out var abbrev);

            Assert.True(addressSuccess);
            Assert.NotNull(addr);

            Assert.True(stateSuccess);
            Assert.NotNull(state);

            Assert.True(abbrevSuccess);
            Assert.Equal("TX", abbrev);
        }
    }
}
