using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Novolis.CodeGen.Reflection.Dump;

public static class DumpExtensions
{
    /// <summary>
    /// Dumps the object to a string as initialization code.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="options"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static string DumpVar<T>(this T obj, VarDump.Visitor.DumpOptions? options = null) => VariableFactory.DumpVar(obj, options);
    
    /// <summary>
    /// Dumps the object to a string as a class, including all properties and fields.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="options"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static string DumpClass<T>(this T obj, VarDump.Visitor.DumpOptions? options = null) => ClassFactory.CreateClass(obj, options);
    
    /// <summary>
    /// Dumps the object to a string as a method, including all properties and fields.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="options"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static string DumpMethod<T>(this T obj, VarDump.Visitor.DumpOptions? options = null) => MethodFactory.CreateMethod(obj, options);

    /// <summary>
    /// Dumps the enumerable to a string as a class, including all properties and fields.
    /// </summary>
    /// <param name="objs"></param>
    /// <param name="idSelector"></param>
    /// <param name="options"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static string DumpEnumerable<T>(this IEnumerable<T> objs, Func<T, string> idSelector, VarDump.Visitor.DumpOptions? options = null) => ClassFactory.CreateEnumerableClass(objs, idSelector, options);
    
    /// <summary>
    /// Dumps the object to a class declaration
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="options"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static ClassDeclarationSyntax DumpClassDeclarationSyntax<T>(T obj, VarDump.Visitor.DumpOptions? options = null)
        => SyntaxFactory.ParseSyntaxTree(DumpClass(obj, options)).GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First().NormalizeWhitespace();
    
    /// <summary>
    /// Dumps the object to a method declaration
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="options"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static MethodDeclarationSyntax DumpMethodDeclarationSyntax<T>(T obj, VarDump.Visitor.DumpOptions? options = null)
        => AsMethodDeclarationSyntax(SyntaxFactory.ParseMemberDeclaration(MethodFactory.CreateMethod(obj, options)) ?? throw new Exception("Could not parse method declaration syntax.")).NormalizeWhitespace();

    /// <summary>
    /// Dumps the enumerable to a class declaration
    /// </summary>
    /// <param name="memberDeclarationSyntax"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static MethodDeclarationSyntax AsMethodDeclarationSyntax(MemberDeclarationSyntax memberDeclarationSyntax)
        => memberDeclarationSyntax as MethodDeclarationSyntax ?? throw new Exception("Could not parse method declaration syntax.");
}