using NUnit.Framework;
using Shouldly;

namespace MonorepoSample.AppB.Tests;

[TestFixture]
public sealed class WordCounterTests
{
    [TestCase("", 0)]
    [TestCase("   ", 0)]
    [TestCase("one", 1)]
    [TestCase("  two\twords\n", 2)]
    [TestCase("the quick brown fox", 4)]
    public void Count_CountsWordsBetweenWhitespace(string text, int expected) =>
        WordCounter
            .Count(text)
            .ShouldBe(expected);
}
