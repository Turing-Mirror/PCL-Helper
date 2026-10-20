using System;
using System.Diagnostics;
using System.IO;

namespace PclHelper {
 /// <summary>Starts HMCL with JVM options scoped to the child process.</summary>
 public static class HmclLauncher {
  public const string DefaultTimeZone = "Asia/Taipei";
  public const string OptionsVariable = "HMCL_JAVA_OPTS";

  /// <summary>Preserves existing HMCL JVM options and overrides its default time zone.</summary>
  public static ProcessStartInfo CreateStartInfo(string executable, string timeZone = DefaultTimeZone) {
   if(string.IsNullOrWhiteSpace(executable))throw new ArgumentException("请选择 HMCL 程序。");
   var file=Path.GetFullPath(executable.Trim().Trim('"'));
   if(!File.Exists(file))throw new FileNotFoundException("找不到选中的 HMCL 程序。",file);
   if(!string.Equals(Path.GetExtension(file),".exe",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("请选择 HMCL 的 Windows exe 程序。");
   var start=new ProcessStartInfo(file) {
    UseShellExecute=false,
    CreateNoWindow=true,
    WorkingDirectory=Path.GetDirectoryName(file)
   };
   var existing=start.EnvironmentVariables[OptionsVariable];
   start.EnvironmentVariables[OptionsVariable]=((existing ?? "").Trim()+" -Duser.timezone="+timeZone).Trim();
   return start;
  }

  /// <summary>Starts the selected HMCL without changing machine or parent-process settings.</summary>
  public static Process Start(string executable) {
   return Process.Start(CreateStartInfo(executable));
  }
 }
}
