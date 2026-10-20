import java.lang.instrument.Instrumentation;
import java.nio.file.Files;
import java.nio.file.Path;
import java.time.ZoneId;
import java.util.Locale;
import java.util.Properties;

// Runs the official artifact's own region checks before its GUI starts.
public class HmclRegionAgent {
 public static void premain(String argument, Instrumentation instrumentation) {
  String[] args=argument.split(",",3);
  Path report=Path.of(args[2]);
  try {
   Class<?> region=Class.forName("org.jackhuang.hmcl.util.RegionUtils");
   boolean machine=(boolean)region.getMethod("isIsChinaMainlandMachine").invoke(null);
   boolean restricted=(boolean)region.getMethod("isIsChinaMainlandNetwork").invoke(null);
   Properties properties=new Properties();
   try(var stream=region.getResourceAsStream("/assets/hmcl.properties")){properties.load(stream);}
   String result="version="+properties.getProperty("hmcl.version")+", commit="+properties.getProperty("hmcl.version.hash")+", timezone="+ZoneId.systemDefault()+", country="+Locale.getDefault().getCountry()+", machine="+machine+", networkRestricted="+restricted;
   Files.writeString(report,result);
   if(!args[0].equals(ZoneId.systemDefault().getId())||(!"any".equals(args[1])&&restricted!=Boolean.parseBoolean(args[1]))||!machine)throw new AssertionError(result);
   System.exit(0);
  }catch(Throwable error){try{Files.writeString(report,"FAIL: "+error);}catch(Exception ignored){}System.exit(1);}
 }
}
