namespace Novolis.CodeGen.Bindings;

/// <summary>Native symbol names used by debug capture hooks.</summary>
/// <param name="LoadImageFromScreen">LoadImageFromScreen symbol.</param>
/// <param name="ExportImageToMemory">ExportImageToMemory symbol.</param>
/// <param name="UnloadImage">UnloadImage symbol.</param>
/// <param name="MemFree">MemFree symbol.</param>
public sealed record DebugSymbolMapSpec(
    string LoadImageFromScreen,
    string ExportImageToMemory,
    string UnloadImage,
    string MemFree);
