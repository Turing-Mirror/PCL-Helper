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
   var empty=Path.Combine(root,"empty-common");Directory.CreateDirectory(empty);
   var automatic=LauncherDiscovery.AutomaticRoots(new[]{empty,root,root});
   Assert(automatic.Length==2&&automatic[0]==empty,"Common roots priority or root deduplication failed");
   Assert(LauncherDiscovery.AutomaticRoots().SequenceEqual(LauncherDiscovery.CommonRoots(),StringComparer.OrdinalIgnoreCase),"Quick search includes locations outside common directories");
   LauncherDiscovery.Scan(automatic,new DiscoveryOptions(),CancellationToken.None,found.Add,null);
   Assert(found.Count==4,"Discovery count mismatch: "+found.Count);
   Assert(found.Count(x=>x.Kind==LauncherKind.Pcl)==3&&found.Count(x=>x.Kind==LauncherKind.Hmcl)==1,"Launcher kinds mismatch");
   Assert(found.Any(x=>x.Name=="renamed-launcher.exe"),"Renamed PCL not detected by metadata");
   Assert(!found.Any(x=>x.Name.Contains("Helper")||x.Name.Contains("hidden")),"Helper or excluded directory was included");
   var deep=Path.Combine(root,"second","level-two","level-three","PCL-deep.exe");Copy(original,deep);
   var quick=new List<LauncherEntry>();LauncherDiscovery.Scan(new[]{root},LauncherDiscovery.QuickOptions(),CancellationToken.None,quick.Add,null);
   Assert(quick.Count==4&&!quick.Any(x=>x.FullPath==deep),"Quick search exceeded its depth limit");
   quick.Clear();LauncherDiscovery.Scan(new[]{root,Path.Combine(root,"second")},LauncherDiscovery.QuickOptions(),CancellationToken.None,quick.Add,null);
   Assert(quick.Count==5&&quick.Any(x=>x.FullPath==deep),"Overlapping quick roots lost results or produced duplicates");
   var manual=new List<LauncherEntry>();LauncherDiscovery.Scan(new[]{root},new DiscoveryOptions(),CancellationToken.None,manual.Add,null);
   Assert(manual.Count==5&&manual.Any(x=>x.FullPath==deep),"Manual directory scan did not include deeper launchers");
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
   Console.WriteLine("PASS: quick search uses only common directories and two subdirectory levels; manual scan includes deeper launchers; overlapping roots deduplicated; multiple/renamed executables; excluded directories; cancellation; three PCL restore cycles; HMCL launch and no-file restore.");
   return 0;
  }catch(Exception ex){Console.WriteLine(ex);return 1;}
 }
}
