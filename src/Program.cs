using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Microsoft.Win32;
[assembly: AssemblyTitle("PCL-Helper")]
[assembly: AssemblyDescription("PCL 与 HMCL 下载功能恢复工具")]
[assembly: AssemblyCompany("Turing Mirror Software")]
[assembly: AssemblyProduct("PCL-Helper")]

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
  readonly ListBox list;
  readonly Button[] actions;
  readonly Button stop;
  readonly Button selectionToggle;
  readonly ObservableCollection<LauncherEntry> entries=new ObservableCollection<LauncherEntry>();
  readonly Dictionary<string,LauncherEntry> byPath=new Dictionary<string,LauncherEntry>(StringComparer.OrdinalIgnoreCase);
  CancellationTokenSource scanCancellation;
  bool busy;
  public MainWindow(string selected) : this(selected,LauncherDiscovery.AutomaticRoots()) {}
  public MainWindow(string selected,IEnumerable<string> startupRoots) {
   var xaml=@"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Margin='30'>
<Grid.Resources>
 <SolidColorBrush x:Key='Accent' Color='#1289f0'/><SolidColorBrush x:Key='Ink' Color='#1e242b'/><SolidColorBrush x:Key='Muted' Color='#5d6874'/>
 <Style TargetType='Button'><Setter Property='Padding' Value='18,11'/><Setter Property='Margin' Value='0,0,10,0'/><Setter Property='Background' Value='White'/><Setter Property='Foreground' Value='{StaticResource Ink}'/><Setter Property='BorderThickness' Value='0'/><Setter Property='Cursor' Value='Hand'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='Button'><Border x:Name='Bg' Background='{TemplateBinding Background}' CornerRadius='8' Padding='{TemplateBinding Padding}'><ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/></Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Bg' Property='Opacity' Value='0.8'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter TargetName='Bg' Property='Opacity' Value='0.45'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
 <Style TargetType='ListBoxItem'><Setter Property='HorizontalContentAlignment' Value='Stretch'/><Setter Property='Padding' Value='10'/><Setter Property='Margin' Value='0,2'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='ListBoxItem'><Border x:Name='Row' Background='Transparent' CornerRadius='8' Padding='{TemplateBinding Padding}'><ContentPresenter/></Border><ControlTemplate.Triggers><Trigger Property='IsSelected' Value='True'><Setter TargetName='Row' Property='Background' Value='#e8f4fd'/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
</Grid.Resources>
<Grid.RowDefinitions><RowDefinition Height='Auto'/><RowDefinition Height='*'/><RowDefinition Height='Auto'/><RowDefinition Height='140'/><RowDefinition Height='Auto'/></Grid.RowDefinitions>
<Grid><Grid.ColumnDefinitions><ColumnDefinition Width='88'/><ColumnDefinition Width='*'/></Grid.ColumnDefinitions><Image x:Name='BrandLogo' Width='72' Height='72' HorizontalAlignment='Left' Stretch='Uniform'/><StackPanel Grid.Column='1' VerticalAlignment='Center'><TextBlock x:Name='Brand' Text='PCL-Helper' FontSize='25' FontWeight='SemiBold'/><TextBlock Text='恢复 PCL 与 HMCL 下载功能' Foreground='{StaticResource Muted}' Margin='0,9,0,0'/></StackPanel></Grid>
<Border Grid.Row='1' Background='White' CornerRadius='14' Padding='20' Margin='0,20,0,0'><Grid>
<Grid.RowDefinitions><RowDefinition Height='Auto'/><RowDefinition Height='Auto'/><RowDefinition Height='Auto'/><RowDefinition Height='*'/></Grid.RowDefinitions>
<TextBlock Text='启动器' FontWeight='SemiBold' Margin='0,0,0,12'/>
<Grid Grid.Row='1'><Grid.ColumnDefinitions><ColumnDefinition Width='*'/><ColumnDefinition Width='Auto'/></Grid.ColumnDefinitions><TextBox x:Name='PathBox' VerticalContentAlignment='Center' Padding='10' BorderBrush='#dfe4ea' BorderThickness='1' Background='#f4f6f8'/><Button x:Name='Browse' Grid.Column='1' Content='添加程序' Margin='10,0,0,0'/></Grid>
<WrapPanel Grid.Row='2' Margin='0,12,0,8'><Button x:Name='Find' Content='快速扫描'/><Button x:Name='FindFolder' Content='扫描目录'/><Button x:Name='Stop' Content='停止查找' IsEnabled='False'/></WrapPanel>
<ListBox x:Name='Launchers' Grid.Row='3' BorderThickness='0' Background='Transparent' ScrollViewer.HorizontalScrollBarVisibility='Disabled'>
<ListBox.ItemTemplate><DataTemplate><Grid><Grid.ColumnDefinitions><ColumnDefinition Width='Auto'/><ColumnDefinition Width='*'/></Grid.ColumnDefinitions><CheckBox IsChecked='{Binding Selected,Mode=TwoWay}' VerticalAlignment='Center' Margin='0,0,12,0'/><StackPanel Grid.Column='1'><StackPanel Orientation='Horizontal'><TextBlock Text='{Binding KindLabel}' FontWeight='SemiBold' Foreground='{StaticResource Accent}' Margin='0,0,10,0'/><TextBlock Text='{Binding Name}' FontWeight='SemiBold'/></StackPanel><TextBlock Text='{Binding FullPath}' Foreground='{StaticResource Muted}' FontSize='11' TextWrapping='Wrap' Margin='0,4,0,0'/><TextBlock Text='{Binding Status}' Foreground='{StaticResource Muted}' FontSize='11' TextWrapping='Wrap' Margin='0,4,0,0'/></StackPanel></Grid></DataTemplate></ListBox.ItemTemplate>
</ListBox>
</Grid></Border>
<Grid Grid.Row='2' Margin='0,16,0,0'><Grid.ColumnDefinitions><ColumnDefinition Width='*'/><ColumnDefinition Width='Auto'/></Grid.ColumnDefinitions><WrapPanel><Button x:Name='Enable' Content='恢复所选' Background='{StaticResource Accent}' Foreground='White'/><Button x:Name='Restore' Content='还原所选'/><Button x:Name='Inspect' Content='检查所选'/><Button x:Name='Launch' Content='启动所选'/></WrapPanel><Button x:Name='ToggleSelection' Grid.Column='1' Content='全选' VerticalAlignment='Top' Margin='10,0,0,0'/></Grid>
<Border Grid.Row='3' Background='White' CornerRadius='14' Padding='16' Margin='0,16,0,12'><ScrollViewer VerticalScrollBarVisibility='Auto'><TextBlock x:Name='Status' Text='自动查找常用目录，也可以添加或拖入多个程序。' Foreground='{StaticResource Muted}' TextWrapping='Wrap' LineHeight='22'/></ScrollViewer></Border>
<StackPanel Grid.Row='4'><TextBlock x:Name='Version' FontSize='11' Foreground='{StaticResource Muted}'/><WrapPanel x:Name='BrandLinks' Margin='0,10,0,0'/></StackPanel>
</Grid>";
   Title="PCL-Helper "+AppVersion.Name;Width=840;Height=780;MinWidth=740;MinHeight=710;WindowStartupLocation=WindowStartupLocation.CenterScreen;FontFamily=new System.Windows.Media.FontFamily("Microsoft YaHei UI");FontSize=13;Background=(System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#f4f6f8");Foreground=(System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#1e242b");
   var grid=(Grid)XamlReader.Parse(xaml);Content=grid;
   Icon=Branding.Image("App.ico");((Image)grid.FindName("BrandLogo")).Source=Branding.Image("Logo.png");
   ((TextBlock)grid.FindName("Brand")).Text="PCL-Helper "+AppVersion.Name;
   ((TextBlock)grid.FindName("Version")).Text="版本号："+AppVersion.Name+" by 图灵镜 Turing Mirror";
   var links=Branding.ReadLinks();var footer=(WrapPanel)grid.FindName("BrandLinks");
   foreach(var link in links.links){var target=link;var shortcut=FooterButton(link.label);shortcut.ToolTip=link.url;shortcut.Click+=(s,e)=>{try{Process.Start(new ProcessStartInfo(target.url){UseShellExecute=true});}catch(Exception ex){status.Text="打开链接失败："+ex.Message;}};footer.Children.Add(shortcut);}
   var community=FooterButton(links.communityLabel);community.Name="Community";community.Click+=(s,e)=>CreateCommunityWindow().ShowDialog();footer.Children.Add(community);
   path=(TextBox)grid.FindName("PathBox");status=(TextBlock)grid.FindName("Status");list=(ListBox)grid.FindName("Launchers");list.ItemsSource=entries;
   Func<string,Button> button=name=>(Button)grid.FindName(name);
   var browse=button("Browse");var find=button("Find");var folder=button("FindFolder");stop=button("Stop");
   var enable=button("Enable");var restore=button("Restore");var inspect=button("Inspect");var launch=button("Launch");
   selectionToggle=button("ToggleSelection");
   actions=new[]{browse,find,folder,enable,restore,inspect,launch,selectionToggle};
   browse.Click+=(s,e)=>{var dialog=new OpenFileDialog{Filter="启动器程序|*.exe",Title="添加 PCL 或 HMCL",Multiselect=true};if(dialog.ShowDialog(this)==true)AddFiles(dialog.FileNames);};
   find.Click+=async(s,e)=>await Scan(LauncherDiscovery.AutomaticRoots(),true);
   folder.Click+=async(s,e)=>{using(var dialog=new System.Windows.Forms.FolderBrowserDialog{Description="选择要查找启动器的目录",ShowNewFolderButton=false}){if(dialog.ShowDialog()==System.Windows.Forms.DialogResult.OK)await Scan(new[]{dialog.SelectedPath});}};
   stop.Click+=(s,e)=>{if(scanCancellation!=null)scanCancellation.Cancel();};
   enable.Click+=async(s,e)=>await Operate(LauncherOperation.Enable);
   restore.Click+=async(s,e)=>await Operate(LauncherOperation.Restore);
   inspect.Click+=async(s,e)=>await Operate(LauncherOperation.Inspect);
   launch.Click+=async(s,e)=>await Operate(LauncherOperation.Launch);
   selectionToggle.Click+=(s,e)=>{var select=!entries.All(item=>item.Selected);foreach(var item in entries)item.Selected=select;UpdateSelectionButton();};
   list.SelectionChanged+=(s,e)=>{var item=list.SelectedItem as LauncherEntry;if(item!=null)path.Text=item.FullPath;};
   path.KeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Enter)AddFiles(new[]{path.Text.Trim().Trim('"')});};
   AllowDrop=true;PreviewDragOver+=(s,e)=>{e.Effects=!busy&&e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;e.Handled=true;};Drop+=async(s,e)=>{if(busy)return;var files=e.Data.GetData(DataFormats.FileDrop) as string[];if(files==null)return;AddFiles(files.Where(File.Exists));var directories=files.Where(Directory.Exists).ToArray();if(directories.Length>0)await Scan(directories);};
   if(!string.IsNullOrWhiteSpace(selected)&&File.Exists(selected))AddFiles(new[]{selected});
   Loaded+=async(s,e)=>await Scan(startupRoots,true);
   Closed+=(s,e)=>{if(scanCancellation!=null)scanCancellation.Cancel();};
  }
  static Button FooterButton(string label){return new Button{Content=label,Padding=new Thickness(12,7,12,7),Margin=new Thickness(0,0,8,0),FontSize=12};}
  Window CreateCommunityWindow(){var links=Branding.ReadLinks();var panel=new StackPanel{Margin=new Thickness(20)};panel.Children.Add(new TextBlock{Text=links.communityHint,Margin=new Thickness(0,0,0,12),HorizontalAlignment=HorizontalAlignment.Center});panel.Children.Add(new Image{Source=Branding.Image("CommunityQr.jpg"),Height=450,Stretch=System.Windows.Media.Stretch.Uniform});return new Window{Title=links.communityTitle,Owner=this,Icon=Icon,Content=panel,SizeToContent=SizeToContent.WidthAndHeight,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=System.Windows.Media.Brushes.White};}
  void UpdateSelectionButton(){selectionToggle.Content=entries.Count>0&&entries.All(item=>item.Selected)?"全不选":"全选";}
  void RegisterEntry(LauncherEntry entry){entries.Add(entry);byPath.Add(entry.FullPath,entry);entry.PropertyChanged+=(s,e)=>{if(e.PropertyName=="Selected")UpdateSelectionButton();};UpdateSelectionButton();}
  LauncherEntry AddFile(string file) {
   var full=Path.GetFullPath(file.Trim().Trim('"'));LauncherEntry known;
   if(byPath.TryGetValue(full,out known))return known;
   var entry=LauncherDiscovery.Identify(full);if(entry==null)return null;
   RegisterEntry(entry);return entry;
  }
  void AddFiles(IEnumerable<string> files) {
   var problems=new List<string>();int count=0;
   foreach(var file in files)try{var entry=AddFile(file);if(entry==null)problems.Add("未识别："+Path.GetFileName(file));else{count++;list.SelectedItem=entry;}}catch(Exception ex){problems.Add(ex.Message);}
   status.Text="已添加 "+count+" 个程序。"+(problems.Count>0?"\r\n"+string.Join("\r\n",problems):"");
  }
  void SetBusy(bool value) {busy=value;foreach(var action in actions)action.IsEnabled=!value;path.IsEnabled=!value;list.IsEnabled=!value;}
  async Task Scan(IEnumerable<string> roots,bool quick=false) {
   if(busy)return;
   SetBusy(true);scanCancellation=new CancellationTokenSource();stop.IsEnabled=true;
   var additions=new Progress<LauncherEntry>(item=>{if(!byPath.ContainsKey(item.FullPath))RegisterEntry(item);});
   var progress=new Progress<string>(directory=>status.Text="正在查找，已找到 "+entries.Count+" 个程序。\r\n"+directory);
   status.Text="正在查找启动器，可随时停止查找。";
   try{await Task.Run(()=>LauncherDiscovery.Scan(roots,quick?LauncherDiscovery.QuickOptions():new DiscoveryOptions(),scanCancellation.Token,((IProgress<LauncherEntry>)additions).Report,((IProgress<string>)progress).Report));status.Text=entries.Count==0?"未找到 PCL 或 HMCL 程序。可点击“添加程序”、拖入启动器 exe，或选择启动器所在目录扫描。":"查找完成，共 "+entries.Count+" 个程序。请勾选后操作。";}
   catch(OperationCanceledException){status.Text="已停止查找，保留已找到的 "+entries.Count+" 个程序。";}
   catch(Exception ex){status.Text=ex.Message;}
   finally{scanCancellation.Dispose();scanCancellation=null;stop.IsEnabled=false;SetBusy(false);}
  }
  async Task Operate(LauncherOperation operation) {
   if(busy)return;
   try{if(!string.IsNullOrWhiteSpace(path.Text)&&!byPath.ContainsKey(Path.GetFullPath(path.Text.Trim().Trim('"'))))AddFiles(new[]{path.Text});}catch(Exception ex){status.Text=ex.Message;return;}
   var targets=entries.Where(item=>item.Selected).ToArray();if(targets.Length==0){status.Text="请先添加或勾选启动器。";return;}
   SetBusy(true);int failures=0;var output=new StringBuilder();
   try {
    foreach(var item in targets) {
     status.Text="正在处理 "+item.Name+"……";
     try{var result=await Task.Run(()=>LauncherActions.Run(item,operation));item.Status=result.Split(new[]{"\r\n","\n"},StringSplitOptions.None)[0];output.AppendLine(item.KindLabel+" · "+item.FullPath).AppendLine(result).AppendLine();}
     catch(Exception ex){failures++;item.Status="失败："+ex.Message;output.AppendLine(item.KindLabel+" · "+item.FullPath).AppendLine(item.Status).AppendLine();}
    }
    status.Text="处理完成："+(targets.Length-failures)+" 项成功，"+failures+" 项失败。\r\n"+output;
   } finally {SetBusy(false);}
  }
 }
}
