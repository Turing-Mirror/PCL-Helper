# PCL-Helper v1

## 使用

运行 `release/PCL-Helper-v1.exe`，选择 PCL 2.99.0.0 的程序文件。退出 PCL 后点击“恢复下载”，然后重新打开 PCL。文件也可以拖入窗口。

“还原原版”会按 SHA256 校验备份并恢复操作前的文件。备份文件为 `<PCL 文件名>.pcl-helper.bak`，记录为 `<PCL 文件名>.pcl-helper.json`。重复恢复下载与重复还原均可执行。旧测试补丁可通过经过校验的原始备份自动重新修补。

GUI 为 C# / .NET Framework WPF 原生窗口，沿用 TRM UI 的浅色、字体、圆角、间距和按钮样式。没有 Tauri、Electron 或 WebView。

## 恢复范围

上游的 `RestrictMcDownload` 是编译期常量，单改元数据无法恢复功能。工具使用 Mono.Cecil 修补实际入口，调用 PCL 原有实现：

- 自动安装页面重新使用 `DlClientListLoader`，恢复原版和加载器组合的选择入口。
- 本地整合包文件选择入口恢复，并在后台调用原有安装流程。
- 整合包格式分派恢复：CurseForge、Modrinth、HMCL、MultiMC、MCBBS、附带启动器及普通压缩包。
- 在线整合包版本列表恢复安装操作，并保留另存为。
- 修改版本页面恢复 Minecraft 组件卡片。

仅处理 2.99.0.0 且具备对应方法结构的程序。其他版本需要另行验证。PCL 自身的账号、依赖库、联网源和安装逻辑继续由 PCL 处理。

## 构建

Windows 需要 .NET Framework 4.x 的 C# 编译器和 WPF 程序集。运行：

```powershell
.\scripts\build.ps1
```

脚本首次获取固定版本的 Mono.Cecil 0.11.6，并将其嵌入可执行文件。输出为 `release/PCL-Helper-v1.exe`。构建工具、测试副本、运行数据和输出目录已加入 `.gitignore`。

## 验证

```powershell
.\tests\smoke.ps1 -PclPath '你的原版 PCL.exe'
```

2026-10-20 验证基线来自上游 `6b9a50d` 附带的“最新正式版.zip”。原版 exe SHA256：

`6b6e1260cb5a1d6a303855f1c60cab3d0bc1aebba7e40a964180188b271e7386`

通过：补丁写入、状态检测、重复写入、SHA256 完整还原、重复还原、重新应用、非目标文件拒绝，以及全部 6 个改动方法的真实 CLR JIT 验证。隔离应用域内实际创建 PCL 的 WPF `PageDownloadInstall` 页面并运行 `LoaderInit` 成功，覆盖先前截图中的异常路径。

已验证 PCL 修补后可启动；尚未完成整套 Minecraft 下载和所有整合包格式的端到端安装。桌面自动化运行时被本机沙箱初始化错误阻断，未以自动点击方式验证全部交互。

## 来源

- PCL 发布说明：https://github.com/Meloong-Git/PCL/releases/tag/2.99.0.0
- PCL 源码与许可：https://github.com/Meloong-Git/PCL
- TRM UI：https://github.com/Turing-Mirror/TRM-UI
- Mono.Cecil 0.11.6：https://www.nuget.org/packages/Mono.Cecil/0.11.6

本仓库仅包含第三方工具源码，PCL 的原始代码和二进制不作为交付物发布。Mono.Cecil 的 MIT 许可见 `Mono.Cecil.LICENSE.txt`，也嵌入 Helper 可执行文件。
