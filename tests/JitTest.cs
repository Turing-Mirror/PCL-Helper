using System;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
class JitTest {
 static byte[] Read(string path) {
  for(int attempt=0;;attempt++)try{return File.ReadAllBytes(path);}catch(IOException){if(attempt>=50)throw;System.Threading.Thread.Sleep(100);}
 }
 static Assembly target;
 static Dictionary<string,Assembly> deps=new Dictionary<string,Assembly>();
 [STAThread] static int Main(string[] args) {
  try {
   target=Assembly.Load(Read(Path.GetFullPath(args[0])));
   AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{
    var name=new AssemblyName(e.Name).Name;
    Assembly found;if(deps.TryGetValue(name,out found))return found;
    using(var stream=target.GetManifestResourceStream("PCL.Resources.resources"))using(var reader=new ResourceReader(stream)) {
     string type;byte[] data;
     try{reader.GetResourceData(name,out type,out data);}catch(ArgumentException){return null;}
     if(type!="ResourceTypeCode.ByteArray")return null;
     using(var raw=new MemoryStream(data))using(var binary=new BinaryReader(raw)){var len=binary.ReadInt32();found=Assembly.Load(binary.ReadBytes(len));deps.Add(name,found);return found;}
    }
   };
   var cases=new[]{new[]{"PCL.PageDownloadInstall","LoaderInit"},new[]{"PCL.ModModpack","ModpackInstall"},new[]{"PCL.PageDownloadResourceDetail","InitializeStackResourceVersions"},new[]{"PCL.PageDownloadResourceDetail","PclHelperPrimaryClick"},new[]{"PCL.PageDownloadModify","LoadInstanceFinished"}};
   int count=0;
   foreach(var item in cases)foreach(var method in target.GetType(item[0],true).GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static)) {
    if(method.Name!=item[1])continue;
    Console.WriteLine("Checking: "+method); RuntimeHelpers.PrepareMethod(method.MethodHandle);
    Console.WriteLine("JIT OK: "+method);count++;
   }
   if(count!=6)throw new Exception("Unexpected test method count: "+count);
   if(target.GetName().Version.ToString()=="2.99.0.1") {
    var entries=new[]{new[]{"PCL.PageSelectLeft","McFolderListUI"},new[]{"PCL.PageSelectRight","McInstanceListUI"},new[]{"PCL.PageLaunchLeft","RefreshButtonsUI"},new[]{"PCL.ModMinecraft","McDownloadClientUpdateHint"},new[]{"PCL.ModSecret","PclHelperDownloadHidden"},new[]{"PCL.ModSecret","PclHelperDownloadVisibility"},new[]{"PCL.ModSecret","PclHelperEmptyLaunchState"}};
    foreach(var entry in entries){var method=target.GetType(entry[0],true).GetMethod(entry[1],BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance);RuntimeHelpers.PrepareMethod(method.MethodHandle);Console.WriteLine("Navigation JIT OK: "+method);}
    RuntimeHelpers.PrepareMethod(target.GetType("PCL.PageDownloadLeft",true).GetConstructor(System.Type.EmptyTypes).MethodHandle);
   }
   return 0;
  }catch(Exception e){Console.WriteLine(e);return 1;}
 }
}



