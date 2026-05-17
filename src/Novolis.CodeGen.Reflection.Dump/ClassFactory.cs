using System.Text;

using Humanizer;

namespace Novolis.CodeGen.Reflection.Dump;

internal static class ClassFactory
{
    public static string CreateEnumerableClass<T>(IEnumerable<T> objs, Func<T, string> idSelector, VarDump.Visitor.DumpOptions? options = null)
    {
        var friendlyName = typeof(T).GetFriendlyName();
        var yieldBuilder = new StringBuilder();
        var membersBuilder = new StringBuilder();
        var ids = new List<string>();

        foreach (var obj in objs)
        {
            var id = ProcessObject<T>(obj, idSelector, ids, membersBuilder, options);
            var methodIdentity = $"Get{id}()";          
            yieldBuilder.AppendLine($"{DumpHelper.GetIndent(2)}yield return {methodIdentity};");
        }

        var type = typeof(T);
        List<string> namespaces = GetNamespaces(type);
        StringBuilder classBuilder = CreateClass(namespaces, friendlyName, type, yieldBuilder, membersBuilder);

        return classBuilder.ToString();
    }
    
    public static string CreateClass<T>(T obj, VarDump.Visitor.DumpOptions? options = null)
    {
        var friendlyName = typeof(T).GetDisplayName();
        var code = MethodFactory.CreateMethod(obj, options);
        
        var codeLines = code.Split('\n').ToList();
        code = string.Join("\n", codeLines.Select(line => $"{DumpHelper.GetIndent()}{line}")).TrimStart().TrimEnd();
        
        var classResult =
            $$"""
              namespace GeneratedCode;

              public static class Generated{{friendlyName}}
              {
                  {{code}}
              }
              """;
        return classResult;
    }

    private static string ProcessObject<T>(T obj, Func<T, string> idSelector, List<string> ids, StringBuilder membersBuilder, VarDump.Visitor.DumpOptions? options)
    {
        var id = idSelector(obj);
        id = DumpHelper.CleanId(id);
        if (ids.Contains(id))
            throw new Exception($"Duplicate id {id} found.{Environment.NewLine}All Ids : {string.Join(", ", ids)}.");
        ids.Add(id);

        var code = MethodFactory.CreateMethod(obj, options);
        code = code.Replace("Get()", $"Get{id}()");
        var codeLines = code.Split(Environment.NewLine).ToList();
        code = string.Join(Environment.NewLine, codeLines.Select(line => $"{DumpHelper.GetIndent(1)}{line}")).TrimStart().TrimEnd();
        
        membersBuilder.AppendLine(code);
        membersBuilder.AppendLine();
    
        return id;
    }
    
    private static List<string> GetNamespaces(Type type)
    {
        var namespaces = new List<string>();
        if (type.IsGenericType)
        {
            foreach (var genericArgument in type.GetGenericArguments())
            {
                var namespaceName = genericArgument.Namespace;
                if (namespaceName != null && !namespaces.Contains(namespaceName))
                    namespaces.Add(namespaceName);
            }
        }
        else
        {
            var namespaceName = type.Namespace;
            if (namespaceName != null && !namespaces.Contains(namespaceName))
                namespaces.Add(namespaceName);
        }
        return namespaces;
    }

    private static StringBuilder CreateClass(List<string> namespaces, string friendlyName, Type type, StringBuilder yieldBuilder, StringBuilder membersBuilder)
    {
        var classBuilder = new StringBuilder();
        foreach (var namespaceName in namespaces)
        {
            classBuilder.AppendLine($"using {namespaceName};");
        }
        classBuilder.AppendLine("using System.Collections;");
        classBuilder.AppendLine();
        classBuilder.AppendLine("namespace TestingInfrastructure.Collections;");
        classBuilder.AppendLine();
        classBuilder.AppendLine($"public class {friendlyName.Pluralize()} : IEnumerable<{friendlyName}>");
        classBuilder.AppendLine("{");
        classBuilder.AppendLine($"{DumpHelper.GetIndent()}public {friendlyName} this[{type.GetFriendlyName()} id] => this.First(); // TODO: Implement indexer.");
        classBuilder.AppendLine();
        classBuilder.AppendLine($"{DumpHelper.GetIndent()}IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();");
        classBuilder.AppendLine();
        classBuilder.AppendLine($"{DumpHelper.GetIndent()}public IEnumerator<{friendlyName}> GetEnumerator()");
        classBuilder.AppendLine($"{DumpHelper.GetIndent()}{{");
        classBuilder.AppendLine(yieldBuilder.ToString().TrimEnd());
        classBuilder.AppendLine($"{DumpHelper.GetIndent()}}}");
        classBuilder.AppendLine();
        classBuilder.AppendLine(membersBuilder.ToString());
        classBuilder.AppendLine();
        classBuilder.AppendLine("}");
        return classBuilder;
    }
}