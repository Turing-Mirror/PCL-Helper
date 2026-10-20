using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
public class PclModpackE2e : MarshalByRefObject {
 static Assembly pcl;
 static readonly BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
 static Type Type(string name){return pcl.GetType("PCL."+name,true);}
 static object Call(Type type,string name,object target,params object[] arguments){var method=type.GetMethods(Flags).Single(m=>m.Name==name&&m.GetParameters().Length==arguments.Length);return method.Invoke(target,arguments);}
 static void Wait(object loader){var timeout=DateTime.UtcNow.AddMinutes(15);var type=loader.GetType();while(true){var state=type.GetProperty("State").GetValue(loader,null).ToString();if(state=="Finished")return;if(state=="Failed"||state=="Canceled")throw new Exception("Actual PCL loader "+state,(Exception)type.GetProperty("Error").GetValue(loader,null));if(DateTime.UtcNow>timeout)throw new TimeoutException("PCL modpack timed out");Console.WriteLine("PCL loader: "+type.GetField("Name").GetValue(loader)+"; state="+state+"; progress="+type.GetProperty("Progress").GetValue(loader,null));Thread.Sleep(2000);}}
 static void IsolateSettings(string game){
  var entries=(IDictionary)Type("Settings").GetField("Entries").GetValue(null);
  foreach(DictionaryEntry pair in entries){var entry=pair.Value;var type=entry.GetType();var source=type.GetField("Source");if(source.GetValue(entry).ToString()=="Instance")continue;if(source.GetValue(entry).ToString()=="Registry")source.SetValue(entry,Enum.Parse(source.FieldType,"Normal"));type.GetField("Encrypted").SetValue(entry,false);var value=type.GetField("DefaultValue").GetValue(entry);if(pair.Key.ToString()=="SystemLastVersionReg"||pair.Key.ToString()=="SystemLastVersion")value=413;if(pair.Key.ToString()=="LaunchFolderSelect")value=game;if(pair.Key.ToString()=="SystemSystemTelemetry")value=false;Call(type,"Set",entry,value,null);}
 }
 public int Run(string[] args){
  System.Net.ServicePointManager.SecurityProtocol=System.Net.SecurityProtocolType.Tls12;AppContext.SetSwitch("Switch.System.IO.UseLegacyPathHandling",false);AppContext.SetSwitch("Switch.System.IO.BlockLongPaths",false);int exit=1;
  try {
   var root=AppDomain.CurrentDomain.BaseDirectory;Directory.SetCurrentDirectory(root);
   var game=Path.Combine(root,"minecraft")+"\\";Directory.CreateDirectory(game);
   pcl=Assembly.Load(File.ReadAllBytes(Path.GetFullPath(args[0])));Application.ResourceAssembly=pcl;
   var app=(Application)Activator.CreateInstance(Type("Application"),true);Call(Type("Application"),"InitializeComponent",app);app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
   var core=Assembly.Load("MeloongCore");Call(core.GetType("MeloongCore.Main"),"Init",null,"PCL-E2E");var wpf=Assembly.Load("MeloongCore.Wpf");Call(wpf.GetType("MeloongCore.Wpf.Main"),"Init",null);
   IsolateSettings(game);
   Type("ModBase").GetField("PathTemp").SetValue(null,Path.Combine(root,"temp")+"\\");Directory.CreateDirectory(Path.Combine(root,"temp"));
   Type("ModMinecraft").GetField("_McFolderSelected",Flags).SetValue(null,game);
   var main=Activator.CreateInstance(Type("FormMain"));Console.WriteLine("Actual PCL main controls initialized");
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
  return exit;
 }
}
class PclE2eMain {
 [STAThread] static int Main(string[] args){var domain=AppDomain.CreateDomain("PCL actual modpack e2e",null,new AppDomainSetup{ApplicationBase=AppDomain.CurrentDomain.BaseDirectory,TargetFrameworkName=".NETFramework,Version=v4.8"});var runner=(PclModpackE2e)domain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName,typeof(PclModpackE2e).FullName);var result=runner.Run(args);AppDomain.Unload(domain);return result;}
}
