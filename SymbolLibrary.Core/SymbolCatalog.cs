namespace SymbolLibrary.Core;

public sealed record SymbolEntry(string Name, string Category, string FilePath, GeometryDocument Geometry)
{
    public Bounds Size => Geometry.Bounds;
}

public sealed class SymbolCatalog
{
    private readonly DxfGeometryReader reader = new();
    public (IReadOnlyList<SymbolEntry> Symbols, IReadOnlyList<string> Errors) Load(string directory)
    {
        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException(directory);
        var items = new List<SymbolEntry>();
        var errors = new List<string>();
        foreach (var path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Where(x => string.Equals(Path.GetExtension(x), ".dxf", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x))
        {
            try
            {
                items.Add(new SymbolEntry(Path.GetFileNameWithoutExtension(path).Replace('_',' '), Path.GetFileName(Path.GetDirectoryName(path)) ?? "Symbols", Path.GetFullPath(path), reader.Read(path)));
            }
            catch (Exception ex) { errors.Add($"{Path.GetFileName(path)}: {ex.Message}"); }
        }
        return (items, errors);
    }
    public static IEnumerable<SymbolEntry> Search(IEnumerable<SymbolEntry> symbols, string? text) =>
        string.IsNullOrWhiteSpace(text) ? symbols : symbols.Where(x => x.Name.Contains(text.Trim(), StringComparison.OrdinalIgnoreCase));
}
