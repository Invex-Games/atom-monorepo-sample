namespace MonorepoSample.AppB;

/// <summary>
///     Counts the words in AppB's input.
/// </summary>
internal static class WordCounter
{
    /// <summary>
    ///     Counts the words in a text, where a word is a run of characters between whitespace.
    /// </summary>
    /// <param name="text">The text to count.</param>
    /// <returns>The number of words.</returns>
    public static int Count(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
}
