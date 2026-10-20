import java.time.ZoneId;
import java.util.Locale;
import java.util.Properties;
public class HmclRegionProbe {
 public static void main(String[] args) throws Exception {
  Properties properties=new Properties();
  try(var stream=HmclRegionProbe.class.getResourceAsStream("/assets/hmcl.properties")){properties.load(stream);}
  var type=Class.forName("org.jackhuang.hmcl.util.RegionUtils");
  boolean machine=(boolean)type.getMethod("isIsChinaMainlandMachine").invoke(null);
  boolean network=(boolean)type.getMethod("isIsChinaMainlandNetwork").invoke(null);
  String expectedZone=args.length>0?args[0]:"Asia/Taipei";
  boolean expectedNetwork=args.length>1?Boolean.parseBoolean(args[1]):false;
  System.out.println("Actual HMCL artifact: version="+properties.getProperty("hmcl.version")+", commit="+properties.getProperty("hmcl.version.hash")+", timezone="+ZoneId.systemDefault()+", country="+Locale.getDefault().getCountry()+", machine="+machine+", networkRestricted="+network);
  if(!expectedZone.equals(ZoneId.systemDefault().getId())||!"CN".equals(Locale.getDefault().getCountry())||!machine||network!=expectedNetwork)throw new AssertionError("Unexpected actual HMCL region result: expected timezone="+expectedZone+", networkRestricted="+expectedNetwork);
  System.out.println("PASS expected actual HMCL region result");
 }
}
