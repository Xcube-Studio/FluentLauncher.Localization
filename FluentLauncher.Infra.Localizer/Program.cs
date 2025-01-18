using Csv;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

// Input arguments
DirectoryInfo srcFolder = new(args[0]);
DirectoryInfo outFolder = new(args[1]);

List<string> languages = [
    "en-US",
    "zh-Hans",
    "zh-Hant",
    "ru-RU",
    "uk-UA"
];

List<string> warnings = new();
List<string> errors = new();

// Init string resource table (key=language code, value=translated string resources)
var strings = new Dictionary<string, Dictionary<string, string>>();
foreach (string lang in languages)
{
    strings[lang] = new();
}

// Enumerate and parse all CSV files
foreach (FileInfo file in srcFolder.EnumerateFiles("*.csv", SearchOption.AllDirectories))
{
    string relativePath = Path.GetRelativePath(srcFolder.FullName, file.FullName);
    foreach (var str in ParseCsv(file, relativePath))
    {
        foreach (string lang in languages)
        {
            string resourceId = relativePath[0..^".csv".Length].Replace(Path.DirectorySeparatorChar, '_') + "_" + str.GetName();
            strings[lang][resourceId] = str.Translations[lang];
        }
    }

}

// Print warnings (missing translations)
Console.ForegroundColor = ConsoleColor.Yellow;

foreach (var item in warnings) 
    Console.WriteLine(item);

// Print errors (invalid CSV files)
Console.ForegroundColor = ConsoleColor.Red;

foreach (var item in errors)
    Console.WriteLine(item);

if (errors.Count > 0)
    Environment.Exit(-1);

Console.ForegroundColor = ConsoleColor.Green;

// Generate .resw files
if (!Directory.Exists(outFolder.FullName))
    Directory.CreateDirectory(outFolder.FullName);

foreach (string lang in languages)
{
    // Build .resw file
    var reswBuilder = new StringBuilder();

    reswBuilder.AppendLine("""
        <?xml version="1.0" encoding="utf-8"?>
        <root>
            <resheader name="resmimetype">
                <value>text/microsoft-resx</value>
            </resheader>
            <resheader name="version">
                <value>2.0</value>
            </resheader>
            <resheader name="reader">
                <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
            </resheader>
            <resheader name="writer">
                <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
            </resheader>
        """);

    foreach ((string key, string translatedString) in strings[lang])
    {
        reswBuilder.AppendLine($"""
            <data name="{key}" xml:space="preserve">
                <value>{translatedString}</value>
            </data>
        """);
    }

    reswBuilder.AppendLine("""
        </root>
        """);

    // Write to file
    var outputFile = new FileInfo(Path.Combine(outFolder.FullName, $"Resources.lang-{lang}.resw"));
    File.WriteAllText(outputFile.FullName, reswBuilder.ToString());
    Console.WriteLine($"[INFO] 已生成资源文件：{outputFile.FullName}");
}


// Parse a CSV file
IEnumerable<StringResource> ParseCsv(FileInfo csvFile, string relativePath)
{
    using var csvFileStream = csvFile.OpenRead();
    IEnumerable<StringResource> lines = CsvReader.ReadFromStream(csvFileStream)
        .Select(line => ParseLine(line, relativePath))
        .Where(x => x is not null)!;
    return lines;
}

// Parse a line in the CSV file
StringResource? ParseLine(ICsvLine line, string relativePath)
{
    // Error checking for CSV file
    if (!line.HasColumn("Id") || string.IsNullOrWhiteSpace(line["Id"]))
    {
        errors.Add($"[ERROR]：at {relativePath}, Line {line.Index} : 资源Id 不能为空");
        return null;
    }

    if (!line.HasColumn("Property"))
    {
        errors.Add($"[ERROR]：at {relativePath}, Line {line.Index} : 缺少Property列");
        return null;
    }

    if (line["Id"].StartsWith('_') && !string.IsNullOrEmpty(line["Property"]))
    {
        errors.Add($"[ERROR]：at {relativePath}, Line {line.Index} : 资源Id 标记为后台代码，但 资源属性Id 又不为空");
        return null;
    }

    // Parse translations
    Dictionary<string, string> translations = new();

    foreach (string lang in languages)
    {
        if (!line.HasColumn(lang))
        {
            errors.Add($"[ERROR]：at {relativePath}, Line {line.Index} : 缺少语言 {lang}");
            return null;
        }

        if (line[lang] == "")
        {
            warnings.Add($"[WARN]：at {relativePath}, Line {line.Index} : 缺少 {lang} 的翻译");
        }

        translations[lang] = line[lang];
    }

    var resource = new StringResource
    {
        Uid = line["Id"],
        Property = line["Property"],
        Translations = translations
    };

    return resource;
}


/// <summary>
/// Represents a string resource with translations for different languages
/// </summary>
record class StringResource
{
    /// <summary>
    /// x:Uid of the component (if used in XAML) or ID of the resource (if used in code behind)
    /// </summary>
    public required string Uid { get; init; }

    /// <summary>
    /// Property name of the component (if used in XAML)
    /// </summary>
    public required string Property { get; init; }

    /// <summary>
    /// Translations for different languages (key=language code, value=translated string)
    /// </summary>
    public required Dictionary<string, string> Translations { get; init; }

    public string GetName()
    {
        if (Uid.StartsWith('_'))
            return Uid;

        return $"{Uid}.{Property}";
    }
}