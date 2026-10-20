using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
class GuiWorkflowTest {
 static Window window;static FrameworkElement content;static ListBox list;static TextBlock status;
 static readonly BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Assert(bool value,string message){if(!value)throw new Exception(message);}
 static byte[] Read(string path){for(int n=0;;n++)try{return File.ReadAllBytes(path);}catch(IOException){if(n>=50)throw;System.Threading.Thread.Sleep(100);}}
 static string Hash(string path){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(Read(path)));}
 static object Property(object item,string name){return item.GetType().GetProperty(name).GetValue(item,null);}
 static async Task Idle(){var until=DateTime.UtcNow.AddSeconds(30);while((bool)window.GetType().GetField("busy",Private).GetValue(window)){if(DateTime.UtcNow>until)throw new TimeoutException("GUI operation did not complete");await Task.Delay(25);}await Task.Delay(40);}
 static async Task Click(string name){var button=(Button)content.FindName(name);Assert(button!=null&&button.IsEnabled,"Button unavailable: "+name);button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Idle();}
 static T Descendant<T>(DependencyObject node) where T:DependencyObject {if(node is T)return (T)node;for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++){var found=Descendant<T>(VisualTreeHelper.GetChild(node,i));if(found!=null)return found;}return null;}
 static async Task Run(string root,string original,string probe,string evidence) {
  await Idle();Assert(list.Items.Count==4,"Automatic discovery did not populate all four executables");
  Assert(window.Title=="PCL-Helper v2"&&((TextBlock)content.FindName("Version")).Text.StartsWith("v2"),"GUI version mismatch");
  var items=list.Items.Cast<object>().ToArray();var pcl=items.Where(x=>Property(x,"Kind").ToString()=="Pcl"&&Property(x,"Name").ToString()!="PCL-broken.exe").ToArray();
  var broken=items.Single(x=>Property(x,"Name").ToString()=="PCL-broken.exe");var hmcl=items.Single(x=>Property(x,"Kind").ToString()=="Hmcl");
  var expected=Hash(original);var hmclPath=Property(hmcl,"FullPath").ToString();var hmclHash=Hash(hmclPath);
  await Click("ClearSelection");Assert(items.All(x=>!(bool)Property(x,"Selected")),"Clear selection button failed");
  await Click("Enable");Assert(status.Text.Contains("请先添加或勾选"),"Empty selection did not return clear feedback");
  await Click("SelectAll");Assert(items.All(x=>(bool)Property(x,"Selected")),"Select all button failed");
  foreach(var item in new[]{broken,hmcl}) {list.ScrollIntoView(item);list.UpdateLayout();var container=list.ItemContainerGenerator.ContainerFromItem(item);var box=Descendant<CheckBox>(container);Assert(box!=null,"Checkbox missing");box.IsChecked=false;}
  await Task.Delay(30);Assert(!(bool)Property(broken,"Selected")&&!(bool)Property(hmcl,"Selected"),"Checkbox binding did not update selection");
  await Click("Enable");foreach(var item in pcl)Assert(Hash(Property(item,"FullPath").ToString())!=expected,"Selected PCL not patched");
  Assert(status.Text.Contains("2 项成功，0 项失败"),"Selected result summary incorrect");
  var report=Path.Combine(root,"launch-probe.txt");Assert(!File.Exists(report),"Unchecked HMCL was launched");
  await Click("Restore");foreach(var item in pcl)Assert(Hash(Property(item,"FullPath").ToString())==expected,"Selected restore was not exact");
  await Click("EnableAll");foreach(var item in pcl)Assert(Hash(Property(item,"FullPath").ToString())!=expected,"All restore did not include unchecked targets");
  Assert(Property(broken,"Status").ToString().StartsWith("失败："),"Invalid PCL was not reported independently");
  Assert(status.Text.Contains("3 项成功，1 项失败"),"Batch did not continue after per-file failure");
  for(int i=0;i<100&&!File.Exists(report);i++)await Task.Delay(25);Assert(File.Exists(report),"All operation did not launch HMCL");
  Assert(File.ReadAllLines(report)[0].Contains("-Duser.timezone=Asia/Taipei"),"HMCL did not receive its option");
  var written=File.GetLastWriteTimeUtc(report);await Click("RestoreAll");
  foreach(var item in pcl)Assert(Hash(Property(item,"FullPath").ToString())==expected,"All restore was not exact");
  Assert(Hash(hmclPath)==hmclHash&&File.GetLastWriteTimeUtc(report)==written,"HMCL restore modified or relaunched the executable");
  await Click("ClearSelection");pcl[0].GetType().GetProperty("Selected").SetValue(pcl[0],true,null);await Click("Inspect");Assert(status.Text.Contains("SHA256"),"Inspect result missing");
  var all=(Button)content.FindName("FindAll");all.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));var stop=(Button)content.FindName("Stop");Assert(stop.IsEnabled,"Stop scan button not enabled");stop.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Idle();Assert(status.Text.Contains("已停止查找"),"Stop scan button did not cancel");
  Assert(((Button)content.FindName("EnableAll")).IsEnabled,"Controls were not restored after cancellation");
  status.Text="v2 窗口流程验证通过：自动查找、勾选、批量恢复/还原、单项失败继续、停止查找。";
  window.UpdateLayout();var bitmap=new RenderTargetBitmap((int)content.ActualWidth+60,(int)content.ActualHeight+60,96,96,PixelFormats.Pbgra32);var drawing=new DrawingVisual();using(var canvas=drawing.RenderOpen()){var bounds=new Rect(0,0,content.ActualWidth+60,content.ActualHeight+60);canvas.DrawRectangle(window.Background,null,bounds);canvas.DrawRectangle(new VisualBrush(content),null,new Rect(30,30,content.ActualWidth,content.ActualHeight));}bitmap.Render(drawing);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(Path.Combine(evidence,"gui-v2.png")))encoder.Save(file);
  File.WriteAllText(Path.Combine(evidence,"gui-workflow.txt"),"PASS: real WPF window Loaded scan; checkbox binding; select/clear; empty selection feedback; selected PCL patch and SHA256 restore; all targets including unchecked HMCL; per-file failure isolation; HMCL child environment; unchanged HMCL on restore; inspect; scan cancellation; controls re-enabled; v2 labels.\r\n");
  Console.WriteLine(File.ReadAllText(Path.Combine(evidence,"gui-workflow.txt")));
 }
 [STAThread] static int Main(string[] args) {
  var gui=Assembly.Load(Read(Path.GetFullPath(args[0])));AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{if(new AssemblyName(e.Name).Name!="Mono.Cecil")return null;using(var stream=gui.GetManifestResourceStream("Mono.Cecil.dll")){var bytes=new byte[stream.Length];stream.Read(bytes,0,bytes.Length);return Assembly.Load(bytes);}};
  var root=Path.GetFullPath(args[3]);Directory.CreateDirectory(root);var evidence=Path.GetFullPath(args[4]);Directory.CreateDirectory(evidence);
  File.Copy(args[2],Path.Combine(root,"PCL-broken.exe"),true);File.Copy(args[1],Path.Combine(root,"PCL-one.exe"),true);File.Copy(args[1],Path.Combine(root,"PCL-two.exe"),true);File.Copy(args[2],Path.Combine(root,"HMCL-test.exe"),true);
  var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};int exit=1;
  window=(Window)Activator.CreateInstance(gui.GetType("PclHelper.MainWindow",true),new object[]{null,new[]{root}});content=(FrameworkElement)window.Content;list=(ListBox)content.FindName("Launchers");status=(TextBlock)content.FindName("Status");
  window.Loaded+=async(s,e)=>{try{await Run(root,args[1],args[2],evidence);exit=0;}catch(Exception ex){Console.WriteLine(ex);File.WriteAllText(Path.Combine(evidence,"gui-workflow.txt"),ex.ToString());}finally{window.Close();app.Shutdown();}};
  app.Run(window);return exit;
 }
}
