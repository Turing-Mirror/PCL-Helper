# PCL-Helper

原生 WPF 下载功能恢复工具。支持 PCL 2.99.0.0，并为 HMCL 提供参数启动。单文件运行，无需安装。

当前版本为 v2，包含快速扫描、多程序选择和批量操作。见 [实际下载、安装与补全验证](docs/verification/v2/download-install.md)。

v2 使用图灵镜 LOGO 和软件图标，支持单按钮全选切换，并提供官网、社媒与 QQ 社群入口。见 [标识与快捷入口](docs/branding.md)。

下载 v2：[GitHub Release](https://github.com/Turing-Mirror/PCL-Helper/releases/tag/v2) · [CNB Release](https://cnb.cool/Turing-Mirror/PCL-Helper/-/releases/tag/v2)

添加或勾选启动器，点击“恢复所选”。PCL 需要先退出；撤销时点击“还原所选”。原文件备份与校验记录保存在 PCL exe 旁，请一起保存。HMCL 通过启动参数恢复，不修改程序文件。

PCL 的原版文件补全仅覆盖支持库和资源，不会恢复缺失模组。HMCL 当前验证基线为官方 CI `3.17.dev-8827fd0`，未覆盖后续版本。v1 保留为独立历史版本。

本工具由 Turing Mirror Software 独立开发，与 PCL 官方无关联。PCL 本体、资源及修改后的程序不随工具分发。

构建、实现与验证说明见 [docs/README.md](docs/README.md)。
