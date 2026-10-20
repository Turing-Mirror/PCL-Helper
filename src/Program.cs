using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Microsoft.Win32;
[assembly: AssemblyTitle("PCL-Helper")]
[assembly: AssemblyDescription("PCL 下载功能恢复工具")]
[assembly: AssemblyCompany("Turing Mirror Software")]
[assembly: AssemblyProduct("PCL-Helper")]
[assembly: AssemblyVersion("1.0.0.0")]
namespace PclHelper {
 public static class Program {
  [STAThread] public static int Main(string[] args) {
   AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=> {
    if(new AssemblyName(e.Name).Name!="Mono.Cecil")return null;
    using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Mono.Cecil.dll")) {
     var bytes=new byte[stream.Length];stream.Read(bytes,0,bytes.Length);return Assembly.Load(bytes);
    }
   };
   return Run(args);
  }
  [MethodImpl(MethodImplOptions.NoInlining)] static int Run(string[] args) {
   if(args.Length>=2 && args[0].StartsWith("--") && args[0]!="--ui") {
    var result="";var exit=0;
    try {
     switch(args[0]){case "--inspect":result=Patcher.Inspect(args[1]);break;case "--enable":result=Patcher.Enable(args[1]);break;case "--restore":result=Patcher.Restore(args[1]);break;default:throw new ArgumentException("未知命令");}
    }catch(Exception ex){result=ex.ToString();exit=1;}
    Console.WriteLine(result);
    var idx=Array.IndexOf(args,"--report");if(idx>=0 && idx+1<args.Length)File.WriteAllText(args[idx+1],result);
    return exit;
   }
   var app=new Application();app.Run(new MainWindow(args.Length>0 ? args[args.Length-1] : null));return 0;
  }
 }
 public sealed class MainWindow : Window {
  readonly TextBox path;
  readonly TextBlock status;
  readonly Button[] actions;
  public MainWindow(string selected) {
   var xaml=@"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Margin='30'>
<Grid.Resources>
 <SolidColorBrush x:Key='Accent' Color='#1289f0'/><SolidColorBrush x:Key='Ink' Color='#1e242b'/><SolidColorBrush x:Key='Muted' Color='#5d6874'/>
 <Style TargetType='Button'><Setter Property='Padding' Value='18,11'/><Setter Property='Margin' Value='0,0,10,0'/><Setter Property='Background' Value='White'/><Setter Property='Foreground' Value='{StaticResource Ink}'/><Setter Property='BorderThickness' Value='0'/><Setter Property='Cursor' Value='Hand'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Button'><Border x:Name='Bg' Background='{TemplateBinding Background}' CornerRadius='8' Padding='{TemplateBinding Padding}'><ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Bg' Property='Opacity' Value='0.8'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter TargetName='Bg' Property='Opacity' Value='0.45'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
</Grid.Resources>
<Grid.RowDefinitions><RowDefinition Height='Auto'/><RowDefinition Height='Auto'/><RowDefinition Height='Auto'/><RowDefinition Height='*'/><RowDefinition Height='Auto'/></Grid.RowDefinitions>
<StackPanel><TextBlock Text='PCL-Helper' FontSize='25' FontWeight='SemiBold'/><TextBlock Text='恢复 PCL 下载与整合包安装功能' Foreground='{StaticResource Muted}' Margin='0,9,0,0'/></StackPanel>
<Border Grid.Row='1' Background='White' CornerRadius='14' Padding='20' Margin='0,26,0,0'><StackPanel><TextBlock Text='PCL 程序' FontWeight='SemiBold' Margin='0,0,0,12'/><Grid><Grid.ColumnDefinitions><ColumnDefinition Width='*'/><ColumnDefinition Width='Auto'/></Grid.ColumnDefinitions><TextBox x:Name='PathBox' VerticalContentAlignment='Center' Padding='10' BorderBrush='#dfe4ea' BorderThickness='1' Background='#f4f6f8'/><Button x:Name='Browse' Grid.Column='1' Content='选择文件' Margin='10,0,0,0'/></Grid><TextBlock Text='选择 Plain Craft Launcher 2.exe，也可以拖入文件。操作前请退出 PCL。' FontSize='12' Foreground='{StaticResource Muted}' TextWrapping='Wrap' Margin='0,12,0,0'/></StackPanel></Border>
<WrapPanel Grid.Row='2' Margin='0,20,0,0'><Button x:Name='Enable' Content='恢复下载' Background='{StaticResource Accent}' Foreground='White'/><Button x:Name='Restore' Content='还原原版'/><Button x:Name='Inspect' Content='检查状态'/><Button x:Name='Launch' Content='启动 PCL' Margin='0'/></WrapPanel>
<Border Grid.Row='3' Background='White' CornerRadius='14' Padding='20' Margin='0,20,0,20'><ScrollViewer VerticalScrollBarVisibility='Auto'><TextBlock x:Name='Status' Text='请选择 PCL 2.99.0.0。原文件会自动备份，可随时还原。' Foreground='{StaticResource Muted}' TextWrapping='Wrap' LineHeight='23'/></ScrollViewer></Border>
<TextBlock Grid.Row='4' Text='v1 · Turing Mirror Software · 第三方工具，与 PCL 官方无关联' FontSize='11' Foreground='{StaticResource Muted}' TextWrapping='Wrap'/>
</Grid>";
   Title="PCL-Helper";Width=760;Height=545;MinWidth=660;MinHeight=510;WindowStartupLocation=WindowStartupLocation.CenterScreen;FontFamily=new System.Windows.Media.FontFamily("Microsoft YaHei UI");FontSize=13;Background=(System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#f4f6f8");Foreground=(System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#1e242b");
   var grid=(Grid)XamlReader.Parse(xaml);Content=grid;
   path=(TextBox)grid.FindName("PathBox");status=(TextBlock)grid.FindName("Status");
   var browse=(Button)grid.FindName("Browse");var enable=(Button)grid.FindName("Enable");var restore=(Button)grid.FindName("Restore");var inspect=(Button)grid.FindName("Inspect");var launch=(Button)grid.FindName("Launch");actions=new[]{browse,enable,restore,inspect,launch};
   browse.Click+=(s,e)=>{var dialog=new OpenFileDialog {Filter="PCL 程序|*.exe",Title="选择 PCL"};if(dialog.ShowDialog(this)==true){path.Text=dialog.FileName;Check();}};
   inspect.Click+=(s,e)=>Check();
   enable.Click+=async(s,e)=>await Work(Patcher.Enable,"正在恢复下载功能……");
   restore.Click+=async(s,e)=>await Work(Patcher.Restore,"正在还原原版……");
   launch.Click+=(s,e)=>{try{var file=Selected();Process.Start(new ProcessStartInfo(file){UseShellExecute=true,WorkingDirectory=Path.GetDirectoryName(file)});}catch(Exception ex){status.Text=Friendly(ex);}};
   AllowDrop=true;PreviewDragOver+=(s,e)=>{e.Effects=e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;e.Handled=true;};Drop+=(s,e)=>{var files=e.Data.GetData(DataFormats.FileDrop) as string[];if(files!=null && files.Length==1){path.Text=files[0];Check();}};
   if(!string.IsNullOrWhiteSpace(selected) && File.Exists(selected))path.Text=Path.GetFullPath(selected);
   else {var candidates=Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory,"*.exe").Where(p=>!Path.GetFileName(p).StartsWith("PCL-Helper",StringComparison.OrdinalIgnoreCase) && (Path.GetFileName(p).IndexOf("PCL",StringComparison.OrdinalIgnoreCase)>=0 || Path.GetFileName(p).IndexOf("Plain Craft",StringComparison.OrdinalIgnoreCase)>=0)).ToArray();if(candidates.Length==1)path.Text=candidates[0];}
   if(path.Text.Length>0)Loaded+=(s,e)=>Check();
  }
  string Selected(){var file=Path.GetFullPath(path.Text.Trim().Trim('"'));if(!File.Exists(file))throw new FileNotFoundException("请选择存在的 PCL 程序文件。");return file;}
  void Check(){try{status.Text=Patcher.Inspect(Selected());}catch(Exception ex){status.Text=Friendly(ex);}}
  async Task Work(Func<string,string> action,string message){try{var file=Selected();foreach(var b in actions)b.IsEnabled=false;path.IsEnabled=false;status.Text=message;status.Text=await Task.Run(()=>action(file));}catch(Exception ex){status.Text=Friendly(ex);}finally{foreach(var b in actions)b.IsEnabled=true;path.IsEnabled=true;}}
  static string Friendly(Exception e){if(e is IOException)return e.Message+"\r\n请退出 PCL，并确认文件夹可写。";return e.Message;}
 }
}

