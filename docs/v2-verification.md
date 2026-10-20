# v2 本地验证

验证日期：2026-10-20。此版本未推送、未发布。

## 版本

文件名为 `PCL-Helper-v2.exe`，原生窗口标题、界面页头和页脚显示 v2，程序集与文件版本为 `2.0.0.0`，产品版本为 `v2`。版本集中定义于 `src/AppVersion.cs`，构建脚本读取同一版本名称。

## 真实窗口事件流程

`tests/gui.ps1` 在独立进程中显示真实 WPF 窗口，让窗口 Loaded 事件自动扫描测试目录，通过原生按钮事件和复选框绑定验证：

- 自动列出两个真实 PCL 副本、一个 HMCL 启动探针和一个故意无效的 PCL 测试样本。
- 右侧单个“全选 / 全不选”切换按钮、部分勾选后的文字同步、复选框双向绑定和空选择反馈。
- 恢复所选、还原所选，以及原文件 SHA256 精确恢复。
- 全选后通过“恢复所选”“还原所选”完成批量操作，包括 HMCL 启动；未勾选项目不处理。
- 单项错误记录、其他项目继续执行，以及逐项和汇总结果。
- 停止全盘扫描和操作完成后控件恢复可用。
- 检查所选结果和完整 v2 界面标识。
- 页头 LOGO、窗口图标、7 个官网/社媒/社群入口，以及真实 QQ 二维码窗口。已去除两个冗余批量按钮。

截图：`.test-data/verification-v2/gui-v2.png`。窗口流程结果：`.test-data/verification-v2/gui-workflow.txt`。

测试触发实际 WPF 事件，未使用模拟鼠标点击。Windows Computer Use 运行时初始化失败，报错为 `windows sandbox failed: helper_unknown_error: setup refresh had errors`。

## HMCL 实际 CI 产物

使用 HMCL 官方 [Java CI 运行 39405614331](https://github.com/HMCL-dev/HMCL/actions/runs/39405614331) 的 JAR，版本为 `3.17.dev-8827fd0`，内嵌提交为 `8827fd06d8ea8bcd899e54f76004f63a447e2601`。

直接在该 JAR 上执行真实 `RegionUtils` 的两个公开判定方法，参数为 `-Duser.timezone=Asia/Taipei -Duser.country=CN -Duser.language=zh`。结果：

```text
machine=true
networkRestricted=false
```

这确认了在中国区域设置下，实际编译代码使用该时区参数会解除下载界面的地区限制。不是对重写的判定逻辑或模拟类进行测试。

可复现命令（使用满足目标 HMCL 运行要求的 JDK）：

```powershell
.\tests\hmcl-real.ps1 -JavaPath 'java.exe 的完整路径' -HmclJar '官方 CI 的 HMCL.jar 完整路径'
```

## 其他回归

自动查找已修正为常用目录优先、随后覆盖本地固定磁盘。回归验证包含常用目录为空而其他根目录存在启动器、默认范围包含所有本地固定磁盘、扫描根去重，以及真实 WPF 窗口零结果后的手动添加提示。之前仅注入含启动器的测试目录，未覆盖默认范围不足的问题。

当前窗口占用旧预览文件时，可使用 `tests/discovery.ps1 -PreviewDirectory "$PWD\.test-data\v2-search-fix"` 和 `tests/gui.ps1 -PreviewDirectory "$PWD\.test-data\v2-search-fix"` 验证并输出独立修正版，文件为 `.test-data/v2-search-fix/PCL-Helper-v2.exe`。

本次品牌与简化操作栏的预览输出为 `.test-data/v2-branded/PCL-Helper-v2.exe`，通过同样的 `-PreviewDirectory` 参数验证。图标包含全部 6 个尺寸，页头 PNG 的透明通道和二维码尺寸已核对。

`tests/discovery.ps1` 验证实际子进程的 JVM 选项、已有选项保留、父进程环境隔离、工作目录、多副本及重命名识别、重叠根去重、排除目录和取消，并对三个 PCL 副本逐一恢复、精确还原。

`tests/smoke.ps1` 验证 PCL 补丁写入、重复操作、精确还原、全部 6 个改动方法的 CLR JIT，以及真实 PCL 下载页面初始化。

以上全部通过。HMCL 原生 exe 启动到完整游戏安装流程、真实鼠标操作和所有游戏版本/整合包安装尚未完成端到端验证。已发布的本地 v1 文件保持原 SHA256，两个远程仓库及 Release 保持不变。
