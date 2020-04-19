namespace Abstractions.Tests
{
    using System;
    using System.Text;
    using Autocrat.Abstractions;
    using FluentAssertions;
    using Xunit;

    public class CaseInsensitiveStringHelperTests
    {
        private static ReadOnlySpan<byte> GetBytes(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        public sealed class EqualTests : CaseInsensitiveStringHelperTests
        {
            [Fact]
            public void ShouldReturnFalseIfTwoStringsAreNotEqual()
            {
                bool result = CaseInsensitiveStringHelper.Equals("ONE", GetBytes("TWO"));

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseIfTwoStringsHaveADifferentLength()
            {
                bool result = CaseInsensitiveStringHelper.Equals("FIRST", GetBytes("SECOND"));

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfTwoStringsAreEqualButDifferByCase()
            {
                bool result = CaseInsensitiveStringHelper.Equals("VALUE", GetBytes("value"));

                result.Should().BeTrue();
            }
        }

        public sealed class GetHashCodeTests : CaseInsensitiveStringHelperTests
        {
            [Fact]
            public void ShouldReturnDifferentHashCodesForDifferentStrings()
            {
                // GetHashCode could return the same hash code for all values
                // and still be valid, though not useful. The way the algorithm
                // is implemented we know for values of the same length it will
                // produce different results
                int first = CaseInsensitiveStringHelper.GetHashCode(GetBytes("one"));
                int second = CaseInsensitiveStringHelper.GetHashCode(GetBytes("two"));

                first.Should().NotBe(second);
            }

            [Fact]
            public void ShouldReturnTheSameHashCodeForStringsThatHaveDifferentCasing()
            {
                int lowercase = CaseInsensitiveStringHelper.GetHashCode(GetBytes("value"));
                int uppercase = CaseInsensitiveStringHelper.GetHashCode(GetBytes("VALUE"));

                lowercase.Should().Be(uppercase);
            }
        }
    }
}
