using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

static class Program {
    // Keeps scanning fast / avoids junk. Remove if you really want EVERYTHING.
    static readonly HashSet<string> ExcludeDirs = new(StringComparer.OrdinalIgnoreCase)
        { "bin", "obj", ".git", ".vs", "node_modules" };

    public static int Main(string[] args) {
        if (args.Length < 1) {
            Console.WriteLine("Usage: CsGrab <baseFolderPath>");
            return 2;
        }

        var baseDir = Path.GetFullPath(args[0]);
        if (!Directory.Exists(baseDir)) {
            Console.WriteLine($"Folder not found: {baseDir}");
            return 2;
        }

        var outDir = Directory.GetCurrentDirectory();

        var outInterfaces = Path.Combine(outDir, "_interfaces.txt");
        var outClasses = Path.Combine(outDir, "_classes.txt");
        var outStructs = Path.Combine(outDir, "_structs.txt");
        var outEvents = Path.Combine(outDir, "_events.txt");
        var outEnums = Path.Combine(outDir, "_enums.txt");

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

            // Interfaces
            foreach (var i in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>()) {
                ifaceCount++;
                AppendBlock(sbInterfaces, file, i);
            }

            // Classes (includes nested classes too)
            foreach (var c in root.DescendantNodes().OfType<ClassDeclarationSyntax>()) {
                classCount++;
                AppendBlock(sbClasses, file, c);
            }

            // Structs (includes nested structs too)
            foreach (var s in root.DescendantNodes().OfType<StructDeclarationSyntax>()) {
                structCount++;
                AppendBlock(sbStructs, file, s);
            }

            // Enums (includes nested enums too)
            foreach (var e in root.DescendantNodes().OfType<EnumDeclarationSyntax>()) {
                enumCount++;
                AppendBlock(sbEnums, file, e);
            }

            // Events: both "event X Y;" and "event X Y { add/remove }"
            foreach (var e in root.DescendantNodes().OfType<EventFieldDeclarationSyntax>()) {
                eventCount++;
                AppendBlock(sbEvents, file, e);
            }
            foreach (var e in root.DescendantNodes().OfType<EventDeclarationSyntax>()) {
                eventCount++;
                AppendBlock(sbEvents, file, e);
            }
        }

        File.WriteAllText(outInterfaces, sbInterfaces.ToString(), Encoding.UTF8);
        File.WriteAllText(outClasses, sbClasses.ToString(), Encoding.UTF8);
        File.WriteAllText(outStructs, sbStructs.ToString(), Encoding.UTF8);
        File.WriteAllText(outEnums, sbEnums.ToString(), Encoding.UTF8);
        File.WriteAllText(outEvents, sbEvents.ToString(), Encoding.UTF8);

        Console.WriteLine("Done.");
        Console.WriteLine($"Scanned files: {fileCount}");
        Console.WriteLine($"Interfaces:   {ifaceCount} -> {Path.GetFileName(outInterfaces)}");
        Console.WriteLine($"Classes:      {classCount} -> {Path.GetFileName(outClasses)}");
        Console.WriteLine($"Structs:      {structCount} -> {Path.GetFileName(outStructs)}");
        Console.WriteLine($"Enums:        {enumCount} -> {Path.GetFileName(outEnums)}");
        Console.WriteLine($"Events:       {eventCount} -> {Path.GetFileName(outEvents)}");

        return 0;
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
}
