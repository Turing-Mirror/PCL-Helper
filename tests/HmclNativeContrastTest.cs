using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using PclHelper;
class HmclNativeContrastTest {
 static void Run(string exe,string javaHome,string agent,string directory,bool helper,bool unavailableNetwork) {
  var label=(helper?"helper":"baseline")+(unavailableNetwork?"-network-unavailable":"-current-network");
  var report=Path.Combine(directory,label+".txt");if(File.Exists(report))File.Delete(report);
  var start=helper?HmclLauncher.CreateStartInfo(exe):new ProcessStartInfo(exe){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=directory};
  var zone=helper?"Asia/Taipei":"Asia/Shanghai";
  var expected=helper?"false":unavailableNetwork?"true":"any";
  start.EnvironmentVariables["HMCL_JAVA_HOME"]=javaHome;
  start.EnvironmentVariables[HmclLauncher.OptionsVariable]+=" \"-javaagent:"+agent+"="+zone+","+expected+","+report+"\"";
  using(var process=Process.Start(start)) {
   if(!SpinWait.SpinUntil(()=>File.Exists(report),30000))throw new Exception("Native HMCL probe timed out: "+label);
   Thread.Sleep(100);
   var result=File.ReadAllText(report);Console.WriteLine(label+": "+result);
   if(result.StartsWith("FAIL:"))throw new Exception(result);
  }
 }
 static int Main(string[] args) {
  var previous=Environment.GetEnvironmentVariable(HmclLauncher.OptionsVariable);
  try {
   var exe=Path.GetFullPath(args[0]);var agent=Path.GetFullPath(args[2]);var directory=Path.GetFullPath(args[3]);Directory.CreateDirectory(directory);
   var baseOptions="-Duser.timezone=Asia/Shanghai -Duser.country=CN -Duser.language=zh";
   Environment.SetEnvironmentVariable(HmclLauncher.OptionsVariable,baseOptions);
   Run(exe,args[1],agent,directory,false,false);
   Run(exe,args[1],agent,directory,true,false);
   Environment.SetEnvironmentVariable(HmclLauncher.OptionsVariable,baseOptions+" -Dhttps.proxyHost=127.0.0.1 -Dhttps.proxyPort="+args[4]);
   Run(exe,args[1],agent,directory,false,true);
   Run(exe,args[1],agent,directory,true,true);
   Console.WriteLine("PASS: official native HMCL exe, actual JVM options, existing Shanghai option overridden by Helper, controlled restriction true -> false. GUI/game download not exercised.");return 0;
  }catch(Exception error){Console.WriteLine(error);return 1;}
  finally{Environment.SetEnvironmentVariable(HmclLauncher.OptionsVariable,previous);}
 }
}
