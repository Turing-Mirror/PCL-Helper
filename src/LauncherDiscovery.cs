using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace PclHelper {
 public enum LauncherKind { Pcl, Hmcl }
 public sealed class LauncherEntry : INotifyPropertyChanged {
  bool selected=true;
  string status="待处理";
  public string FullPath { get; private set; }
  public LauncherKind Kind { get; private set; }
  public string Name { get { return Path.GetFileName(FullPath); } }
  public string KindLabel { get { return Kind==LauncherKind.Pcl ? "PCL" : "HMCL"; } }
  public bool Selected { get { return selected; } set { if(selected!=value){selected=value;Changed("Selected");} } }
  public string Status { get { return status; } set { if(status!=value){status=value;Changed("Status");} } }
  public LauncherEntry(string fullPath,LauncherKind kind){FullPath=Path.GetFullPath(fullPath);Kind=kind;}
  public event PropertyChangedEventHandler PropertyChanged;
  void Changed(string name){var handler=PropertyChanged;if(handler!=null)handler(this,new PropertyChangedEventArgs(name));}
 }
 public sealed class DiscoveryOptions {
  public int MaximumDepth { get; set; }
  public ISet<string> ExcludedDirectories { get; set; }
  public DiscoveryOptions(){MaximumDepth=int.MaxValue;ExcludedDirectories=new HashSet<string>(new[]{"Windows","$Recycle.Bin","System Volume Information",".git",".svn",".hg","node_modules",".gradle",".m2",".nuget",".cache",".test-data"},StringComparer.OrdinalIgnoreCase);}
 }
 public static class LauncherDiscovery {
  public static string[] CommonRoots() {
   var home=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
   return new[]{AppDomain.CurrentDomain.BaseDirectory,Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),Path.Combine(home,"Downloads"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs")}.Where(p=>!string.IsNullOrWhiteSpace(p)&&Directory.Exists(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
  }
  public static string[] AutomaticRoots(){return AutomaticRoots(CommonRoots());}
  public static string[] AutomaticRoots(IEnumerable<string> roots){return roots.Where(p=>!string.IsNullOrWhiteSpace(p)&&Directory.Exists(p)).Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();}
  public static DiscoveryOptions QuickOptions(){return new DiscoveryOptions{MaximumDepth=2};}
  public static LauncherEntry Identify(string path) {
   var file=Path.GetFullPath(path);
   if(!File.Exists(file)||!string.Equals(Path.GetExtension(file),".exe",StringComparison.OrdinalIgnoreCase))return null;
   var name=Path.GetFileNameWithoutExtension(file);
   if(name.StartsWith("PCL-Helper",StringComparison.OrdinalIgnoreCase))return null;
   string metadata="";
   try{var info=FileVersionInfo.GetVersionInfo(file);metadata=(info.ProductName??"")+" "+(info.FileDescription??"")+" "+(info.OriginalFilename??"");}catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}
   if(metadata.IndexOf("PCL-Helper",StringComparison.OrdinalIgnoreCase)>=0)return null;
   if(metadata.IndexOf("HMCL",StringComparison.OrdinalIgnoreCase)>=0||metadata.IndexOf("Hello Minecraft",StringComparison.OrdinalIgnoreCase)>=0||name.StartsWith("HMCL",StringComparison.OrdinalIgnoreCase))return new LauncherEntry(file,LauncherKind.Hmcl);
   if(metadata.IndexOf("Plain Craft Launcher",StringComparison.OrdinalIgnoreCase)>=0||name.Equals("PCL",StringComparison.OrdinalIgnoreCase)||name.StartsWith("PCL2",StringComparison.OrdinalIgnoreCase)||name.StartsWith("PCL-",StringComparison.OrdinalIgnoreCase)||name.StartsWith("Plain Craft Launcher",StringComparison.OrdinalIgnoreCase))return new LauncherEntry(file,LauncherKind.Pcl);
   return null;
  }
  public static void Scan(IEnumerable<string> roots,DiscoveryOptions options,CancellationToken cancellation,Action<LauncherEntry> found,Action<string> progress) {
   var pending=new Stack<KeyValuePair<string,int>>(roots.Where(p=>!string.IsNullOrWhiteSpace(p)&&Directory.Exists(p)).Reverse().Select(p=>new KeyValuePair<string,int>(p,0)));
   var seen=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
   var foundPaths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   var clock=Stopwatch.StartNew();long last=-250;
   while(pending.Count>0) {
    cancellation.ThrowIfCancellationRequested();
    var next=pending.Pop();var directory=Path.GetFullPath(next.Key);var key=directory.TrimEnd(Path.DirectorySeparatorChar);int previous;
    if(seen.TryGetValue(key,out previous)&&previous<=next.Value)continue;
    seen[key]=next.Value;
    if(clock.ElapsedMilliseconds-last>=200){last=clock.ElapsedMilliseconds;if(progress!=null)progress(directory);}
    try {
     foreach(var file in Directory.EnumerateFiles(directory,"*.exe",SearchOption.TopDirectoryOnly)) {
      cancellation.ThrowIfCancellationRequested();
      try{var entry=Identify(file);if(entry!=null&&foundPaths.Add(entry.FullPath))found(entry);}catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}
     }
    }catch(IOException){}catch(UnauthorizedAccessException){}
    if(next.Value>=options.MaximumDepth)continue;
    try {
     foreach(var child in Directory.EnumerateDirectories(directory)) {
      cancellation.ThrowIfCancellationRequested();
      if(options.ExcludedDirectories.Contains(Path.GetFileName(child)))continue;
      try{if((File.GetAttributes(child)&FileAttributes.ReparsePoint)==0)pending.Push(new KeyValuePair<string,int>(child,next.Value+1));}catch(IOException){}catch(UnauthorizedAccessException){}
     }
    }catch(IOException){}catch(UnauthorizedAccessException){}
   }
  }
 }
}
