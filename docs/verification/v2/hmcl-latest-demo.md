# HMCL 最新构建与演示准备

日期：2026-10-20。

HMCL 本地仓库已快进更新至 `acc9740fa9289ded38553b6a35045fd608c1ea99`。相对已验证的 `8827fd0`，新增提交修改模组更新页和检查更新提示，地区判定代码没有变化。

演示程序来自官方成功的 Java CI 工作流运行 `39430624375`，EXE 产物 `12267343773`，文件为 `HMCL-3.17.dev-acc9740.exe`。已放入本地 `release/v2/`，未加入 Helper 的 Release 附件。

SHA256：`1e40f055b70d659a3f845d79eca0c6eacfe3241907f6e2a0a781231c02f5f9e5`。

使用现有 `tests/hmcl-contrast.ps1` 对此官方 EXE 重跑测试：当前网络下，上海时区和 Helper 台北时区均返回 networkRestricted=false；仅在子进程地区查询网络不可用的受控条件下，上海返回 true，Helper 返回 false。确认参数传递和新版地区检测有效，未重跑新版完整游戏下载流程。

随后在真实网络下以 `-Duser.timezone=Asia/Shanghai` 打开程序，实际 java.exe 命令行已核对，窗口标题为 `Hello Minecraft! Launcher v3.17.dev-acc9740`，进程响应正常。系统时区保持原设置。当前网络下不能仅靠上海时区演示下载入口消失。

来源：https://github.com/HMCL-dev/HMCL/actions/runs/39430624375
