# v2：HMCL 与批量操作（本地待发布）

本次功能仅在本地准备，不推送、不更新公开 Release。HMCL 当前分析基线为 `8827fd06d8ea8bcd899e54f76004f63a447e2601`；已验证该提交的官方 CI JAR 中的实际地区判定；正式发行包的完整下载链路仍待官方发布后验证。

该 CI 版本的真实页面、Minecraft 1.12.2 / 1.20.1 下载与安装，以及 PCL 整合包安装和原版文件补全已完成集成验证。详情与限制见 [下载、安装与补全报告](verification/v2/download-install.md)。PCL 的原版补全不会恢复缺失模组。

## 查找与选择

程序启动后快速查找 Helper 所在目录、桌面、文档、下载目录和当前用户的 Programs 目录，最多查找两层子目录。点击“快速扫描”使用相同范围重新扫描，不遍历磁盘。未找到时会提示手动添加程序或扫描指定目录。

“扫描目录”递归查找指定目录。扫描会跳过 Windows、常见依赖缓存和目录链接，并可点击“停止查找”。找到的结果按完整路径去重，保留不同位置的多个 exe。也可以手动添加、输入路径后按回车，或一次拖入多个程序和目录。已移除全盘扫描入口。

列表按程序名和版本资源识别 PCL / HMCL，显示类型、完整路径和操作结果。重命名但保留官方版本资源的 PCL 也可识别。扫描过程不执行找到的程序。

## 恢复与还原

- “恢复所选”“还原所选”处理勾选项目。
- 操作栏最右侧的“全选 / 全不选”切换勾选状态；全选后点击“恢复所选”或“还原所选”即可批量处理。
- 每项独立执行并显示结果，一项失败不会中断其他项目。
- PCL 继续使用已验证的文件补丁、原文件备份与 SHA256 还原，支持范围仍为 2.99.0.0。
- HMCL 的恢复操作会启动该 exe，仅为它的子进程附加 `HMCL_JAVA_OPTS` 中的 `-Duser.timezone=Asia/Taipei`，保留已有 JVM 选项。
- HMCL 程序文件和系统设置保持原样；还原时不结束现有进程，直接打开原程序即可按默认方式启动。

PCL 和 HMCL 统一使用“启动所选”。启动 HMCL 时自动附带恢复参数。目前只处理 Windows exe。

## 本地构建与验证

```powershell
.\scripts\build.ps1 -OutputDirectory "$PWD\.test-data\hmcl-preview"
.\tests\discovery.ps1 -PclPath '原版 PCL.exe'
.\tests\smoke.ps1 -PclPath '原版 PCL.exe'
```

本地预览输出为 `.test-data/hmcl-preview/PCL-Helper-v2.exe`，独立于已发布的 `release/PCL-Helper-v1.exe`。

已验证实际子进程的 JVM 选项传递、现有参数保留、父进程环境不变、带空格的工作目录、原生按钮及布局、多副本与重命名识别、重叠扫描根去重、目录排除、扫描取消、三个 PCL 副本的修补与精确还原、HMCL 不修改文件，以及原有 6 个 CLR 方法与真实 PCL 下载页构造。

参考：

- 地区判定：https://github.com/HMCL-dev/HMCL/blob/8827fd06d8ea8bcd899e54f76004f63a447e2601/HMCL/src/main/java/org/jackhuang/hmcl/util/RegionUtils.java
- 启动选项：https://github.com/HMCL-dev/HMCL/blob/8827fd06d8ea8bcd899e54f76004f63a447e2601/docs/Contributing_zh_Hans.md

详细验证结果见 [v2-verification.md](v2-verification.md)。

图灵镜 LOGO、软件图标、底部官网和社媒入口、QQ 社群二维码见 [branding.md](branding.md)。
