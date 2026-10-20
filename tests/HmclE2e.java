import javafx.application.Platform;
import javafx.scene.Scene;
import javafx.scene.Node;
import javafx.scene.Parent;
import javafx.scene.control.ListView;
import javafx.stage.Stage;
import org.jackhuang.hmcl.download.*;
import org.jackhuang.hmcl.game.*;
import org.jackhuang.hmcl.task.Task;
import org.jackhuang.hmcl.util.RegionUtils;
import org.jackhuang.hmcl.ui.download.VersionsPage;
import java.nio.file.*;
import java.util.concurrent.*;

public class HmclE2e {
 static class Repository extends DefaultGameRepository {
  Repository(Path path){super(path);}
  protected DefaultGameRepositoryLayout createLayout(Path path){return new DefaultGameRepositoryLayout(path);}
  protected DefaultGameInstance createInstance(DefaultGameRepositorySnapshot snapshot,GameInstanceID id,GameInstanceManifest manifest,Path directory){return new Instance(snapshot,id,manifest,directory);}
 }
 static class Instance extends DefaultGameInstance {
  Instance(DefaultGameRepositorySnapshot snapshot,GameInstanceID id,GameInstanceManifest manifest,Path directory){super(snapshot,id,manifest,directory);}
  protected DefaultGameInstance withNewSnapshot(DefaultGameRepositorySnapshot snapshot){return new Instance(snapshot,getId(),getManifest(),getRunDirectory());}
  protected DefaultGameInstance withManifest(DefaultGameRepositorySnapshot snapshot,GameInstanceManifest manifest){return new Instance(snapshot,getId(),manifest,getRunDirectory());}
 }
 static void run(Task<?> task)throws Exception{var executor=task.executor();if(!executor.test())throw new Exception("HMCL task failed",executor.getException());}
 static ListView<?> findList(Node node){if(node instanceof ListView<?> list&&list.getStyleClass().contains("jfx-list-view-float"))return list;if(node instanceof Parent parent)for(Node child:parent.getChildrenUnmodifiable()){var found=findList(child);if(found!=null)return found;}return null;}
 static void page(Path directory,boolean expected)throws Exception {
  boolean restricted=RegionUtils.isIsChinaMainlandNetwork();
  if(restricted!=expected)throw new AssertionError("Region state does not match page test");
  // Preserve the computed region while restoring network access for the real version list.
  System.clearProperty("https.proxyHost");System.clearProperty("https.proxyPort");
  var ready=new CompletableFuture<Void>();
  Platform.startup(()->ready.complete(null));ready.get(30,TimeUnit.SECONDS);
  var settings=org.jackhuang.hmcl.setting.SettingsManager.class.getDeclaredField("launcherSettings");settings.setAccessible(true);settings.set(null,new org.jackhuang.hmcl.setting.LauncherSettings());
  var future=new CompletableFuture<VersionsPage>();
  Platform.runLater(()->{try{var page=new VersionsPage(null,"Minecraft download",null,new DownloadProvider(),GameComponentType.GAME,()->{});var stage=new Stage();stage.setScene(new Scene(page,900,640));org.jackhuang.hmcl.setting.StyleSheets.init(stage.getScene());stage.show();page.applyCss();page.layout();future.complete(page);}catch(Throwable error){future.completeExceptionally(error);}});
  var page=future.get(30,TimeUnit.SECONDS);boolean success=false;
  for(int attempt=0;attempt<120;attempt++) {
   var check=new CompletableFuture<Boolean>();
   Platform.runLater(()->{try{var list=findList(page);if(list!=null&&list.getItems().size()>0){if(list.isDisabled()!=expected)throw new AssertionError("Download list disabled="+list.isDisabled()+", expected="+expected);page.applyCss();page.layout();list.applyCss();list.layout();if(list.lookupAll(".list-cell").stream().noneMatch(n->n instanceof javafx.scene.control.ListCell<?> c&&!c.isEmpty()&&c.getHeight()>0)){check.complete(false);return;}System.out.println("PASS actual HMCL VersionsPage: versions="+list.getItems().size()+", disabled="+list.isDisabled());var image=page.snapshot(null,null);int width=(int)image.getWidth(),height=(int)image.getHeight();var bitmap=new java.awt.image.BufferedImage(width,height,java.awt.image.BufferedImage.TYPE_INT_ARGB);for(int y=0;y<height;y++)for(int x=0;x<width;x++)bitmap.setRGB(x,y,image.getPixelReader().getArgb(x,y));javax.imageio.ImageIO.write(bitmap,"png",directory.resolve("hmcl-page-"+expected+".png").toFile());check.complete(true);}else check.complete(false);}catch(Throwable error){check.completeExceptionally(error);}});
   if(check.get(10,TimeUnit.SECONDS)){success=true;break;}Thread.sleep(500);
  }
  if(!success)throw new AssertionError("Actual download page did not load its remote version list");
  Platform.exit();
 }
 static void install(Path directory,String version)throws Exception {
  var toolkit=new CompletableFuture<Void>();Platform.startup(()->toolkit.complete(null));toolkit.get(30,TimeUnit.SECONDS);
  var repository=new Repository(directory.resolve("minecraft"));repository.refresh();
  var manager=new DefaultDependencyManager(repository,new DownloadProvider(),new DefaultCacheRepository(directory.resolve("cache")));
  var id=new GameInstanceID("helper-e2e-"+version);
  if(!Files.exists(repository.getLayout().getInstanceJson(id))){try(var builder=manager.newGameBuilder(id)){builder.component(GameComponentType.GAME,version);System.out.println("Installing actual HMCL game: "+version);run(builder.buildAsync());}}
  var instance=repository.getInstance(id);var manifest=instance.getResolvedManifest();
  System.out.println("Completing actual HMCL libraries/assets");run(manager.checkGameCompletionAsync(instance,manifest,true));
  if(!manifest.getDownloadInfo().validateChecksum(instance.getInstanceJarFile(),true))throw new AssertionError("Client checksum mismatch");
  long libraries;try(var files=Files.walk(repository.getLayout().getLibrariesDirectory())){libraries=files.filter(Files::isRegularFile).count();}
  long assets;try(var files=Files.walk(repository.getLayout().getAssetDirectory().resolve("objects"))){assets=files.filter(Files::isRegularFile).count();}
  if(libraries==0||assets==0)throw new AssertionError("Missing installed libraries/assets");
  var library=manifest.getLibraries().stream().filter(l->Files.isRegularFile(repository.getLayout().getLibraryFile(id,l))).findFirst().orElseThrow();
  var libraryPath=repository.getLayout().getLibraryFile(id,library);var bytes=Files.readAllBytes(libraryPath);Files.delete(libraryPath);
  run(manager.checkGameCompletionAsync(instance,manifest,true));
  if(!java.util.Arrays.equals(bytes,Files.readAllBytes(libraryPath)))throw new AssertionError("Repair did not restore the removed library");
  System.out.println("PASS actual HMCL install/completion/repair: version="+version+", libraries="+libraries+", assetObjects="+assets+", client="+instance.getInstanceJarFile());
 }
 public static void main(String[] args){try{var directory=Path.of(args[1]).toAbsolutePath();Files.createDirectories(directory);System.setProperty("hmcl.home",directory.resolve("user").toString());System.setProperty("hmcl.dir",directory.resolve("local").toString());org.jackhuang.hmcl.util.CacheRepository.getInstance().changeDirectory(directory.resolve("http-cache"));if(args[0].equals("page"))page(directory,Boolean.parseBoolean(args[2]));else install(directory,args[2]);System.exit(0);}catch(Throwable error){error.printStackTrace();System.exit(1);}}
}
