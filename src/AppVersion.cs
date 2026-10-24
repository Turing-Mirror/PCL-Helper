using System.Reflection;
[assembly: AssemblyVersion(PclHelper.AppVersion.AssemblyNumber)]
[assembly: AssemblyFileVersion(PclHelper.AppVersion.AssemblyNumber)]
[assembly: AssemblyInformationalVersion(PclHelper.AppVersion.Name)]
namespace PclHelper {
 public static class AppVersion {
  public const string Name = "v4";
  public const string AssemblyNumber = "4.0.0.0";
 }
}
