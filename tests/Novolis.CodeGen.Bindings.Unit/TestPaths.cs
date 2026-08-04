namespace Novolis.CodeGen.Bindings.Unit;

internal static class TestPaths
{
    public static string Root(string name) =>
        Path.GetFullPath(Path.Combine(Path.DirectorySeparatorChar == '\\' ? @"C:\novolis" : "/novolis", name));

    public static string Combine(string root, params string[] parts)
    {
        var path = root;
        foreach (var part in parts)
            path = Path.Combine(path, part);
        return path;
    }
}
