using System.Text;

namespace LocalizerScript;

internal class Program
{
    private static readonly List<string> Languages = new()
    {
        "en-US",
        "zh-Hans",
        "zh-Hant",
        "ru-RU"
    };

    private static DirectoryInfo OutputsFolder;
    private static DirectoryInfo SourcesFolder;

    private static readonly List<string> Warns = new();
    private static readonly List<string> Errors = new();

    static void Main(string[] args)
    {
        SourcesFolder = new DirectoryInfo(args[0]);
        OutputsFolder = new DirectoryInfo(args[1]);

        var keyValuePairs = GetResources(SourcesFolder);

        var languages = new Dictionary<string, Dictionary<string,string>>();

        foreach (var item in Languages)
            languages.Add(item, new());

        foreach (var keyValuePair in keyValuePairs)
        {
            var relativePath = keyValuePair.Key.Trim('\\');

            foreach (var @string in keyValuePair.Value)
            {
                var Path_Name = relativePath.Replace(".csv", string.Empty).Replace('\\', '_') + "_" + @string.GetName();
                Console.WriteLine($"[INFO] 已找到资源: {Path_Name}");

                int empty = 0;

                foreach (var value in @string.Values)
                {
                    if (string.IsNullOrEmpty(value.Value))
                        empty++;
                    
                    languages[value.Key].Add(Path_Name, value.Value);
                }

                if (empty > 0)
                    Warns.Add($"[WARN]：at {relativePath}, 资源 {@string.GetName()} 有 {empty} 个空项");
            }
        }

        Console.ForegroundColor = ConsoleColor.Yellow;

        foreach (var item in Warns) 
            Console.WriteLine(item);

        Console.ForegroundColor = ConsoleColor.Red;

        foreach (var item in Errors)
            Console.WriteLine(item);

        if (Errors.Count > 0)
            throw new Exception("Invalid Csvs");

        Console.ForegroundColor = ConsoleColor.Green;

        foreach (var item in languages)
        {
            var outputFile = new FileInfo(Path.Combine(OutputsFolder.FullName, "Strings", item.Key, "Resources.resw"));

            if (!outputFile.Directory.Exists)
                outputFile.Directory.Create();

            File.WriteAllText(outputFile.FullName, BuildResw(item.Value));
            Console.WriteLine($"[INFO] 已生成资源文件：{outputFile.FullName}");
        }

        Console.ReadLine();
    }

    private static Dictionary<string, IEnumerable<StringResource>> GetResources(DirectoryInfo directory)
    {
        var dic = new Dictionary<string, IEnumerable<StringResource>>();

        foreach (var file in directory.EnumerateFiles())
        {
            if (file.Extension.Equals(".csv"))
            {
                var csvLines = Csv.CsvReader.ReadFromText(File.ReadAllText(file.FullName));

                var relativePath = file.FullName.Replace(SourcesFolder.FullName, string.Empty);
                var lines = csvLines.Select(x => ParseLine(relativePath, x)).Where(x => x != null).ToList();

                dic.Add(relativePath, lines);
            }
        }

        foreach (var directoryInfo in directory.EnumerateDirectories())
            foreach (var item in GetResources(directoryInfo))
                dic.Add(item.Key, item.Value);

        return dic;
    }

    private static StringResource ParseLine(string relativePath, Csv.ICsvLine line)
    {
        var values = line.Values.ToList();

        if (values.Count != Languages.Count + 2)
        {
            Errors.Add($"[ERROR]：at {relativePath}, Line {line.Index} : 项数目不正确");
            return null;
        }

        if (string.IsNullOrEmpty(values[0]))
        {
            Errors.Add($"[ERROR]：at {relativePath}, Line {line.Index} : 资源Id 不能为空");
            return null;
        }

        if (values[0].StartsWith('_') && !string.IsNullOrEmpty(values[1]))
        {
            Errors.Add($"[ERROR]：at {relativePath}, Line {line.Index} : 资源Id 标记为后台代码，但 资源属性Id 又不为空");
            return null;
        }

        var @string = new StringResource
        {
            Uid = values[0],
            Property = values[1]
        };

        values.RemoveAt(0);
        values.RemoveAt(0);

        var dic = new Dictionary<string, string>();

        for (int i = 0; i < values.Count; i++)
            dic.Add(Languages[i], values[i]);

        @string.Values = dic;

        return @string;
    }

    static string head = """
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
            """;

    static string dataTemplate = """
                <data name="${Name}" xml:space="preserve">
                    <value>${Value}</value>
                </data>
            """;

    static string end = """
            </root>
            """;

    private static string BuildResw(Dictionary<string, string> values)
    {
        var @string = new StringBuilder(head);

        foreach (var item in values)
            @string.AppendLine(dataTemplate.Replace("${Name}", item.Key).Replace("${Value}", item.Value));

        @string.AppendLine(end);

        return @string.ToString();
    }
}

public class StringResource
{
    public string Uid { get; set; }

    public string Property { get; set; }

    public Dictionary<string,string> Values { get; set; }

    public string GetName()
    {
        if (Uid.StartsWith('_'))
            return Uid;

        return $"{Uid}.{Property}";
    }
}