# Fluent Launcher Localization Project

English | [简体中文](README_zh.md)

This repository provides the translation resources and localization toolchain for [Fluent Launcher](https://github.com/Xcube-Studio/Natsurainko.FluentLauncher), which currently supports the following languages:

- English (en-US)
- Simplified Chinese (zh-Hans)
- Traditional Chinese (zh-Hant)
- Russian (ru-RU)
- Ukrainian (uk-UA)

If you wish to add more languages, please submit a PR following [these instructions](#adding-a-language). Once the PR is accepted, support for the new language will be added in the next release.

## Translation Resources

All the strings in Fluent Launcher and their translations for various languages are stored in multiple `.csv` files within the `Views/` directory. To manage the large number of strings in the app efficiently, we will use the relative path to the `Views/` directory and the filename of the `.csv` file to describe the component that owns the strings in the file. During the build process of Fluent Launcher, the localization toolchain will convert these `.csv` files into PRI resources used by the WinUI app.

### `.csv` File Paths

Each `.csv` file in the `Views/` directory typically corresponds to a page in Fluent Launcher. The filename and path should match the corresponding page in [Natsurainko.FluentLauncher/Views](https://github.com/Xcube-Studio/Natsurainko.FluentLauncher/tree/main/Natsurainko.FluentLauncher/Views). Strings in these files may be used in the associated `View.xaml`, `View.xaml.cs`, or `ViewModel.cs`. For some components that only have a `ViewModel.cs`, the strings used are also stored in a separate `.csv` file.

Other strings used in backend code are stored in specific `.csv` files, such as `Views/Exceptions.csv` and `Views/Converters.csv`.

### `.csv` Content

The `.csv` files storing translation resources must use UTF8-BOM encoding. Each row represents a string and its translations for all supported languages. Every `.csv` file must include the following header:

| Id | Property | en-US | zh-Hans | zh-Hant | ru-RU | uk-UA |
| -- | -------- | ----- | ------- | ------- | ----- | ----- |


- `Id` and `Property` describe the string's path in the PRI resource system.

  - If the string is only used in backend code, the `Id` field must start with an `_` prefix, and the `Property` field must be empty.

- Each subsequent column represents a language supported by Fluent Launcher, identified by its language code.

### Adding a Language

Clone this repository:

```bash
git clone https://github.com/Xcube-Studio/FluentLauncher.Localization.git
```

Add a column to the rightmost side of each `.csv` file. Enter the new language code in the header and provide translations for each row. **Do NOT** modify the content of the `Id` and `Property` columns. The headers of all `.csv` files must remain consistent.

You may submit a PR after completing partial translations. The Fluent Launcher build system allows missing translations. Untranslated strings will fall back to the content of the `en-US` column.

## WinUI 3 Localization Toolchain

### `.resw` Generator

`FluentLauncher.Infra.Localizer` is a C# command-line tool for converting all `.csv` files in the `Views/` directory into a set of `Resources.lang-<lang>.resw` files that can be used by WinUI apps, where `<lang>` is the language code of a supported language. You can also specify a default language using the `--default-language` option. Translations of the default language will be stored in the `Resources.resw` file, which will provide the default translations of strings in the app.

Each item in a `.csv` file represetns a PRI string resource. The PRI resource ID is formatted using the following pattern:  
`<path_relative_to_Views_directory>_<csv_file_name>_<resourceId>/<resourcePropertyId>`.

Example: An entry in `Views/Settings/AboutPage.csv`:

| Id | Property | en-US             | zh-Hans  | zh-Hant  | ru-RU  | uk-UA      |
| -- | -------- | ----------------- | -------- | -------- | ------ | ---------- |
| T1 | Text     | Other information | 其它信息 | 其它信息 | Другая | информация |

Will output to the `.resw` file as:

```xml
<data name="Settings_AboutPage_T1.Text" xml:space="preserve">
  <value>Other information</value>
</data>
```

Here, `<value>` contains the translation for the corresponding language.

Usage of the command-line program:

````
Description:
  Convert .csv files to .resw files for UWP/WinUI localization

Usage:
  FluentLauncher.Infra.Localizer [options]

Options:
  --src <src> (REQUIRED)                 The source folder containing the .csv files
  --out <out> (REQUIRED)                 The output folder for .resw files
  --languages <languages> (REQUIRED)     All languages for translation
  --default-language <default-language>  Default language of the app []
  --version                              Show version information
  -?, -h, --help                         Show help and usage information
````

### `.resw` Resource Accessor Source Generator

To simplify the access of PRI string resources in a WinUI project using C#, a Roslyn source generator is provided to generate the code that reads strings in the `.resw` files. The `FluentLauncher.Infra.LocalizedStrings` project provides the `GeneratedLocalizedStringsAttribute` for marking a `partial` static class, in which the string accessors will be generated as properties. Since `.resw` files are not C# source files, they need to be included as `AdditionalFiles` in the `.csproj` file of the WinUI project to allow the source generator to read them.

These tools can be used in any C# WinUI project by referencing the following projects:

```xml
<ItemGroup>
    <ProjectReference Include="..\FluentLauncher.Localization\FluentLauncher.Infra.LocalizedStrings.SourceGenerators\FluentLauncher.Infra.LocalizedStrings.SourceGenerators.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
    <ProjectReference Include="..\FluentLauncher.Localization\FluentLauncher.Infra.LocalizedStrings\FluentLauncher.Infra.LocalizedStrings.csproj" />
</ItemGroup>
```

Example: If the project contains two `.resw` files:

- `Resources.resw` with two resources:

  - `Button1.Content`
  - `Sample_Message`

- `TextBlocks.resw` with one resource:

  - `DemoTextBlock.Text`


The source generator will produce the following code:

```csharp
[GeneratedLocalizedStrings]
partial static class LocalizedStrings
{
    private static ResourceManager s_resourceManager;
    private static ResourceMap s_resourceMap;

    static LocalizedStrings()
    {
        s_resourceManager = new();
        s_resourceMap = s_resourceManager.MainResourceMap;
    }

    // Default resource map (Resources.resw)
    public static string Button1_Content => s_resourceMap.GetValue("Resources/Button1/Content").ValueAsString;
    public static string Sample_Message => s_resourceMap.GetValue("Resources/Sample_Message").ValueAsString;

    // Other resw files
    public static class TextBlocks
    {
        public static string DemoTextBlock_Text => s_resourceMap.GetValue("TextBlocks/DemoTextBlock/Text").ValueAsString;
    }

    public static void UpdateLanguage()
    {
        s_resourceManager = new();
        s_resourceMap = s_resourceManager.MainResourceMap;
    }
}
```

Note: `Resources.resw` provides the default resource group in WinUI, and the source generator will generates accessors as members of the class marked by `GeneratedLocalizedStrings`. For other `.resw` files, the source generator creates a nested class named after the resource group and generates accessors within it.

If the project contains multiple `.resw` files with the same name but different qualifiers, the source generator will prioritize the file without a qualifier. If all versions have qualifiers, the alphabetically first file name will be used for code generation.

