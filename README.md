# FluentLauncher.LocalizationPoroject
#### Fluent Launcher 的 本地化计划

## 制定如下规定

首先 我们将一组对应的 `ViewModel.cs` 、`View.xaml` 、`View.xaml.cs` 称为一个 **视图单位**，  
此外一些仅有的 `ViewModel.cs` 的我们也将其单独成为一个 **视图单位**，  
如若还有一些零碎的翻译内容，我们会将其单独汇总到一些特定分类的 `.csv` 文件上

1. 所有的语言文件均以 `.csv` 格式储存
2. `.csv` 的文件名与 `View.xaml` 的文件名一致 如 `HomePage.xaml` 对应 `HomePage.csv`
3. 一个 `.csv` 文件应对应一个 **视图单位** （零碎汇总除外）
4. 所有的 `.csv` 必须以 UTF8-BOM 格式储存

## .csv 文件中的存储规范

`.csv` 文件中每一行的存储格式应该如下表  
且 `.csv` 文件中不能包含表头 仅包含内容  
**每一行的存储中，若该行为后台代码调用，应该在 `资源Id` 部分前面加上 `_` 前缀，且该行的 `资源属性Id` 留空**

| 资源Id | 资源属性Id | 英文原文 | 简体中文 | 繁体中文 | 俄文 |
| ---    | ---        |---        | ---      | ---      | ---  |

> _不要问我为什么英文是原文，因为开发时用直接写英文可以避免一些麻烦，节省很多时间_

## 开发者事宜

Q: .csv 文件如何使用到项目中  
A: 后续我会编写一个脚本来批处理生成到对应语言文件夹的 `Resources.resw` 文件  

生成例子:

在 Views/CoresPage.csv 中
| SearchBox | PlaceholderText | Search Core | 搜索核心 | 搜索核心 | поисковое ядро |
| ---    | ---        |---        | ---      | ---      | ---  |

`value` 为对应语言的翻译内容
`name` 的拼接规则为 相对于Views文件夹的路径转换_.csv文件名字_资源Id.资源属性Id

相对路径转换规则
`Views/CoresPage.csv` => `CoresPage`  
`Views/Activities/ActivitiesNavigationPage.csv` => `Activities_ActivitiesNavigationPage`

在 Strings/en-Us/Resources.resw 中
```
  <data name="CoresPage_SearchBox.PlaceholderText" xml:space="preserve">
    <value>Search Core</value>
  </data>
```

#### 后续若还有疑问等待补充
