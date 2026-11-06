using MonorepoSample.AppB;
using MonorepoSample.Shared;

Console.WriteLine(Banner.Format("AppB", Banner.GetVersion(typeof(WordCounter).Assembly)));

// Count the words in the arguments, or in standard input when there are none
var text = args.Length > 0
    ? string.Join(' ', args)
    : await Console.In.ReadToEndAsync();

Console.WriteLine($"{WordCounter.Count(text)} words");
