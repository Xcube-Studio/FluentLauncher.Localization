using Csv;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

List<string> Warnings = new();
List<string> Errors = new();

var srcOption = new Option<string>("--src", "The source folder containing the .csv files") { IsRequired = true };
var outOption = new Option<string>("--out", "The output folder for .resw files") { IsRequired = true };
var languagesOption = new Option<IEnumerable<string>>("--languages", "All languages for translation") { IsRequired = true, AllowMultipleArgumentsPerToken = true };
var defaultLanguageOption = new Option<string>("--default-language", () => "", "Default language of the app");
defaultLanguageOption.AddValidator(result =>
{
    IEnumerable<string> languages = result.GetValueForOption(languagesOption)!;
    string defaultLanguage = result.GetValueForOption(defaultLanguageOption)!;
    if (defaultLanguage != "" && !languages.Contains(defaultLanguage))
        result.ErrorMessage = "Default language must be in the list of languages";
});

var rootCommand = new RootCommand("Convert .csv files to .resw files for UWP/WinUI localization");
rootCommand.AddOption(srcOption);
rootCommand.AddOption(outOption);
rootCommand.AddOption(languagesOption);
rootCommand.AddOption(defaultLanguageOption);
rootCommand.SetHandler(ConvertCsvToResw, srcOption, outOption, languagesOption, defaultLanguageOption);
rootCommand.Invoke(args);

void ConvertCsvToResw(string srcPath, string outPath, IEnumerable<string> languages, string defaultLanguage)
{
    DirectoryInfo srcFolder = new(srcPath);
    DirectoryInfo outFolder = new(outPath);

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
        foreach (var str in ParseCsv(file, relativePath, languages))
        {
            foreach (string lang in languages)
            {
                string resourceId = relativePath[0..^".csv".Length].Replace(Path.DirectorySeparatorChar, '_') + "_" + str.GetName();
                strings[lang][resourceId] = str.Translations[lang];
            }
        }

    }

    // Print errors (invalid CSV files)
    Console.ForegroundColor = ConsoleColor.Red;

    foreach (var item in Errors)
        Console.WriteLine(item);

    if (Errors.Count > 0)
    {
        Console.WriteLine($"Failed to generate .resw files due to {Errors.Count} errors.");
        Environment.Exit(-1);
    }

    // Print warnings (missing translations)
    Console.ForegroundColor = ConsoleColor.Yellow;

    foreach (var item in Warnings)
        Console.WriteLine(item);

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

        string outputPath = lang == defaultLanguage
            ? Path.Combine(outFolder.FullName, $"Resources.resw")
            : Path.Combine(outFolder.FullName, $"Resources.lang-{lang}.resw");
        var outputFile = new FileInfo(outputPath);
        File.WriteAllText(outputFile.FullName, reswBuilder.ToString());
        Console.WriteLine($"[INFO] Generated translation for {lang}: {outputFile.FullName}");
    }
    Console.WriteLine($"Successfully generated {strings.First().Value.Count} translations for {languages.Count()} languages.");
}


// Parse a CSV file
IEnumerable<StringResource> ParseCsv(FileInfo csvFile, string relativePath, IEnumerable<string> languages)
{
    var csvLines = CsvReader.ReadFromText(File.ReadAllText(csvFile.FullName));

    // Check CSV headers
    var line = csvLines.FirstOrDefault();
    if (line is null) // Empty file
        return [];

    bool invalid = false;
    if (!line.HasColumn("Id"))
    {
        Errors.Add($"[ERROR] {relativePath}: Missing column \"Id\"");
        invalid = true;
    }

    if (!line.HasColumn("Property"))
    {
        Errors.Add($"[ERROR] {relativePath}: Missing column \"Property\"");
        invalid = true;
    }

    foreach (string lang in languages)
    {
        if (!line.HasColumn(lang))
        {
            Errors.Add($"[ERROR] {relativePath}: Missing column for translation to {lang}");
            invalid = true;
        }
    }

    if (invalid) return [];

    // Parse lines
    IEnumerable<StringResource> lines = csvLines
        .Select(line => ParseLine(line, relativePath, languages))
        .Where(x => x is not null)!;
    return lines;
}

// Parse a line in the CSV file
StringResource? ParseLine(ICsvLine line, string relativePath, IEnumerable<string> languages)
{
    // Error checking
    if (string.IsNullOrWhiteSpace(line["Id"]))
    {
        Errors.Add($"[ERROR] {relativePath}, Line {line.Index}: Id must not be empty");
        return null;
    }

    if (line["Id"].StartsWith('_') && !string.IsNullOrEmpty(line["Property"]))
    {
        Errors.Add($"[ERROR] {relativePath}, Line {line.Index}: Property must be empty for strings for code-behind");
        return null;
    }

    // Parse translations
    Dictionary<string, string> translations = new();

    foreach (string lang in languages)
    {
        if (line[lang] == "") // Missing translation
        {
            Warnings.Add($"[WARNING] {relativePath}, Line {line.Index}: Missing translation to {lang}");
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