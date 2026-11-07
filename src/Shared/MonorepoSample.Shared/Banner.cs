namespace MonorepoSample.Shared;

/// <summary>
///     Formats the banner that each app prints when it starts.
/// </summary>
public static class Banner
{
    /// <summary>
    ///     Formats a boxed banner with the app's name and version.
    /// </summary>
    /// <param name="product">The app's name, such as <c>AppA</c>.</param>
    /// <param name="version">The app's version, such as <c>1.2.0</c>.</param>
    /// <returns>The banner, three lines long.</returns>
    public static string Format(string product, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(product);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        var title = $" {product.Trim()} {version.Trim()} ";
        var border = new string('-', title.Length);

        return $"+{border}+{Environment.NewLine}|{title}|{Environment.NewLine}+{border}+";
    }

    /// <summary>
    ///     Gets the informational version of an assembly, without any source revision suffix.
    /// </summary>
    /// <param name="assembly">The assembly, usually the app's entry assembly.</param>
    /// <returns>The version, such as <c>1.2.0-rc.3</c>, or <c>0.0.0</c> if the assembly has none.</returns>
    public static string GetVersion(System.Reflection.Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var version = assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()
            ?.InformationalVersion;

        if (string.IsNullOrWhiteSpace(version))
            return "0.0.0";

        // The SDK appends "+<commit>" when it knows the source revision
        var plus = version.IndexOf('+', StringComparison.Ordinal);

        return plus >= 0
            ? version[..plus]
            : version;
    }
}
