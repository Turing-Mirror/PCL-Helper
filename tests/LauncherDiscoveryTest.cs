using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using PclHelper;
class LauncherDiscoveryTest {
 static void Assert(bool value,string message){if(!value)throw new Exception(message);}
 static void Copy(string source,string destination){Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(source,destination,true);}
 static int Main(string[] args) {
  try {
   var root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
   var original=Path.GetFullPath(args[1]);var hmclProbe=Path.GetFullPath(args[2]);var helper=Path.GetFullPath(args[3]);
   Copy(original,Path.Combine(root,"PCL.exe"));Copy(original,Path.Combine(root,"second","PCL-second.exe"));Copy(original,Path.Combine(root,"renamed-launcher.exe"));
   Copy(hmclProbe,Path.Combine(root,"HMCL-test.exe"));Copy(helper,Path.Combine(root,"PCL-Helper-local.exe"));Copy(original,Path.Combine(root,"node_modules","PCL-hidden.exe"));
   var found=new List<LauncherEntry>();
   LauncherDiscovery.Scan(new[]{root,Path.Combine(root,"second")},new DiscoveryOptions(),CancellationToken.None,found.Add,null);
   Assert(found.Count==4,"Discovery count mismatch: "+found.Count);
   Assert(found.Count(x=>x.Kind==LauncherKind.Pcl)==3&&found.Count(x=>x.Kind==LauncherKind.Hmcl)==1,"Launcher kinds mismatch");
   Assert(found.Any(x=>x.Name=="renamed-launcher.exe"),"Renamed PCL not detected by metadata");
   Assert(!found.Any(x=>x.Name.Contains("Helper")||x.Name.Contains("hidden")),"Helper or excluded directory was included");
   var hash=Patcher.Hash(original);
   foreach(var entry in found.Where(x=>x.Kind==LauncherKind.Pcl)) {
    LauncherActions.Run(entry,LauncherOperation.Enable);
    Assert(Patcher.Hash(entry.FullPath)!=hash,"PCL not patched");
    LauncherActions.Run(entry,LauncherOperation.Restore);
    Assert(Patcher.Hash(entry.FullPath)==hash,"PCL not restored exactly");
   }
   var hmcl=found.Single(x=>x.Kind==LauncherKind.Hmcl);var before=Patcher.Hash(hmcl.FullPath);
   var report=Path.Combine(root,"launch-probe.txt");if(File.Exists(report))File.Delete(report);
   var result=LauncherActions.Run(hmcl,LauncherOperation.Restore);
   Assert(result.StartsWith("无需文件还原")&&!File.Exists(report)&&Patcher.Hash(hmcl.FullPath)==before,"HMCL restore altered or launched the file");
   LauncherActions.Run(hmcl,LauncherOperation.Enable);
   Assert(SpinWait.SpinUntil(()=>File.Exists(report),5000),"HMCL enable did not launch");
   Assert(Patcher.Hash(hmcl.FullPath)==before,"HMCL file was modified");
   var cancellation=new CancellationTokenSource();cancellation.Cancel();bool cancelled=false;
   try{LauncherDiscovery.Scan(new[]{root},new DiscoveryOptions(),cancellation.Token,x=>{},null);}catch(OperationCanceledException){cancelled=true;}
   Assert(cancelled,"Scan cancellation ignored");
   Console.WriteLine("PASS: multiple/renamed executables, duplicate roots, helper/excluded directory filtering, cancellation, three PCL restore cycles, HMCL launch and no-file restore.");
   return 0;
  }catch(Exception ex){Console.WriteLine(ex);return 1;}
 }
}
