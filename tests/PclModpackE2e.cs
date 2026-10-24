using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
public class PclModpackE2e : MarshalByRefObject {
 static Assembly pcl;
 static readonly BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
 static byte[] ReadAssembly(string path){for(int attempt=0;;attempt++)try{return File.ReadAllBytes(path);}catch(IOException){if(attempt>=50)throw;Thread.Sleep(100);}}
 static Type Type(string name){return pcl.GetType("PCL."+name,true);}
 static object Call(Type type,string name,object target,params object[] arguments){var method=type.GetMethods(Flags).Single(m=>m.Name==name&&m.GetParameters().Length==arguments.Length);return method.Invoke(target,arguments);}
 static object Property(object target,string name){return target.GetType().GetProperty(name,Flags).GetValue(target,null);}
 static void Assert(bool value,string message){if(!value)throw new Exception(message);}
 static void Setting(string name,object value){var entry=((IDictionary)Type("Settings").GetField("Entries").GetValue(null))[name];Call(entry.GetType(),"Set",entry,value,null);}
 static void VerifyNavigation(string output,bool restored){
  var left=Activator.CreateInstance(Type("PageDownloadLeft"));
  Assert(((UIElement)Property(left,"ItemInstall")).Visibility==(restored?Visibility.Visible:Visibility.Collapsed),"Automatic installation entry visibility mismatch");
  Assert(((UIElement)Property(left,"LabResource")).Visibility==(restored?Visibility.Visible:Visibility.Collapsed),"Resource heading visibility mismatch");
  var installPage=Enum.Parse(Type("PageDownloadLeft").GetField("PageID").FieldType,restored?"DownloadInstall":"DownloadMod");
  Assert(Type("PageDownloadLeft").GetField("PageID").GetValue(left).Equals(installPage),"Default download page mismatch");
  var folders=Activator.CreateInstance(Type("PageSelectLeft"));Type("ModMain").GetField("FrmSelectLeft").SetValue(null,folders);
  Call(Type("PageSelectLeft"),"McFolderListUI",folders);
  var panel=(Panel)Property(folders,"PanList");
  var pack=panel.Children.Cast<object>().Any(x=>x.GetType().GetProperty("Title")!=null&&Convert.ToString(Property(x,"Title"))=="导入整合包");
  Assert(pack==restored,"Local modpack entry mismatch");
  var right=Activator.CreateInstance(Type("PageSelectRight"));
  ((IDictionary)Type("ModMinecraft").GetField("McInstanceList").GetValue(null)).Clear();
  Call(Type("PageSelectRight"),"McInstanceListUI",right,new object[]{null});
  Assert(((UIElement)Property(right,"BtnEmptyDownload")).Visibility==(restored?Visibility.Visible:Visibility.Collapsed),"Empty versions download entry mismatch");
  Type("PageSelectRight").GetField("ShowHidden").SetValue(right,true);Call(Type("PageSelectRight"),"McInstanceListUI",right,new object[]{null});
  Assert(((UIElement)Property(right,"BtnEmptyDownload")).Visibility==Visibility.Collapsed,"Hidden versions view incorrectly gained a download entry");
  Type("PageSelectRight").GetField("ShowHidden").SetValue(right,false);
  if(restored){
   Setting("UiHiddenPageDownload",true);Call(Type("PageSelectRight"),"McInstanceListUI",right,new object[]{null});
   Assert(((UIElement)Property(right,"BtnEmptyDownload")).Visibility==Visibility.Collapsed,"User hidden download setting ignored");
   Type("PageSetupUI").GetProperty("HiddenForceShow",Flags).SetValue(null,true,null);Call(Type("PageSelectRight"),"McInstanceListUI",right,new object[]{null});
   Assert(((UIElement)Property(right,"BtnEmptyDownload")).Visibility==Visibility.Visible,"Force-show setting ignored");
   Type("PageSetupUI").GetProperty("HiddenForceShow",Flags).SetValue(null,false,null);Setting("UiHiddenPageDownload",false);
   Assert((int)Call(Type("ModSecret"),"PclHelperEmptyLaunchState",null)==2,"Empty homepage did not select Download game state");
   Setting("UiHiddenPageDownload",true);Assert((int)Call(Type("ModSecret"),"PclHelperEmptyLaunchState",null)==1,"Homepage ignored user hidden setting");Setting("UiHiddenPageDownload",false);
  }
  Call(Type("PageSelectRight"),"McInstanceListUI",right,new object[]{null});
  var grid=new Grid{Background=Brushes.White,Width=850,Height=500};grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(220)});grid.ColumnDefinitions.Add(new ColumnDefinition());
  var leftControl=(FrameworkElement)left;var rightControl=(FrameworkElement)right;grid.Children.Add(leftControl);grid.Children.Add(rightControl);Grid.SetColumn(rightControl,1);
  grid.Measure(new Size(850,500));grid.Arrange(new Rect(0,0,850,500));grid.UpdateLayout();
  var bitmap=new RenderTargetBitmap(850,500,96,96,PixelFormats.Pbgra32);bitmap.Render(grid);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(output+".png"))encoder.Save(file);
  Console.WriteLine("PASS actual PCL navigation controls: restored="+restored+", automatic installation, default page, local modpack, empty versions, hidden versions, user hidden and force-show settings.");
 }
 static void Wait(object loader){var timeout=DateTime.UtcNow.AddMinutes(15);var type=loader.GetType();while(true){var state=type.GetProperty("State").GetValue(loader,null).ToString();if(state=="Finished")return;if(state=="Failed"||state=="Canceled")throw new Exception("Actual PCL loader "+state,(Exception)type.GetProperty("Error").GetValue(loader,null));if(DateTime.UtcNow>timeout)throw new TimeoutException("PCL modpack timed out");Console.WriteLine("PCL loader: "+type.GetField("Name").GetValue(loader)+"; state="+state+"; progress="+type.GetProperty("Progress").GetValue(loader,null));Thread.Sleep(2000);}}
 static void IsolateSettings(string game){
  var entries=(IDictionary)Type("Settings").GetField("Entries").GetValue(null);
  foreach(DictionaryEntry pair in entries){var entry=pair.Value;var type=entry.GetType();var source=type.GetField("Source");if(source.GetValue(entry).ToString()=="Instance")continue;if(source.GetValue(entry).ToString()=="Registry")source.SetValue(entry,Enum.Parse(source.FieldType,"Normal"));type.GetField("Encrypted").SetValue(entry,false);var value=type.GetField("DefaultValue").GetValue(entry);if(pair.Key.ToString()=="SystemLastVersionReg"||pair.Key.ToString()=="SystemLastVersion")value=Type("ModBase").GetField("VersionCode").GetRawConstantValue();if(pair.Key.ToString()=="LaunchFolderSelect")value=game;if(pair.Key.ToString()=="SystemSystemTelemetry")value=false;Call(type,"Set",entry,value,null);}
 }
 public int Run(string[] args){
  System.Net.ServicePointManager.SecurityProtocol=System.Net.SecurityProtocolType.Tls12;AppContext.SetSwitch("Switch.System.IO.UseLegacyPathHandling",false);AppContext.SetSwitch("Switch.System.IO.BlockLongPaths",false);int exit=1;
  try {
   var root=AppDomain.CurrentDomain.BaseDirectory;Directory.SetCurrentDirectory(root);
   var game=Path.Combine(root,"minecraft")+"\\";Directory.CreateDirectory(game);
   pcl=Assembly.Load(ReadAssembly(Path.GetFullPath(args[0])));Application.ResourceAssembly=pcl;
   var app=(Application)Activator.CreateInstance(Type("Application"),true);Call(Type("Application"),"InitializeComponent",app);app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
   // The host initializes core/settings itself; avoid the normal singleton and user-config startup.
   app.Startup-=(StartupEventHandler)Delegate.CreateDelegate(typeof(StartupEventHandler),app,Type("Application").GetMethod("Application_Startup",Flags));
   app.DispatcherUnhandledException+=(sender,eventArgs)=>{Console.WriteLine("PCL UI ERROR: "+eventArgs.Exception);Console.Out.Flush();Environment.Exit(1);};
   AppDomain.CurrentDomain.UnhandledException+=(sender,eventArgs)=>{Console.WriteLine("PCL THREAD ERROR: "+eventArgs.ExceptionObject);Console.Out.Flush();};
   var core=Assembly.Load("MeloongCore");Call(core.GetType("MeloongCore.Main"),"Init",null,"PCL-E2E");var wpf=Assembly.Load("MeloongCore.Wpf");Call(wpf.GetType("MeloongCore.Wpf.Main"),"Init",null);
   var logger=Activator.CreateInstance(Type("PclLogger"));Directory.CreateDirectory(Path.Combine(root,"PCL"));logger.GetType().GetField("logFolder",Flags).SetValue(logger,Path.Combine(root,"PCL")+"\\");core.GetType("MeloongCore.Logger").GetProperty("Instance").SetValue(null,logger,null);
   IsolateSettings(game);
   Type("ModBase").GetField("PathTemp").SetValue(null,Path.Combine(root,"temp")+"\\");Directory.CreateDirectory(Path.Combine(root,"temp"));
   Type("ModMinecraft").GetField("_McFolderSelected",Flags).SetValue(null,game);
   var main=Activator.CreateInstance(Type("FormMain"));Console.WriteLine("Actual PCL main controls initialized");
   if(args.Length>2&&args[2]=="navigation"){VerifyNavigation(args[1],bool.Parse(args[3]));Console.Out.Flush();Environment.Exit(0);}
   var page=Activator.CreateInstance(Type("PageDownloadInstall"));Console.WriteLine("Actual patched PCL download page constructed");var window=new Window{Title="PCL modpack integration test",Content=new System.Windows.Controls.TextBlock{Text="正在执行 PCL 实际整合包下载与安装测试",Margin=new Thickness(20)},Width=600,Height=140};window.Show();
   Task.Run(()=>{
    try {
     var versions=Type("ModDownload").GetField("DlClientListLoader").GetValue(null);Call(versions.GetType(),"Start",versions,null,false);Wait(versions);Console.WriteLine("Version JSON URL: "+Call(Type("ModDownload"),"DlClientListGet",null,"1.20.1"));
     var instanceName=args.Length>3?args[3]:"helper-e2e-modrinth";
     if(args.Length>4){
      var download=Type("ModNet").GetMethods(Flags).Single(m=>m.Name=="NetDownloadByLoader"&&m.GetParameters().Length==5&&m.GetParameters()[0].ParameterType==typeof(string));
      var checkerType=download.GetParameters()[3].ParameterType;var checker=Activator.CreateInstance(checkerType);checkerType.GetField("Hash").SetValue(checker,args[5]);
      download.Invoke(null,new object[]{args[4],Path.GetFullPath(args[1]),null,checker,false});Console.WriteLine("PASS actual PCL LoaderDownload fetched online modpack with checksum verification");
     }
     if(args.Length<3||args[2]!="repair"){
      Console.WriteLine("Starting actual PCL ModpackInstall: "+args[1]);
      var loader=Call(Type("ModModpack"),"ModpackInstall",null,Path.GetFullPath(args[1]),instanceName,null);Wait(loader);
      Console.WriteLine("PASS actual PCL modpack installation completed: "+instanceName);
     }
     var library=Directory.GetFiles(Path.Combine(game,"libraries"),"*.jar",SearchOption.AllDirectories).First();
     var asset=Directory.GetFiles(Path.Combine(game,"assets","objects"),"*",SearchOption.AllDirectories).First();
     var libraryBytes=File.ReadAllBytes(library);var assetBytes=File.ReadAllBytes(asset);File.Delete(library);File.Delete(asset);
     var instanceType=pcl.GetTypes().Single(t=>t.Name=="McInstance");var folder=Path.Combine(game,"versions",instanceName)+"\\";
     var instance=Activator.CreateInstance(instanceType,new object[]{folder});Call(instanceType,"Load",instance);
     var tasks=Call(Type("ModDownload"),"DlClientFix",null,instance,true,false);
     var combo=pcl.GetTypes().Single(t=>t.Name=="LoaderCombo`1").MakeGenericType(typeof(string));
     var repair=Activator.CreateInstance(combo,new object[]{"PCL actual completion test",tasks});Call(combo,"Start",repair,folder,false);Wait(repair);
     if(!libraryBytes.SequenceEqual(File.ReadAllBytes(library))||!assetBytes.SequenceEqual(File.ReadAllBytes(asset)))throw new Exception("PCL completion did not exactly restore deleted files");
     Console.WriteLine("PASS actual PCL completion restored removed library and asset byte-for-byte");
     var mod=Directory.GetFiles(Path.Combine(folder,"mods"),"*.jar").First();var modBytes=File.ReadAllBytes(mod);
     try {
      File.Delete(mod);
      var modTasks=Call(Type("ModDownload"),"DlClientFix",null,instance,true,false);
      var modCheck=Activator.CreateInstance(combo,new object[]{"PCL missing mod capability test",modTasks});Call(combo,"Start",modCheck,folder,false);Wait(modCheck);
      Console.WriteLine(File.Exists(mod)?"PCL completion also restored missing mod":"LIMITATION: PCL base-game completion does not restore a removed modpack mod");
     }finally{File.WriteAllBytes(mod,modBytes);}
     exit=0;
    }catch(Exception error){Console.WriteLine(error);}
    finally{Console.Out.Flush();Environment.Exit(exit);}
   });
   Dispatcher.Run();
  }catch(Exception error){Console.WriteLine(error);}
  Console.Out.Flush();Environment.Exit(exit);return exit;
 }
}
class PclE2eMain {
 [STAThread] static int Main(string[] args){var domain=AppDomain.CreateDomain("PCL actual modpack e2e",null,new AppDomainSetup{ApplicationBase=AppDomain.CurrentDomain.BaseDirectory,TargetFrameworkName=".NETFramework,Version=v4.8"});var runner=(PclModpackE2e)domain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName,typeof(PclModpackE2e).FullName);var result=runner.Run(args);AppDomain.Unload(domain);return result;}
}
