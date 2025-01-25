# Fluent Launcher 本地化计划

[English](README.md) | 简体中文

本仓库用于提供 [Fluent Launcher](https://github.com/Xcube-Studio/Natsurainko.FluentLauncher) 所需要的翻译资源和本地化工具链。Fluent Launcher 目前支持的语言如下：

- 英语 (en-US)
- 简体中文 (zh-Hans)
- 繁体中文 (zh-Hant)
- 俄语 (ru-RU)
- 乌克兰语 (uk-UA)

如果您希望添加更多语言，请根据[以下指示](#添加一种语言)提交 PR 。在 PR 被接受之后，我们会在下一个版本中添加对该语言的支持。

## 翻译资源

Fluent Launcher 需要的所有字符串以及对应不同语言的翻译都存储在 `Views/` 目录中的多个  `.csv`  文件中。为方便管理不同组件使用的字符串，我们使用从 `Views/` 目录开始的文件路径与文件名描述一个 `.csv` 文件对应模块的层级与名称。在构建 Fluent Launcher 时，这些 `.csv` 文件将会通过翻译工具链被转换成可以被 WinUI App 使用的 PRI 资源。

### `.csv` 文件路径

`Views/` 目录中的每个 `.csv` 文件通常对应 Fluent Launcher 的一个页面，并且该文件的文件名与路径都和该页面在 [Natsurainko.FluentLauncher/Views](https://github.com/Xcube-Studio/Natsurainko.FluentLauncher/tree/main/Natsurainko.FluentLauncher/Views) 中的路径一致。这些字符串可能在该页面对应的 `View.xaml`, `View.xaml.cs` 或 `ViewModel.cs` 中被使用。对于一些仅有 `ViewModel.cs` 的组件，其中使用的字符串也被存储在一个单独的 `.csv` 文件中。

后端代码中使用的其它字符串在一些特定的 `.csv` 文件存储，例如 `Views/Exceptions.csv`, `Views/Converters.csv` 。

### `.csv` 内容

存储翻译资源的 `.csv` 文件均使用 UTF8-BOM 编码，其中每一行表示一个字符串及其对应所有受支持语言的翻译。每个 `.csv` 文件必须包含以下表头：

| Id | Property | en-US | zh-Hans | zh-Hant | ru-RU | uk-UA |
| -- | -------- | ----- | ------- | ------- | ----- | ----- |


- `Id` 和 `Property` 用于描述该字符串在 PRI 资源系统中的路径。

  - 若该字符串仅在后台代码中使用，应该在 `Id` 部分前面加上 `_` 前缀，且该行的 `Property` 必须留空。

- 之后的每一列表示一种 Fluent Launcher 支持的语言的语言代码。

### 添加一种语言

克隆本仓库：

```bash
git clone https://github.com/Xcube-Studio/FluentLauncher.Localization.git
```

在每个 `.csv` 文件的最右侧添加一列，在表头中填写新的语言代码，然后在每一行填写翻译。**请勿**修改 Id 和 Property 列的内容。所有 `.csv` 文件的表头必须保持一致。

您可以在完成部分翻译后提交 PR ，Fluent Launcher 的构建系统允许部分翻译的缺失，未完成翻译的字符串将会使用 en-US 列的内容显示。

## WinUI 3 本地化工具链

### `.resw` 生成器

`FluentLauncher.Infra.Localizer` 是一个使用 C# 编写的命令行程序，用于将 `Views/` 目录中的所有 `.csv` 文件转换为一组可以被 WinUI App 使用的 `Resources.lang-<lang>.resw` 文件。每个 `.resw` 文件对应一种语言，其中 `<lang>` 为该语言的语言代码。您也可以通过 `--default-language` 选项指定一种语言为默认语言，该语言对应的翻译将会存储在 `Resources.resw` 文件中，作为应用程序的默认语言。

每个 `.csv` 文件中每一项都会生成一个对应的 PRI 字符串资源。PRI 资源 ID 的生成规则为 `相对于Views目录的路径_<csv文件名字>_<资源Id>/<资源属性Id>` 。

示例：在 `Views/Settings/AboutPage.csv` 中的一项

| Id | Property | en-US             | zh-Hans  | zh-Hant  | ru-RU  | uk-UA      |
| -- | -------- | ----------------- | -------- | -------- | ------ | ---------- |
| T1 | Text     | Other information | 其它信息 | 其它信息 | Другая | информация |

对应输出的 `.resw` 文件中的 `Settings_AboutPage_T1.Text`

```xml
  <data name="Settings_AboutPage_T1.Text" xml:space="preserve">
    <value>Other information</value>
  </data>
```

其中 `<value>` 为对应语言的翻译。

命令行程序的使用方法如下：

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

### `.resw` 资源访问代码生成器

为了方便在 WinUI App 项目中使用 C# 代码读取 PRI 字符串资源，我们在 中编写了一个 Roslyn 源生成器，用于自动化读取项目中的 `.resw` 资源文件，并在一个静态类中为每个 PRI 资源生成一个属性，用于在运行时通过 `Microsoft.Windows.ApplicationModel.Resources.ResourceManager` 获取这个资源。 `FluentLauncher.Infra.LocalizedStrings` 项目提供了 `GeneratedLocalizedStringsAttribute` ，用于标记需要生成属性的 `partial` 静态类。由于 `.resw` 文件不是 C# 代码文件，为了使源生成器能够读取这些文件，我们需要在 `.csproj` 项目文件中将 `.resw` 文件指定为 `AdditionalFiles` 。

这些工具可以在任意 C# WinUI 项目中使用，只需引用这两个项目即可：

```xml
<ItemGroup>
    <ProjectReference Include="..\FluentLauncher.Localization\FluentLauncher.Infra.LocalizedStrings.SourceGenerators\FluentLauncher.Infra.LocalizedStrings.SourceGenerators.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
    <ProjectReference Include="..\FluentLauncher.Localization\FluentLauncher.Infra.LocalizedStrings\FluentLauncher.Infra.LocalizedStrings.csproj" />
</ItemGroup>
```

假设项目中存在两个 `.resw` 文件：

- `Resources.resw` 包含两个资源：

  - `Button1.Content`
  - `Sample_Message`

- `TextBlocks.resw` 包含一个资源：

  - `DemoTextBlock.Text`


源生成器生成的代码如下：

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

注意：`Resources.resw` 为 WinUI 默认的资源组，所以源生成器直接在 `GeneratedLocalizedStrings` 类中生成访问器。对于其它的 `.resw` 文件，源生成器会使用资源组名称生成一个类，然后在其中生成访问器。

如果项目中存在多个名字相同但使用不同 Qualifer 的 `.resw` ，源生成器会默认选择不含 Qualifier 的版本。如果所有版本都含有 Qualifier ，那么其中文件名按字母排序最前的文件将会被用于代码生成。