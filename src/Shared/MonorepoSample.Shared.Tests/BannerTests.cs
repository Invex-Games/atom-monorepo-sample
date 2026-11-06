using NUnit.Framework;
using Shouldly;

namespace MonorepoSample.Shared.Tests;

[TestFixture]
public sealed class BannerTests
{
    [Test]
    public void Format_BoxesTheNameAndVersion() =>
        Banner
            .Format("AppA", "1.2.0")
            .ShouldBe(string.Join(Environment.NewLine, "+------------+", "| AppA 1.2.0 |", "+------------+"));

    [TestCase("")]
    [TestCase(" ")]
    public void Format_WithBlankProduct_Throws(string product) =>
        Should.Throw<ArgumentException>(() => Banner.Format(product, "1.0.0"));

    [Test]
    public void GetVersion_DropsTheSourceRevision() =>
        Banner
            .GetVersion(typeof(BannerTests).Assembly)
            .ShouldNotContain("+");
}
