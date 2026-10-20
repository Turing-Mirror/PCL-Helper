using System;
using System.Diagnostics;
using System.IO;
namespace PclHelper {
 public enum LauncherOperation { Enable, Restore, Inspect, Launch }
 public static class LauncherActions {
  public static string Run(LauncherEntry entry,LauncherOperation operation) {
   if(entry.Kind==LauncherKind.Hmcl) {
    if(operation==LauncherOperation.Restore)return "无需文件还原；直接打开原程序即可按默认方式启动。";
    if(operation==LauncherOperation.Inspect)return "使用本次启动参数，程序文件保持原样。";
    using(var process=HmclLauncher.Start(entry.FullPath)){}
    return "已附带参数启动 HMCL。";
   }
   switch(operation) {
    case LauncherOperation.Enable:return Patcher.Enable(entry.FullPath);
    case LauncherOperation.Restore:return Patcher.Restore(entry.FullPath);
    case LauncherOperation.Inspect:return Patcher.Inspect(entry.FullPath);
    case LauncherOperation.Launch:
     using(var process=Process.Start(new ProcessStartInfo(entry.FullPath){UseShellExecute=true,WorkingDirectory=Path.GetDirectoryName(entry.FullPath)})){}
     return "已启动 PCL。";
    default:throw new ArgumentOutOfRangeException("operation");
   }
  }
 }
}
