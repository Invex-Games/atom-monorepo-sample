namespace MonorepoSample.AppA;

/// <summary>
///     Builds AppA's greeting.
/// </summary>
internal static class Greeter
{
    /// <summary>
    ///     Greets the given names, or the world if there are none.
    /// </summary>
    /// <param name="names">The names to greet.</param>
    /// <returns>The greeting.</returns>
    public static string Greet(IReadOnlyList<string> names)
    {
        var people = names
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .ToList();

        return people.Count switch
        {
            0 => "Hello, world!",
            1 => $"Hello, {people[0]}!",
            _ => $"Hello, {string.Join(", ", people[..^1])} and {people[^1]}!",
        };
    }
}
