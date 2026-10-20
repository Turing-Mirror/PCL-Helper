using System;
using System.IO;
class HmclLaunchProbe {
 static int Main() {
  File.WriteAllLines("launch-probe.txt",new[]{Environment.GetEnvironmentVariable("HMCL_JAVA_OPTS") ?? "",Directory.GetCurrentDirectory()});
  return 0;
 }
}
