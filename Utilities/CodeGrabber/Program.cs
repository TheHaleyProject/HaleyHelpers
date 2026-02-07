using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

static class Program {
    static readonly HashSet<string> ExcludeDirs = new(StringComparer.OrdinalIgnoreCase)
        { "bin", "obj", ".git", ".vs", "node_modules" };

    public static int Main(string[] args) {
        if (args.Length < 1) {
            Console.WriteLine("Usage: CodeGrabber.exe <baseFolderPath> [--prefix <name>]");
            return 2;
        }

        var baseDir = Path.GetFullPath(args[0]);
        if (!Directory.Exists(baseDir)) {
            Console.WriteLine($"Folder not found: {baseDir}");
            return 2;
        }

        var prefix = GetPrefix(args);
        prefix = SanitizeFileName(prefix);

        var outDir = Directory.GetCurrentDirectory();

        string FileName(string category)
            => string.IsNullOrWhiteSpace(prefix) ? $"_{category}.txt" : $"{prefix}_{category}.txt";

        var outInterfaces = Path.Combine(outDir, FileName("interfaces"));
        var outClasses = Path.Combine(outDir, FileName("classes"));
        var outStructs = Path.Combine(outDir, FileName("structs"));
        var outEvents = Path.Combine(outDir, FileName("events"));
        var outEnums = Path.Combine(outDir, FileName("enums"));

        var sbInterfaces = new StringBuilder();
        var sbClasses = new StringBuilder();
        var sbStructs = new StringBuilder();
        var sbEvents = new StringBuilder();
        var sbEnums = new StringBuilder();

        int ifaceCount = 0, classCount = 0, structCount = 0, eventCount = 0, enumCount = 0, fileCount = 0;

        foreach (var file in EnumerateCsFiles(baseDir)) {
            fileCount++;

            string text;
            try { text = File.ReadAllText(file); } catch { continue; }

            var tree = CSharpSyntaxTree.ParseText(text);
            var root = tree.GetCompilationUnitRoot();

            foreach (var i in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>()) {
                ifaceCount++;
                AppendBlock(sbInterfaces, file, i);
            }

            foreach (var c in root.DescendantNodes().OfType<ClassDeclarationSyntax>()) {
                classCount++;
                AppendBlock(sbClasses, file, c);
            }

            foreach (var s in root.DescendantNodes().OfType<StructDeclarationSyntax>()) {
                structCount++;
                AppendBlock(sbStructs, file, s);
            }

            foreach (var e in root.DescendantNodes().OfType<EnumDeclarationSyntax>()) {
                enumCount++;
                AppendBlock(sbEnums, file, e);
            }

            foreach (var e in root.DescendantNodes().OfType<EventFieldDeclarationSyntax>()) {
                eventCount++;
                AppendBlock(sbEvents, file, e);
            }

            foreach (var e in root.DescendantNodes().OfType<EventDeclarationSyntax>()) {
                eventCount++;
                AppendBlock(sbEvents, file, e);
            }
        }

        // Only create files if count > 0; delete old ones if count == 0
        var created = new List<string>();
        WriteIfAny(outInterfaces, sbInterfaces, ifaceCount, created);
        WriteIfAny(outClasses, sbClasses, classCount, created);
        WriteIfAny(outStructs, sbStructs, structCount, created);
        WriteIfAny(outEnums, sbEnums, enumCount, created);
        WriteIfAny(outEvents, sbEvents, eventCount, created);

        Console.WriteLine("Done.");
        Console.WriteLine($"Scanned files: {fileCount}");
        if (ifaceCount > 0) Console.WriteLine($"Interfaces:   {ifaceCount} -> {Path.GetFileName(outInterfaces)}");
        else Console.WriteLine($"Interfaces:   0 (no file)");
        if (classCount > 0) Console.WriteLine($"Classes:      {classCount} -> {Path.GetFileName(outClasses)}");
        else Console.WriteLine($"Classes:      0 (no file)");
        if (structCount > 0) Console.WriteLine($"Structs:      {structCount} -> {Path.GetFileName(outStructs)}");
        else Console.WriteLine($"Structs:      0 (no file)");
        if (enumCount > 0) Console.WriteLine($"Enums:        {enumCount} -> {Path.GetFileName(outEnums)}");
        else Console.WriteLine($"Enums:        0 (no file)");
        if (eventCount > 0) Console.WriteLine($"Events:       {eventCount} -> {Path.GetFileName(outEvents)}");
        else Console.WriteLine($"Events:       0 (no file)");

        return 0;
    }

    static void WriteIfAny(string path, StringBuilder sb, int count, List<string> created) {
        try {
            if (count <= 0) {
                if (File.Exists(path)) File.Delete(path); // prevents stale confusion
                return;
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            created.Add(path);
        } catch {
            // keep it simple: ignore write errors, but you can log if you want
        }
    }

    static IEnumerable<string> EnumerateCsFiles(string root) {
        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0) {
            var dir = stack.Pop();

            IEnumerable<string> subDirs = Array.Empty<string>();
            try { subDirs = Directory.EnumerateDirectories(dir); } catch { }

            foreach (var sd in subDirs) {
                var name = Path.GetFileName(sd.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (ExcludeDirs.Contains(name)) continue;
                stack.Push(sd);
            }

            IEnumerable<string> csFiles = Array.Empty<string>();
            try { csFiles = Directory.EnumerateFiles(dir, "*.cs"); } catch { }

            foreach (var f in csFiles)
                yield return Path.GetFullPath(f);
        }
    }

    static void AppendBlock(StringBuilder sb, string file, SyntaxNode node) {
        var ns = GetNamespace(node);
        var container = GetContainingTypes(node);

        sb.AppendLine(new string('-', 100));
        sb.AppendLine($"// File: {file}");
        sb.AppendLine($"// Namespace: {ns}");
        if (!string.IsNullOrWhiteSpace(container))
            sb.AppendLine($"// Container: {container}");
        sb.AppendLine(new string('-', 100));
        sb.AppendLine(node.NormalizeWhitespace().ToFullString());
        sb.AppendLine();
    }

    static string GetNamespace(SyntaxNode node) {
        var parts = node.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Reverse()
            .Select(n => n.Name.ToString())
            .ToArray();

        return parts.Length == 0 ? "(global)" : string.Join(".", parts);
    }

    static string GetContainingTypes(SyntaxNode node) {
        var types = node.Ancestors()
            .OfType<TypeDeclarationSyntax>()
            .Reverse()
            .Select(t => t.Identifier.ValueText)
            .ToArray();

        return types.Length == 0 ? "" : string.Join(".", types);
    }

    static string? GetPrefix(string[] args) {
        // Supports: --prefix queries
        for (int i = 0; i < args.Length; i++) {
            if (string.Equals(args[i], "--prefix", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1];
        }
        return null;
    }

    static string? SanitizeFileName(string? s) {
        if (string.IsNullOrWhiteSpace(s)) return null;

        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');

        return s.Trim();
    }
}
