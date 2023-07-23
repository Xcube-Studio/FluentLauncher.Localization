# FluentLauncher.LocalizationPoroject
#### Fluent Launcher 的 本地化计划

## 制定如下规定

首先 我们将对应的一组 `ViewModel.cs` 、`View.xaml` 、`View.xaml.cs` 称为一个 **视图单位**  
对于部分仅有的 ViewModel.cs 的我们也将其单独成为一个 **视图单位**

1. 所有的语言文件均以 `.csv` 格式储存
2. `.csv` 的文件名与 `View.xaml` 的文件名一致 如 `HomePage.xaml` 对应 `HomePage.csv`
3. 一个 `.csv` 文件对应且仅对应一个 **视图单位**


## .csv 文件中的存储规范

`.csv` 文件中每一行的存储应该如下表所示  
且 `.csv` 文件中不能包含表头 仅包含内容  
每一行的存储中，若该行为后台代码调用，应该在 `资源Id` 部分前面加上 `_` 前缀，且该行的 `资源属性Id` 留空

| 资源Id | 资源属性Id | 英文原文 | 简体中文 | 繁体中文 | 俄文 |
| ---    | ---        |---        | ---      | ---      | ---  |

## 开发者事宜

Q: .csv 文件如何使用到项目中
A: 后续我会编写一个脚本来批处理生成到对应语言文件夹的 `Resources.resw` 文件  

生成例子:

在 Views/CoresPage.csv 中
| SearchBox | PlaceholderText | Search Core | 搜索核心 | 搜索核心 | поисковое ядро |
| ---    | ---        |---        | ---      | ---      | ---  |

生成对应语言文件中的 `data` 块，在不同的语言文件中生成的 value 值对应那一行的语言  
name 值的拼接规则为 相对于Views文件夹的路径转换_.csv文件名字_资源Id.资源属性Id

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
