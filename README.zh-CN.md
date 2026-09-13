# PanelTextor

[日本語](README.md) | [English](README.en.md) | [简体中文](README.zh-CN.md) | [한국어](README.ko.md)

![PanelTextor](Assets/PanelTextor.png)

适用于 Windows 11 的本地多语言文字排版工具。

Copyright (c) 2026 **sirokata** · 原创代码采用 [MIT License](LICENSE)

## 下载与启动

1. 从本仓库的 **Releases** 下载 Windows x64 版本 ZIP。
2. 解压后运行 `PanelTextor.exe`，无需单独安装 .NET。
3. 首次启动时，请阅读使用条款及第三方许可证。同意后进入编辑界面；不同意则退出。
4. 点击“添加图片”，然后添加并放置文字。

详细操作请参阅 **[使用说明](USAGE.zh-CN.md)**。

exe 内包含所需的运行库及许可证，可独立运行；分发 ZIP 同时提供说明文档。启动时会将运行所需文件解压到 Windows 临时目录。

## 主要功能

- 分别管理 JP / EN / CN / KR 的文字、位置、字号和颜色
- 日语竖排、主要标点，以及短符号串和数字串的自动纵中横
- 可变字体字重选择，如 Regular / Medium / Bold
- 按空行拆分输入、拖动、多选及 Delete 删除
- 按文件名匹配多个背景图片集，并检查不匹配情况
- 多语言×背景集批量导出、进度显示、完成通知
- PNG / JPEG 质量100、sRGB转换及ICC配置文件
- 按页面设置拍摄取景框、已用时间、5级电池电量
- 日语、英语、简体中文、韩语界面

## 保存与迁移

- 项目文件：`作品.paneltextor.json`（可自定义名称）。图片以路径引用，请同时保管项目和图片文件夹。
- 支持打开旧 `.polytext.json`。保存时会建议新名称，不会自动删除旧文件。
- 设置：`%LOCALAPPDATA%\PanelTextor\settings.json`
- 新位置没有设置时，会自动复制旧 `%LOCALAPPDATA%\PolyText\settings.json`，并保留旧文件。
- 同意记录：`%LOCALAPPDATA%\PanelTextor\terms-acceptance.txt`。条款或许可证原文更改后需要重新同意。

## 环境与限制

适用于 Windows 11 x64。不附带字体，请自行安装所需字体。应用没有自行实现的图片上传或访问分析功能。

不支持与 其他图像编辑器 像素级一致、注音、日语禁则处理、自动换行及撤销/重做。JPEG质量100仍是有损压缩；无损保存请用PNG。

## 从源码构建

需要 Windows 和 .NET 10 SDK。

```powershell
dotnet restore PanelTextor.csproj
dotnet build PanelTextor.csproj -c Release
dotnet publish PanelTextor.csproj -c Release -r win-x64 --self-contained true -o dist/PanelTextor
```

生成分发ZIP：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/package.ps1
```

输出位于 `release/`。ZIP包含可执行文件、用户文档及所需许可证。

```powershell
dist/PanelTextor/PanelTextor.exe --self-test test-output
```

测试需要日语、中文和韩语字体。可变字体测试需要安装Source Han Sans CN VF。测试使用独立文件夹中的设置。

## 许可证与再分发

PanelTextor原创代码采用 **MIT**，允许免费使用、商业使用、修改及再分发。请保留版权及许可声明。

.NET/WPF等附带组件适用各自权利人的条件，不会改为MIT。请阅读[使用条款](TERMS.zh-CN.md)、[第三方声明](THIRD_PARTY_NOTICES.md)及 `licenses/`。分发版首次启动时要求同意，包括适用的第三方条件。修改或再分发时，请保留必要声明及同意机制。

报告问题时，请提供应用版本、Windows版本、字体及复现步骤。不要向公开Issue附加非公开作品或个人信息。


