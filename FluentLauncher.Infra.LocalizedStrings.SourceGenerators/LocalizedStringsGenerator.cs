using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.SourceGenerators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace FluentLauncher.Infra.Settings.SourceGenerators;

internal record struct ClassInfo(string Namespace, string ClassName);

internal record struct ReswFileInfo(string FilePath)
{
    public string Filename => Path.GetFileNameWithoutExtension(FilePath);
    public string ResourceMapName => Filename.Split('.')[0];
    public string Qualifier => Filename.Substring(ResourceMapName.Length, Filename.Length - ResourceMapName.Length - ".resw".Length);
}

[Generator(LanguageNames.CSharp)]
public class LocalizedStringsGenerator : IIncrementalGenerator
{
    public LocalizedStringsGenerator()
    {
#if DEBUG
        if (!Debugger.IsAttached)
        {
            //Debugger.Launch();
        }
#endif
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all classes with the [GeneratedLocalizedStrings] attribute
        var classDeclarations = context.SyntaxProvider.ForAttributeWithMetadataName(
            "FluentLauncher.Infra.LocalizedStrings.GeneratedLocalizedStringsAttribute",
            static (node, _) => node is ClassDeclarationSyntax,
            static (ctx, token) =>
            {
                // Extract class info
                ITypeSymbol localizedStringClassSymbol = (ITypeSymbol)ctx.TargetSymbol;
                string containingNamespace = localizedStringClassSymbol.ContainingNamespace.ToDisplayString();
                string className = localizedStringClassSymbol.Name;
                return new ClassInfo(containingNamespace, className);
            })
            .Collect();

        // Find all .resw files, group by resource map name and keep the neutral one (or the first in alphabetical order)
        var reswFilesProvider = context.AdditionalTextsProvider
            // Find all .resw files
            .Where(file => file.Path.EndsWith(".resw", StringComparison.OrdinalIgnoreCase))
            .Select((file, token) =>
            {
                var reswFile = new ReswFileInfo(file.Path);
                return (reswFile.ResourceMapName, reswFile);
            })
            .GroupBy(
                static item => item.Left,
                static item => item.Right
            )
            .Select((group, token) => group.Right
                .ToList()
                .OrderBy(file => file.Filename)
                .First()
            )
            .Collect();

        context.RegisterSourceOutput(classDeclarations.Combine(reswFilesProvider), Execute);
    }

    private static void Execute(SourceProductionContext context, (ImmutableArray<ClassInfo> classInfos, ImmutableArray<ReswFileInfo> reswFiles) input)
    {
        var (classes, reswFiles) = input;
        if (classes.IsDefaultOrEmpty || reswFiles.IsDefaultOrEmpty)
            return;

        foreach (var classInfo in classes)
        {
            var namespaceName = classInfo.Namespace;
            var className = classInfo.ClassName;

            // Parse and generate properties for each .resw file
            IEnumerable<string> defaultStringIds = []; // Strings in Resources.resw
            var otherStringIds = new Dictionary<string, IEnumerable<string>>();

            foreach (var reswFile in reswFiles)
            {
                string resourceMapName = reswFile.ResourceMapName;
                if (reswFile.Filename.Equals("Resources", StringComparison.OrdinalIgnoreCase))
                    defaultStringIds = ParseReswFile(reswFile);
                else
                    otherStringIds[resourceMapName] = ParseReswFile(reswFile);
            }

            // Generate the class in the detected namespace
            string source = GenerateClass(namespaceName, className, defaultStringIds, otherStringIds);

            // Add the generated source to the compilation
            context.AddSource($"{namespaceName}.{className}.g.cs", SourceText.From(source, Encoding.UTF8));
        }
    }

    private static IEnumerable<string> ParseReswFile(ReswFileInfo reswFile)
    {
        using var reader = new StreamReader(reswFile.FilePath);

        IEnumerable<string>? stringIds = System.Xml.Linq.XDocument.Load(reader).Root?
            .Elements("data")
            .Select(node => node.Attribute("name")?.Value.Replace(".", "/"))
            .Where(name => !string.IsNullOrWhiteSpace(name))!;

        return stringIds ?? [];
        //properties.Add($"public static string {propertyName} => s_resourceMap.GetValue(\"{namespaceName}/{name}\").ValueAsString;");
    }

    private static string GenerateClass(
        string namespaceName,
        string className,
        IEnumerable<string> defaultStringIds,
        Dictionary<string, IEnumerable<string>> otherStringIds)
    {
        var propertyBuilder = new StringBuilder();

        propertyBuilder.AppendLine("// Default resource map (Resources.resw)");
        foreach (var id in defaultStringIds)
        {
            string propertyName = id.Replace('/', '_').Replace(' ', '_');
            propertyBuilder.AppendLine($"        public static string {propertyName} => s_resourceMap.GetValue(\"/Resources/{id}\").ValueAsString;");
        }

        propertyBuilder.AppendLine("\n        // Other resource maps");
        foreach (var item in otherStringIds)
        {
            string resourceMapName = item.Key;
            IEnumerable<string> stringIds = item.Value;
            propertyBuilder.AppendLine($"        public static class {resourceMapName}")
                   .AppendLine("        {");

            foreach (string id in stringIds)
            {
                string propertyName = id.Replace('/', '_').Replace(' ', '_');
                propertyBuilder.AppendLine($"            public static string {propertyName} => s_resourceMap.GetValue(\"/{resourceMapName}/{id}\").ValueAsString;");
            }

            propertyBuilder.AppendLine("        }");
        }

        return $$"""
            using global::Microsoft.Windows.ApplicationModel.Resources;

            namespace {{namespaceName}}
            {
                static partial class {{className}}
                {
                    private static ResourceManager s_resourceManager;
                    private static ResourceMap s_resourceMap;

                    static {{className}}()
                    {
                        s_resourceManager = new ResourceManager();
                        s_resourceMap = s_resourceManager.MainResourceMap;
                    }

                    {{propertyBuilder}}
                }
            }
            """;
    }
}
