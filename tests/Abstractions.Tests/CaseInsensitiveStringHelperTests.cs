namespace Abstractions.Tests
{
    using System.Text;
    using Autocrat.Abstractions;
    using FluentAssertions;
    using Xunit;

    public class CaseInsensitiveStringHelperTests
    {
        public sealed class EqualTests : CaseInsensitiveStringHelperTests
        {
            [Fact]
            public void ShouldReturnFalseIfTwoStringsAreNotEqual()
            {
                AreEqual("ONE", "TWO").Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseIfTwoStringsHaveADifferentLength()
            {
                AreEqual("FIRST", "SECOND").Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfTwoStringsAreEqualButDifferByCase()
            {
                AreEqual("VALUE", "value").Should().BeTrue();
            }

            private static bool AreEqual(string a, string b)
            {
                bool stringResult = CaseInsensitiveStringHelper.Equals(a, b);
                bool byteResult = CaseInsensitiveStringHelper.Equals(a, Encoding.UTF8.GetBytes(b));

                stringResult.Should().Be(byteResult);
                return stringResult;
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
                int first = GetHashCodeFor("one");
                int second = GetHashCodeFor("two");

                first.Should().NotBe(second);
            }

            [Fact]
            public void ShouldReturnTheSameHashCodeForStringsThatHaveDifferentCasing()
            {
                int lowercase = GetHashCodeFor("value");
                int uppercase = GetHashCodeFor("VALUE");

                lowercase.Should().Be(uppercase);
            }

            private static int GetHashCodeFor(string value)
            {
                int stringResult = CaseInsensitiveStringHelper.GetHashCode(value);
                int byteResult = CaseInsensitiveStringHelper.GetHashCode(Encoding.UTF8.GetBytes(value));

                stringResult.Should().Be(byteResult);
                return stringResult;
            }
        }
    }
}
