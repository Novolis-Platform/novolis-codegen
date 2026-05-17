namespace Novolis.CodeGen.Reflection.Dump;

internal static class MethodFactory
{
    public static string CreateMethod<T>(T obj, VarDump.Visitor.DumpOptions? options = null)
    {
        var friendlyName = typeof(T).GetFriendlyName();
        var code = obj.DumpVar(options);
        code = DumpHelper.ReplaceVarDeclaration<T>(code);
        
        var codeLines = code.Split('\n').ToList();
        code = string.Join("\n", codeLines.Select(line => $"{DumpHelper.GetIndent()}{line}")).TrimStart().TrimEnd();
        
        var classResult =
            $$"""
              public static {{friendlyName}} Get()
              {
              {{DumpHelper.GetIndent()}}{{code}}
              }
              """;
        return classResult;
    }

}