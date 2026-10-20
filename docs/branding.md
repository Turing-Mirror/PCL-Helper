# 软件标识与快捷入口

软件使用 `docs/logo/` 中的图灵镜原图，保留图案、颜色及透明背景。`favicon.ico` 包含 16、32、48、64、128、256 像素，作为 exe 图标和窗口图标；`favicon.png` 用于页头左侧。

底部入口集中配置于 `assets/links.json`，包括官网、哔哩哔哩、抖音、小红书、YouTube、GitHub 和 QQ 社群。社媒链接于 2026-10-20 从 [图灵镜官网](https://www.turingmirror.com/) 核对。

QQ 二维码保存为 `assets/qq-group.jpg`，来源为官网的 [社群二维码](https://www.turingmirror.com/assets/qq_group.jpg)。点击“QQ 社群”在本地窗口展示二维码，无需联网加载图片。LOGO、图标、入口配置和二维码均嵌入 exe。

操作栏只保留“恢复所选”“还原所选”“检查所选”“启动所选”，右侧使用一个“全选 / 全不选”按钮。部分勾选时显示“全选”，全部勾选时显示“全不选”。
