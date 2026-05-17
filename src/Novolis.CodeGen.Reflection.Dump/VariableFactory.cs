namespace Novolis.CodeGen.Reflection.Dump;

internal static class VariableFactory
{
    public static string DumpVar<T>(T obj, VarDump.Visitor.DumpOptions? options = null)
    {
        options ??= new VarDump.Visitor.DumpOptions();
        var dumper = new VarDump.CSharpDumper(options);
        var result = dumper.Dump(obj);
        return result;
    }
}