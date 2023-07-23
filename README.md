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

#### 后续若还有疑问等待补充
