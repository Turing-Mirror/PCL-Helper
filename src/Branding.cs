using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Media.Imaging;

namespace PclHelper {
 public sealed class BrandLink {
  public string label { get; set; }
  public string url { get; set; }
 }
 public sealed class BrandLinks {
  public BrandLink[] links { get; set; }
  public string communityLabel { get; set; }
  public string communityTitle { get; set; }
  public string communityHint { get; set; }
 }
 public static class Branding {
  public static BrandLinks ReadLinks(){using(var reader=new StreamReader(Resource("Links.json")))return new JavaScriptSerializer().Deserialize<BrandLinks>(reader.ReadToEnd());}
  public static BitmapSource Image(string resource){using(var stream=Resource(resource)){var decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);var image=decoder.Frames[0];image.Freeze();return image;}}
  static Stream Resource(string name){return Assembly.GetExecutingAssembly().GetManifestResourceStream(name);}
 }
}
