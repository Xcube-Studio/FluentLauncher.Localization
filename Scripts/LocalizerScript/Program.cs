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

    private static string OutputsFolder;
    private static string SourcesFolder;

    private static readonly List<string> Warns = new();

    static void Main(string[] args)
    {
        SourcesFolder = args[0];
        OutputsFolder = args[1];

        var keyValuePairs = GetResources(new DirectoryInfo(SourcesFolder));

        var languages = new Dictionary<string, Dictionary<string,string>>();

        foreach (var item in Languages)
            languages.Add(item, new());

        foreach (var keyValuePair in keyValuePairs)
        {
            var relativePath = keyValuePair.Key.Trim('\\');

            foreach (var @string in keyValuePair.Value)
            {
                var Path_Name = relativePath.Replace(".csv", string.Empty).Replace('\\', '_') + "_" + @string.GetName();
                Console.WriteLine("[检索] 已找到资源：" + Path_Name);

                int empty = 0;

                foreach (var value in @string.Values)
                {
                    if (string.IsNullOrEmpty(value.Value))
                        empty++;
                    
                    languages[value.Key].Add(Path_Name, value.Value);
                }

                if (empty > 0)
                    Warns.Add("[警告]：在文件 " + relativePath + " 中，" + @string.GetName() + " 有" + empty.ToString() + "个空项");
            }
        }

        Console.ForegroundColor = ConsoleColor.Yellow;

        foreach (var item in Warns) 
            Console.WriteLine(item);

        Console.ForegroundColor = ConsoleColor.Green;

        foreach (var item in languages)
        {
            var outputFile = new FileInfo(Path.Combine(OutputsFolder, "Strings", item.Key, "Resources.resw"));

            if (!outputFile.Directory.Exists)
                outputFile.Directory.Create();

            File.WriteAllText(outputFile.FullName, BuildResw(item.Value));
            Console.WriteLine("[输出] 已生成资源文件：" + outputFile.FullName);
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
                var csvLines = Csv.CsvReader.ReadFromText(File.ReadAllText(file.FullName), options: new Csv.CsvOptions
                {
                    HeaderMode = Csv.HeaderMode.HeaderAbsent
                });

                var lines = csvLines.Select(x => ParseLine(x));
                var relativePath = file.FullName.Replace(SourcesFolder, string.Empty);

                dic.Add(relativePath, lines);
            }
        }

        foreach (var directoryInfo in directory.EnumerateDirectories())
            foreach (var item in GetResources(directoryInfo))
                dic.Add(item.Key, item.Value);

        return dic;
    }

    private static StringResource ParseLine(Csv.ICsvLine line)
    {
        var values = line.Values.ToList();

        while (values.Count - 2 < Languages.Count)
            values.Add(string.Empty);

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