# PCL-Helper

原生 WPF 下载功能恢复工具。支持 PCL 2.99.0.0 / 2.99.0.1，并为 HMCL 提供参数启动。单文件运行，无需安装。

当前版本为 v4，修复 PCL 在原路径更新后旧备份阻止再次修补的问题，保留历史备份。已交叉验证 v1、v2、v3 补丁的旧版 PCL 还原。见 [v4 验证报告](docs/verification/v4/compatibility.md)。

v2 使用图灵镜 LOGO 和软件图标，支持单按钮全选切换，并提供官网、社媒与 QQ 社群入口。见 [标识与快捷入口](docs/branding.md)。

下载 v4：[GitHub Release](https://github.com/Turing-Mirror/PCL-Helper/releases/tag/v4) · [CNB Release](https://cnb.cool/Turing-Mirror/PCL-Helper/-/releases/tag/v4)。旧版本保留独立标签与附件。

添加或勾选启动器，点击“恢复所选”。PCL 需要先退出；撤销时点击“还原所选”。更新 PCL 前先还原，更新完成后仍可使用同一个 Helper 恢复下载。原文件备份与校验记录保存在 PCL exe 旁，历史备份位于 `.pcl-helper.history` 目录，请一起保存。HMCL 通过启动参数恢复，不修改程序文件。

PCL 的原版文件补全仅覆盖支持库和资源，不会恢复缺失模组。HMCL 当前验证基线为官方 CI `3.17.dev-8827fd0`，未覆盖后续版本。v1 保留为独立历史版本。

本工具由 Turing Mirror Software 独立开发，与 PCL 官方无关联。PCL 本体、资源及修改后的程序不随工具分发。

构建、实现与验证说明见 [docs/README.md](docs/README.md)。
