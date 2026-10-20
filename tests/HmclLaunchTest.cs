using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using PclHelper;
class HmclLaunchTest {
 static void Assert(bool condition,string message){if(!condition)throw new Exception(message);}
 [STAThread] static int Main(string[] args) {
  var before=Environment.GetEnvironmentVariable(HmclLauncher.OptionsVariable);
  try {
   var exe=Path.GetFullPath(args[0]);
   const string options="-Xmx512m -Duser.timezone=Asia/Shanghai";
   Environment.SetEnvironmentVariable(HmclLauncher.OptionsVariable,options);
   using(var process=HmclLauncher.Start(exe)) {
    Assert(process!=null && process.WaitForExit(5000),"Child process did not exit");
    Assert(process.ExitCode==0,"Child process failed");
   }
   var received=File.ReadAllLines(Path.Combine(Path.GetDirectoryName(exe),"launch-probe.txt"));
   Assert(received[0]==options+" -Duser.timezone=Asia/Taipei","Child JVM options not preserved/overridden");
   Assert(string.Equals(received[1],Path.GetDirectoryName(exe),StringComparison.OrdinalIgnoreCase),"Child working directory mismatch");
   Assert(Environment.GetEnvironmentVariable(HmclLauncher.OptionsVariable)==options,"Parent environment was changed");
   Environment.SetEnvironmentVariable(HmclLauncher.OptionsVariable,null);
   Assert(HmclLauncher.CreateStartInfo(exe).EnvironmentVariables[HmclLauncher.OptionsVariable]=="-Duser.timezone=Asia/Taipei","Empty JVM options failed");
   var app=new Application();
   var gui=Assembly.Load(File.ReadAllBytes(Path.GetFullPath(args[1])));
   var window=(Window)Activator.CreateInstance(gui.GetType("PclHelper.MainWindow",true),new object[]{null});
   var content=(FrameworkElement)window.Content;
   var button=content.FindName("Launch") as Button;
   Assert(button!=null && (string)button.Content=="启动所选"&&content.FindName("LaunchHmcl")==null,"Unified launch button missing or redundant HMCL button remains");
   Assert(content.FindName("Enable") is Button && content.FindName("Restore") is Button,"PCL controls missing");
   content.Measure(new Size(600,450));content.Arrange(new Rect(0,0,600,450));
   Assert(button.ActualWidth>0 && button.ActualHeight>0,"HMCL button not laid out");
   window.Close();app.Shutdown();
   Console.WriteLine("PASS: child process launch, existing/empty JVM options, parent environment isolation, working directory with spaces, native GUI button and layout.");
   return 0;
  }catch(Exception ex){Console.WriteLine(ex);return 1;}
  finally{Environment.SetEnvironmentVariable(HmclLauncher.OptionsVariable,before);}
 }
}
