using System;
using System.IO;
using System.Reflection;
using System.Windows;
public class PageRunner:MarshalByRefObject {
 static byte[] Read(string path) {
  for(int attempt=0;;attempt++)try{return File.ReadAllBytes(path);}catch(IOException){if(attempt>=50)throw;System.Threading.Thread.Sleep(100);}
 }
 public int Run(string path) {
  try {
   var assembly=Assembly.Load(Read(Path.GetFullPath(path)));
   Application.ResourceAssembly=assembly;
   var app=(Application)Activator.CreateInstance(assembly.GetType("PCL.Application",true),true);
   assembly.GetType("PCL.Application").GetMethod("InitializeComponent").Invoke(app,null);
   var page=Activator.CreateInstance(assembly.GetType("PCL.PageDownloadInstall",true));
   Console.WriteLine("PASS: actual WPF PageDownloadInstall construction and LoaderInit.");
   Console.WriteLine("Page type: "+page.GetType().FullName);
   app.Shutdown();return 0;
  }catch(Exception ex){Console.WriteLine(ex);return 1;}
 }
}
class PageTest {
 [STAThread] static int Main(string[] args) {
  var domain=AppDomain.CreateDomain("PCL page smoke",null,new AppDomainSetup {ApplicationBase=AppDomain.CurrentDomain.BaseDirectory});
  var runner=(PageRunner)domain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName,typeof(PageRunner).FullName);
  var result=runner.Run(args[0]);AppDomain.Unload(domain);return result;
 }
}


