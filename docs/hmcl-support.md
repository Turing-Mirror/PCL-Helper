# v2：HMCL 与批量操作（本地待发布）

本次功能仅在本地准备，不推送、不更新公开 Release。HMCL 当前分析基线为 `8827fd06d8ea8bcd899e54f76004f63a447e2601`；已验证该提交的官方 CI JAR 中的实际地区判定；正式发行包的完整下载链路仍待官方发布后验证。

## 查找与选择

程序启动后自动查找常用目录，也可点击“自动查找”重新扫描。默认范围包括 Helper 所在目录、桌面、文档、下载目录和当前用户的 Programs 目录。

“扫描目录”递归查找指定目录；“扫描全部磁盘”检查已就绪的本地固定磁盘。扫描会跳过 Windows、常见依赖缓存和目录链接，并可点击“停止查找”。找到的结果按完整路径去重，保留不同位置的多个 exe。也可以手动添加、输入路径后按回车，或一次拖入多个程序和目录。

列表按程序名和版本资源识别 PCL / HMCL，显示类型、完整路径和操作结果。重命名但保留官方版本资源的 PCL 也可识别。扫描过程不执行找到的程序。

## 恢复与还原

- “恢复所选”“还原所选”处理勾选项目。
- “全部恢复”“全部还原”处理列表中的所有项目。
- 每项独立执行并显示结果，一项失败不会中断其他项目。
- PCL 继续使用已验证的文件补丁、原文件备份与 SHA256 还原，支持范围仍为 2.99.0.0。
- HMCL 的恢复操作会启动该 exe，仅为它的子进程附加 `HMCL_JAVA_OPTS` 中的 `-Duser.timezone=Asia/Taipei`，保留已有 JVM 选项。
- HMCL 程序文件和系统设置保持原样；还原时不结束现有进程，直接打开原程序即可按默认方式启动。

“启动 HMCL”也可直接选择一个 HMCL exe 并附带参数启动。目前只处理 Windows exe。

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
