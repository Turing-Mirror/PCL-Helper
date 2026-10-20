using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Security.Cryptography;
using System.Threading;
using System.Web.Script.Serialization;
using Microsoft.Win32;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace PclHelper {
 public sealed class PatchInfo {
  public string HelperVersion = "v1";
  public int PatchRevision = 2;
  public string OriginalSha256;
  public string PatchedSha256;
  public string FileName;
  public string CreatedUtc;
 }
 sealed class EmbeddedResolver : DefaultAssemblyResolver {
 public ModuleDefinition Owner;
 readonly Dictionary<string,AssemblyDefinition> cache=new Dictionary<string,AssemblyDefinition>();
 public override AssemblyDefinition Resolve(AssemblyNameReference name) { return Resolve(name,new ReaderParameters()); }
 public override AssemblyDefinition Resolve(AssemblyNameReference name,ReaderParameters parameters) {
  AssemblyDefinition found;
  if(cache.TryGetValue(name.FullName,out found))return found;
  if(Owner!=null)foreach(var resource in Owner.Resources.OfType<EmbeddedResource>().Where(r=>r.Name.EndsWith(".resources",StringComparison.Ordinal))) {
   using(var reader=new ResourceReader(resource.GetResourceStream())) {
    string type;byte[] data;
    try{reader.GetResourceData(name.Name,out type,out data);}catch(ArgumentException){continue;}
    if(type!="ResourceTypeCode.ByteArray" || data.Length<4)continue;
    using(var raw=new MemoryStream(data))using(var binary=new BinaryReader(raw)) {
     var length=binary.ReadInt32();
     var bytes=binary.ReadBytes(length);
     found=AssemblyDefinition.ReadAssembly(new MemoryStream(bytes),new ReaderParameters { AssemblyResolver=this, InMemory=true });
     cache.Add(name.FullName,found);return found;
    }
   }
  }
  return base.Resolve(name,parameters);
 }
}
public static class Patcher {
  public const string Marker = "PCLHelperVersion";
  public const string Restriction = "为遵守相关法律法规，PCL 暂时不再提供 MC 下载功能，非常抱歉 qwq……";
  static T Retry<T>(Func<T> action) {
   for(int attempt=0;;attempt++)try{return action();}
   catch(IOException e){var code=e.HResult & 0xffff;if(attempt>=20 || (code!=32 && code!=33))throw;Thread.Sleep(100);}
  }
  public static string Hash(string path) { return Retry(()=>{using(var h=SHA256.Create()) using(var s=File.OpenRead(path)) return BitConverter.ToString(h.ComputeHash(s)).Replace("-","").ToLowerInvariant();}); }
  static AssemblyDefinition Read(string path) {
   var resolver=new EmbeddedResolver();
   resolver.AddSearchDirectory(Path.GetDirectoryName(path));
   resolver.AddSearchDirectory(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory());
   resolver.AddSearchDirectory(Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(),"WPF"));
   var assembly=AssemblyDefinition.ReadAssembly(new MemoryStream(Retry(()=>File.ReadAllBytes(path))),new ReaderParameters { InMemory=true, ReadSymbols=false, AssemblyResolver=resolver });
   resolver.Owner=assembly.MainModule;return assembly;
  }
  static TypeDefinition Type(ModuleDefinition m,string name) { var t=m.Types.SingleOrDefault(x=>x.Name==name); if(t==null) throw new InvalidDataException("未找到 PCL 模块："+name); return t; }
  static MethodDefinition Method(TypeDefinition t,string name,int count) { var f=t.Methods.SingleOrDefault(x=>x.Name==name && x.Parameters.Count==count); if(f==null || !f.HasBody) throw new InvalidDataException("未找到 PCL 方法："+name); return f; }
  static void Require(bool ok,string message) { if(!ok) throw new InvalidDataException("不兼容的 PCL 文件："+message); }
  static string Operand(Instruction i) { return i.Operand==null ? "" : i.Operand.ToString(); }
  static void Nop(Instruction i) { i.OpCode=OpCodes.Nop; i.Operand=null; }
  static void LongBranches(MethodDefinition m) {
   foreach(var i in m.Body.Instructions) {
    if(i.OpCode==OpCodes.Br_S)i.OpCode=OpCodes.Br;
    else if(i.OpCode==OpCodes.Brtrue_S)i.OpCode=OpCodes.Brtrue;
    else if(i.OpCode==OpCodes.Brfalse_S)i.OpCode=OpCodes.Brfalse;
    else if(i.OpCode==OpCodes.Beq_S)i.OpCode=OpCodes.Beq;
    else if(i.OpCode==OpCodes.Bne_Un_S)i.OpCode=OpCodes.Bne_Un;
    else if(i.OpCode==OpCodes.Bge_S)i.OpCode=OpCodes.Bge;
    else if(i.OpCode==OpCodes.Bge_Un_S)i.OpCode=OpCodes.Bge_Un;
    else if(i.OpCode==OpCodes.Bgt_S)i.OpCode=OpCodes.Bgt;
    else if(i.OpCode==OpCodes.Bgt_Un_S)i.OpCode=OpCodes.Bgt_Un;
    else if(i.OpCode==OpCodes.Ble_S)i.OpCode=OpCodes.Ble;
    else if(i.OpCode==OpCodes.Ble_Un_S)i.OpCode=OpCodes.Ble_Un;
    else if(i.OpCode==OpCodes.Blt_S)i.OpCode=OpCodes.Blt;
    else if(i.OpCode==OpCodes.Blt_Un_S)i.OpCode=OpCodes.Blt_Un;
    else if(i.OpCode==OpCodes.Leave_S)i.OpCode=OpCodes.Leave;
   }
  }
  public static string Inspect(string path) {
   using(var a=Read(path)) {
    var t=Type(a.MainModule,"ModSecret");
    var flag=t.Fields.SingleOrDefault(f=>f.Name=="RestrictMcDownload");
    Require(flag!=null && flag.IsLiteral && flag.Constant is bool,"未找到下载限制常量");
    return "PCL "+a.Name.Version+"\r\n"+(t.Fields.Any(f=>f.Name==Marker) ? "已恢复下载功能" : ((bool)flag.Constant ? "下载功能受限，可以检查并恢复" : "此文件未开启下载限制"))+"\r\nSHA256: "+Hash(path);
   }
  }
  static void EnsureNotRunning(string path) {
   foreach(var process in System.Diagnostics.Process.GetProcessesByName(Path.GetFileNameWithoutExtension(path)))using(process) {
    string running=null;
    try{running=process.MainModule.FileName;}catch(System.ComponentModel.Win32Exception){}
    if(string.Equals(running,path,StringComparison.OrdinalIgnoreCase))throw new IOException("请先退出选中的 PCL，再执行操作。");
   }
  }
  static void Replace(string source,string destination) {
   for(int attempt=0;;attempt++)try{File.Replace(source,destination,null);return;}
   catch(IOException e){var code=e.HResult & 0xffff;if(attempt>=20 || (code!=32 && code!=33))throw;Thread.Sleep(100);}
  }
  public static string Enable(string path) {
   path=Path.GetFullPath(path);
   EnsureNotRunning(path);
   var originalHash=Hash(path);
   var backup=path+".pcl-helper.bak";
   var record=path+".pcl-helper.json";
   var temp=path+".pcl-helper-"+Guid.NewGuid().ToString("N")+".tmp";
   try {
    using(var a=Read(path)) {
     var m=a.MainModule;
     Require(a.Name.Version.ToString()=="2.99.0.0","当前仅支持 2.99.0.0");
     var secret=Type(m,"ModSecret");
     if(secret.Fields.Any(f=>f.Name==Marker)) {
      if(secret.Fields.Any(f=>f.Name=="PCLHelperPatchRevision" && Equals(f.Constant,2)))return "此 PCL 已恢复，无需重复操作。";
      // Upgrade only through the hash-verified original backup.
      a.Dispose();Restore(path);return Enable(path);
     }
     var flag=secret.Fields.SingleOrDefault(f=>f.Name=="RestrictMcDownload");
     Require(flag!=null && flag.IsLiteral && Equals(flag.Constant,true),"下载限制状态不匹配");
     RestoreLoader(m);
     RestorePackDispatch(m);
     RestorePackDialog(m);
     RestoreResourceInstall(m);
     RestoreModifyCard(m);
     flag.Constant=false;
     secret.Fields.Add(new FieldDefinition(Marker,Mono.Cecil.FieldAttributes.Assembly|Mono.Cecil.FieldAttributes.Static|Mono.Cecil.FieldAttributes.Literal|Mono.Cecil.FieldAttributes.HasDefault,m.TypeSystem.String) { Constant="v1" });
     secret.Fields.Add(new FieldDefinition("PCLHelperPatchRevision",Mono.Cecil.FieldAttributes.Assembly|Mono.Cecil.FieldAttributes.Static|Mono.Cecil.FieldAttributes.Literal|Mono.Cecil.FieldAttributes.HasDefault,m.TypeSystem.Int32) { Constant=2 });
     a.Write(temp);
    }
    using(var verified=Read(temp)) {
     Require(Type(verified.MainModule,"ModSecret").Fields.Any(f=>f.Name==Marker),"写入校验失败");
     var dispatch=Method(Type(verified.MainModule,"ModModpack"),"ModpackInstall",3);
     Require(dispatch.Body.Instructions.Any(i=>Operand(i).Contains("::InstallPackModrinth(")),"整合包分派写入失败");
    }
    if(File.Exists(backup)) Require(Hash(backup)==originalHash,"已有不同原文件的备份，请先还原或选择新的 PCL 文件");
    else File.Copy(path,backup,false);
    var info=new PatchInfo { OriginalSha256=originalHash, PatchedSha256=Hash(temp), FileName=Path.GetFileName(path), CreatedUtc=DateTime.UtcNow.ToString("o") };
    File.WriteAllText(record,new JavaScriptSerializer().Serialize(info));
    Replace(temp,path);
    return "已恢复下载与整合包安装功能。\r\n原文件备份："+backup;
   } finally { if(File.Exists(temp)) File.Delete(temp); }
  }
  public static string Restore(string path) {
   path=Path.GetFullPath(path);
   EnsureNotRunning(path);
   var backup=path+".pcl-helper.bak";
   var record=path+".pcl-helper.json";
   Require(File.Exists(backup)&&File.Exists(record),"未找到此文件的原始备份");
   var info=new JavaScriptSerializer().Deserialize<PatchInfo>(File.ReadAllText(record));
   Require(Hash(backup)==info.OriginalSha256,"备份校验失败");
   var current=Hash(path);
   if(current==info.OriginalSha256) return "此文件已经是原版。";
   Require(current==info.PatchedSha256,"PCL 文件在恢复下载后被其他程序修改，未覆盖");
   var temp=path+".pcl-helper-restore-"+Guid.NewGuid().ToString("N")+".tmp";
   try {File.Copy(backup,temp,false); Replace(temp,path);} finally {if(File.Exists(temp))File.Delete(temp);}
   return "已还原为操作前的 PCL，备份继续保留。";
  }
  static void RestoreLoader(ModuleDefinition m) {
   var f=Method(Type(m,"PageDownloadInstall"),"LoaderInit",0);
   var ins=f.Body.Instructions;
   var start=ins.SingleOrDefault(i=>i.OpCode==OpCodes.Ldstr && Equals(i.Operand,"关闭下载"));
   Require(start!=null,"自动安装入口不匹配");
   var finish=ins.Skip(ins.IndexOf(start)).FirstOrDefault(i=>i.OpCode==OpCodes.Newobj && ((MethodReference)i.Operand).DeclaringType.FullName.StartsWith("PCL.ModLoader/LoaderTask`2",StringComparison.Ordinal));
   Require(finish!=null,"自动安装加载器不匹配");
   var field=Type(m,"ModDownload").Fields.Single(x=>x.Name=="DlClientListLoader");
   // Keep instruction objects so all existing branch targets remain valid.
   var from=ins.IndexOf(start); var to=ins.IndexOf(finish);
   for(var n=from;n<=to;n++)Nop(ins[n]);
   start.OpCode=OpCodes.Ldsfld; start.Operand=field;
  }
  static void RestoreModifyCard(ModuleDefinition m) {
   var f=Method(Type(m,"PageDownloadModify"),"LoadInstanceFinished",1);
   var getter=f.Body.Instructions.SingleOrDefault(i=>Operand(i).Contains("::get_CardMinecraft()"));
   Require(getter!=null && getter.Next.OpCode==OpCodes.Ldc_I4_2 && Operand(getter.Next.Next).Contains("::set_Visibility("),"版本修改入口不匹配");
   getter.Next.OpCode=OpCodes.Ldc_I4_0;
  }
  static void RestorePackDispatch(ModuleDefinition m) {
   var t=Type(m,"ModModpack"); var f=Method(t,"ModpackInstall",3); var ins=f.Body.Instructions;
   var denied=ins.SingleOrDefault(i=>i.OpCode==OpCodes.Ldstr && Equals(i.Operand,Restriction));
   Require(denied!=null && denied.Next.OpCode==OpCodes.Newobj && denied.Next.Next.OpCode==OpCodes.Throw,"整合包限制入口不匹配");
   Require(f.Body.Variables.Count>4 && f.Body.Variables[1].VariableType.FullName=="System.IO.Compression.ZipArchive" && f.Body.Variables[2].VariableType.FullName=="System.String" && f.Body.Variables[4].VariableType.FullName=="System.Int32","整合包局部变量不匹配");
   var eh=f.Body.ExceptionHandlers.SingleOrDefault(e=>e.HandlerType==ExceptionHandlerType.Finally && e.TryStart.Offset<denied.Offset && e.HandlerStart.Offset>denied.Offset);
   Require(eh!=null && eh.HandlerEnd==null,"整合包清理结构不匹配");
   Nop(denied.Next.Next); Nop(denied.Next); Nop(denied);
   var result=new VariableDefinition(f.ReturnType); f.Body.Variables.Add(result); f.Body.InitLocals=true;
   var ret=Instruction.Create(OpCodes.Ldloc,result); var il=f.Body.GetILProcessor();
   denied.Next.Next.OpCode=OpCodes.Leave;denied.Next.Next.Operand=ret;
   var code=new List<Instruction>();
   var targets=new Instruction[10];
   for(int i=0;i<targets.Length;i++)targets[i]=Instruction.Create(OpCodes.Nop);
   var fallback=Instruction.Create(OpCodes.Nop);
   code.Add(Instruction.Create(OpCodes.Ldloc,f.Body.Variables[4])); code.Add(Instruction.Create(OpCodes.Switch,targets)); code.Add(Instruction.Create(OpCodes.Br,fallback));
   var names=new Dictionary<int,string>{{0,"InstallPackCurseForge"},{1,"InstallPackHMCL"},{2,"InstallPackMMC"},{3,"InstallPackMCBBS"},{4,"InstallPackModrinth"},{9,"InstallPackLauncherPack"}};
   for(int n=0;n<10;n++) {
    code.Add(targets[n]);
    if(!names.ContainsKey(n)){code.Add(Instruction.Create(OpCodes.Br,fallback));continue;}
    var call=t.Methods.Single(x=>x.Name==names[n]);
    code.Add(Instruction.Create(OpCodes.Ldarg_0)); code.Add(Instruction.Create(OpCodes.Ldloc,f.Body.Variables[1])); code.Add(Instruction.Create(OpCodes.Ldloc,f.Body.Variables[2]));
    if(call.Parameters.Count>=4)code.Add(Instruction.Create(OpCodes.Ldarg_1));
    if(call.Parameters.Count>=5)code.Add(Instruction.Create(OpCodes.Ldarg_2));
    code.Add(Instruction.Create(OpCodes.Call,call)); code.Add(Instruction.Create(OpCodes.Stloc,result)); code.Add(Instruction.Create(OpCodes.Leave,ret));
   }
   code.Add(fallback); code.Add(Instruction.Create(OpCodes.Ldarg_0)); code.Add(Instruction.Create(OpCodes.Ldloc,f.Body.Variables[1])); code.Add(Instruction.Create(OpCodes.Call,t.Methods.Single(x=>x.Name=="InstallPackCompress"))); code.Add(Instruction.Create(OpCodes.Stloc,result)); code.Add(Instruction.Create(OpCodes.Leave,ret));
   // Insert after the existing target, inside the original try/finally region.
   var cursor=denied;
   foreach(var i in code){il.InsertAfter(cursor,i);cursor=i;}
   il.Append(ret); il.Append(Instruction.Create(OpCodes.Ret)); eh.HandlerEnd=ret;
   LongBranches(f);
  }
  static void RestorePackDialog(ModuleDefinition m) {
   var t=Type(m,"ModModpack"); var f=Method(t,"ModpackInstall",0);
   Require(f.Body.Instructions.Any(i=>Equals(i.Operand,Restriction)),"本地整合包入口不匹配");
   var closure=t.NestedTypes.Single(x=>x.Fields.Any(y=>y.Name=="$VB$Local_File") && x.Methods.Any(y=>y.Name=="_Lambda$__0"));
   var ctor=closure.Methods.Single(x=>x.IsConstructor && !x.IsStatic && x.Parameters.Count==0);
   f.Body=new Mono.Cecil.Cil.MethodBody(f) { InitLocals=true };
   var c=new VariableDefinition(closure); var d=new VariableDefinition(m.ImportReference(typeof(OpenFileDialog))); f.Body.Variables.Add(c);f.Body.Variables.Add(d);
   var il=f.Body.GetILProcessor(); var done=Instruction.Create(OpCodes.Ret);
   il.Emit(OpCodes.Newobj,ctor);il.Emit(OpCodes.Stloc,c);
   il.Emit(OpCodes.Newobj,m.ImportReference(typeof(OpenFileDialog).GetConstructor(System.Type.EmptyTypes))); il.Emit(OpCodes.Stloc,d);
   il.Emit(OpCodes.Ldloc,d);il.Emit(OpCodes.Ldstr,"整合包文件|*.zip;*.mrpack|所有文件|*.*");il.Emit(OpCodes.Callvirt,m.ImportReference(typeof(FileDialog).GetProperty("Filter").GetSetMethod()));
   il.Emit(OpCodes.Ldloc,d);il.Emit(OpCodes.Callvirt,m.ImportReference(typeof(CommonDialog).GetMethod("ShowDialog",System.Type.EmptyTypes)));il.Emit(OpCodes.Pop);
   il.Emit(OpCodes.Ldloc,d);il.Emit(OpCodes.Callvirt,m.ImportReference(typeof(FileDialog).GetProperty("FileName").GetGetMethod()));il.Emit(OpCodes.Call,m.ImportReference(typeof(string).GetMethod("IsNullOrEmpty")));il.Emit(OpCodes.Brtrue,done);
   il.Emit(OpCodes.Ldloc,c);il.Emit(OpCodes.Ldloc,d);il.Emit(OpCodes.Callvirt,m.ImportReference(typeof(FileDialog).GetProperty("FileName").GetGetMethod()));il.Emit(OpCodes.Stfld,closure.Fields.Single(x=>x.Name=="$VB$Local_File"));
   il.Emit(OpCodes.Ldloc,c);il.Emit(OpCodes.Ldftn,closure.Methods.Single(x=>x.Name=="_Lambda$__0"));il.Emit(OpCodes.Newobj,m.ImportReference(typeof(ThreadStart).GetConstructor(new[]{typeof(object),typeof(IntPtr)})));il.Emit(OpCodes.Newobj,m.ImportReference(typeof(Thread).GetConstructor(new[]{typeof(ThreadStart)})));il.Emit(OpCodes.Dup);il.Emit(OpCodes.Ldc_I4_1);il.Emit(OpCodes.Callvirt,m.ImportReference(typeof(Thread).GetProperty("IsBackground").GetSetMethod()));il.Emit(OpCodes.Callvirt,m.ImportReference(typeof(Thread).GetMethod("Start",System.Type.EmptyTypes)));il.Append(done);
  }
  static void RestoreResourceInstall(ModuleDefinition m) {
   var t=Type(m,"PageDownloadResourceDetail");var f=Method(t,"InitializeStackResourceVersions",1);var ins=f.Body.Instructions;
   var call=ins.SingleOrDefault(i=>i.OpCode==OpCodes.Ldftn && Operand(i).Contains("::Save_Click("));
   Require(call!=null && call.Next.OpCode==OpCodes.Newobj && call.Next.Next.OpCode==OpCodes.Ldnull,"在线整合包入口不匹配");
   var primary=Type(m,"MyListItem").NestedTypes.Single(x=>x.Name=="ClickEventHandler");
   var wrapper=new MethodDefinition("PclHelperPrimaryClick",Mono.Cecil.MethodAttributes.Private,m.TypeSystem.Void);
   foreach(var p in primary.Methods.Single(x=>x.Name=="Invoke").Parameters)wrapper.Parameters.Add(new ParameterDefinition(p.ParameterType));
   t.Methods.Add(wrapper);var il=wrapper.Body.GetILProcessor();var save=Instruction.Create(OpCodes.Ldarg_0);
   il.Emit(OpCodes.Ldarg_0);il.Emit(OpCodes.Ldfld,t.Fields.Single(x=>x.Name=="TargetResourceType"));il.Emit(OpCodes.Ldc_I4_2);il.Emit(OpCodes.Bne_Un,save);
   il.Emit(OpCodes.Ldarg_0);il.Emit(OpCodes.Ldarg_1);il.Emit(OpCodes.Castclass,Type(m,"MyListItem"));il.Emit(OpCodes.Ldarg_2);il.Emit(OpCodes.Call,Method(t,"Install_Click",2));il.Emit(OpCodes.Ret);
   il.Append(save);il.Emit(OpCodes.Ldarg_1);il.Emit(OpCodes.Ldarg_2);il.Emit(OpCodes.Call,Method(t,"Save_Click",2));il.Emit(OpCodes.Ret);
   call.Operand=wrapper;
   var second=Type(m,"MyIconButton").NestedTypes.Single(x=>x.Name=="ClickEventHandler");
   var nullarg=call.Next.Next; var ready=Instruction.Create(OpCodes.Nop); var absent=Instruction.Create(OpCodes.Ldnull);
   var injected=new[]{Instruction.Create(OpCodes.Ldarg_0),Instruction.Create(OpCodes.Ldfld,t.Fields.Single(x=>x.Name=="TargetResourceType")),Instruction.Create(OpCodes.Ldc_I4_2),Instruction.Create(OpCodes.Bne_Un,absent),Instruction.Create(OpCodes.Ldarg_0),Instruction.Create(OpCodes.Ldftn,Method(t,"Save_Click",2)),Instruction.Create(OpCodes.Newobj,second.Methods.Single(x=>x.IsConstructor)),Instruction.Create(OpCodes.Br,ready),absent,ready};
   Nop(nullarg);var proc=f.Body.GetILProcessor();var cursor=nullarg;foreach(var i in injected){proc.InsertAfter(cursor,i);cursor=i;}LongBranches(f);
  }
 }
}






