# v2 本地验证

验证日期：2026-10-20。本报告对应独立发布的 v2。

## 版本

文件名为 `PCL-Helper-v2.exe`，原生窗口标题、界面页头和页脚显示 v2，程序集与文件版本为 `2.0.0.0`，产品版本为 `v2`。版本集中定义于 `src/AppVersion.cs`，构建脚本读取同一版本名称。

## 真实窗口事件流程

`tests/gui.ps1` 在独立进程中显示真实 WPF 窗口，让窗口 Loaded 事件自动扫描测试目录，通过原生按钮事件和复选框绑定验证：

- 自动列出两个真实 PCL 副本、一个 HMCL 启动探针和一个故意无效的 PCL 测试样本。
- 右侧单个“全选 / 全不选”切换按钮、部分勾选后的文字同步、复选框双向绑定和空选择反馈。
- 恢复所选、还原所选，以及原文件 SHA256 精确恢复。
- 全选后通过“恢复所选”“还原所选”完成批量操作，包括 HMCL 启动；未勾选项目不处理。
- 单项错误记录、其他项目继续执行，以及逐项和汇总结果。
- 停止快速扫描和操作完成后控件恢复可用，界面没有全盘扫描入口。
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

### 官方 Windows exe 的反向对照

已使用同一份官方 `HMCL-3.17.dev-8827fd0.exe` 和 Helper 实际使用的 `HmclLauncher.CreateStartInfo` 完成四组启动。测试探针通过 Java agent 在 HMCL 主界面启动前调用它自身的 `RegionUtils`，随后退出；未修改 HMCL 判定代码。

| 条件 | 直接启动，上海时区 | Helper 启动，台北时区 |
| --- | --- | --- |
| 本机当前网络 | `networkRestricted=false` | `networkRestricted=false` |
| 地区查询失败 | `networkRestricted=true` | `networkRestricted=false` |

“地区查询失败”通过仅在测试子进程中设置不可连接的本地 HTTPS 代理制造，触发 HMCL 原有的查询失败时限制下载分支。它不是大陆网络的实测替代，也没有修改系统时区、代理设置或 HMCL 文件。

结果证明官方 exe 接收了 Helper 的实际参数，并且已有的上海时区选项被后追加的台北时区覆盖。当前网络下上海时区也未触发限制，因此只切换时区不能在本机直接形成受限/不受限对照。真实页面变化和完整下载流程仍未验证；Windows 界面工具初始化失败，错误为 `windows sandbox failed: helper_unknown_error: setup refresh had errors`。

证据位于 `.test-data/hmcl-contrast-fc98f78ff06e4a7dba98679237f7c309/contrast.txt`。复现命令：

```powershell
.\tests\hmcl-contrast.ps1 -JavaHome 'JDK 的完整目录' -HmclExe '官方 HMCL.exe 的完整路径'
```

根据用户反馈，自动查找已改为快速扫描：仅覆盖常用目录及其两层子目录，不扫描所有磁盘。回归验证包含默认范围、深度限制、重叠根去重、手动目录深层查找，以及真实 WPF 窗口零结果后的手动添加提示。

当前窗口占用旧预览文件时，可使用 `tests/discovery.ps1 -PreviewDirectory "$PWD\.test-data\v2-search-fix"` 和 `tests/gui.ps1 -PreviewDirectory "$PWD\.test-data\v2-search-fix"` 验证并输出独立修正版，文件为 `.test-data/v2-search-fix/PCL-Helper-v2.exe`。

本次品牌与简化操作栏的预览输出为 `.test-data/v2-branded/PCL-Helper-v2.exe`，通过同样的 `-PreviewDirectory` 参数验证。图标包含全部 6 个尺寸，页头 PNG 的透明通道和二维码尺寸已核对。

最新快速扫描版本为 `.test-data/v2-quick/PCL-Helper-v2.exe`，同样通过 `tests/discovery.ps1` 和 `tests/gui.ps1` 的 `-PreviewDirectory` 参数构建验证。

本机单次默认快速扫描实测 40 ms，扫描 5 个常用根目录，结果为 0；该结果反映当前目录内没有启动器，不代表其他位置没有。深层或非常用位置需手动添加或选择目录扫描。耗时随目录内容与磁盘情况变化。

`tests/discovery.ps1` 验证实际子进程的 JVM 选项、已有选项保留、父进程环境隔离、工作目录、多副本及重命名识别、重叠根去重、排除目录和取消，并对三个 PCL 副本逐一恢复、精确还原。

`tests/smoke.ps1` 验证 PCL 补丁写入、重复操作、精确还原、全部 6 个改动方法的 CLR JIT，以及真实 PCL 下载页面初始化。

上述项目已通过，后续实际页面与完整下载验证见 [下载、安装与补全报告](verification/v2/download-install.md)。该报告已覆盖 HMCL 两个游戏版本的实际安装，以及 PCL Modrinth / CurseForge 格式样本的下载、安装与原版文件补全。真实鼠标完整向导、进入游戏及其他版本/整合包仍未覆盖。PCL 的原版补全不会恢复缺失模组。已发布的本地 v1 文件保持原 SHA256，v2 单独发布，v1 标签与附件保持原样。
