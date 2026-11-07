using NUnit.Framework;
using Shouldly;

namespace MonorepoSample.AppA.Tests;

[TestFixture]
public sealed class GreeterTests
{
    [Test]
    public void Greet_WithNoNames_GreetsTheWorld() =>
        Greeter
            .Greet([])
            .ShouldBe("Hello, world!");

    [Test]
    public void Greet_WithOneName_GreetsThatPerson() =>
        Greeter
            .Greet([" Ada "])
            .ShouldBe("Hello, Ada!");

    [Test]
    public void Greet_WithSeveralNames_ListsThem() =>
        Greeter
            .Greet(["Ada", "", "Grace", "Linus"])
            .ShouldBe("Hello, Ada, Grace and Linus!");
}
