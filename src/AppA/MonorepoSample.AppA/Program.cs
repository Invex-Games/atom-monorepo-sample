using MonorepoSample.AppA;
using MonorepoSample.Shared;

Console.WriteLine(Banner.Format("AppA", Banner.GetVersion(typeof(Greeter).Assembly)));
Console.WriteLine(Greeter.Greet(args));
