package org.darkaxt.revamped.updater;

import android.app.*;
import android.content.*;
import android.net.Uri;
import android.os.Bundle;
import android.provider.DocumentsContract;
import android.view.*;
import android.widget.*;
import org.json.*;
import java.util.concurrent.Future;
import java.util.Arrays;

public final class UpdaterInstrumentation extends Instrumentation {
    private Bundle arguments;
    @Override public void onCreate(Bundle arguments) { this.arguments=arguments; start(); }
    private static final class Interrupted extends RuntimeException { }
    private static void require(boolean value,String message) { SafGame.check(value,"TEST: "+message); }
    @Override public void onStart() {
        JSONObject report=new JSONObject(); Bundle result=new Bundle();
        try {
            String mode=arguments.getString("mode","ui-check"); long started=System.nanoTime();
            if(mode.equals("recovery")) recovery(report);
            else if(mode.equals("delivery-audit")) audit(report);
            else ui(mode,report);
            report.put("ok",true).put("elapsed_ms",(System.nanoTime()-started)/1000000); result.putString("report",report.toString()); finish(Activity.RESULT_OK,result);
        } catch(Throwable failure) { try{report.put("ok",false).put("error",failure.toString());}catch(Exception ignored){} result.putString("report",report.toString()); finish(Activity.RESULT_CANCELED,result); }
    }
    private void preserved(android.content.Context context,Uri tree,String path,JSONObject hashes)throws Exception {
        Uri children=DocumentsContract.buildChildDocumentsUriUsingTree(tree,path);
        try(android.database.Cursor cursor=context.getContentResolver().query(children,new String[]{DocumentsContract.Document.COLUMN_DOCUMENT_ID,DocumentsContract.Document.COLUMN_MIME_TYPE,DocumentsContract.Document.COLUMN_DISPLAY_NAME},null,null,null)){
            require(cursor!=null,"preserved folder listing");
            while(cursor.moveToNext()){
                String id=cursor.getString(0);
                if(cursor.getString(1).equals(DocumentsContract.Document.MIME_TYPE_DIR))preserved(context,tree,id,hashes);
                else if(!cursor.getString(2).endsWith(".log"))try(java.io.InputStream input=context.getContentResolver().openInputStream(DocumentsContract.buildDocumentUriUsingTree(tree,id))){hashes.put(id,SafGame.hash(input));}
            }
        }
    }
    private void audit(JSONObject report)throws Exception {
        android.content.Context context=getTargetContext();android.content.SharedPreferences prefs=context.getSharedPreferences("MainActivity",0);
        Uri tree=Uri.parse(prefs.getString("gameTree",""));String root=prefs.getString("gameDocumentId","");SafGame game=new SafGame(context.getContentResolver(),tree,root);
        JSONObject installed=UpdateEngine.installed(game);JSONArray files=installed.getJSONArray("files");
        for(int i=0;i<files.length();i++){JSONObject file=files.getJSONObject(i);require(game.hash(file.getString("path")).equals(file.getString("sha256")),"delivered member "+file.getString("path"));}
        JSONObject stock;try(java.io.InputStream input=context.getAssets().open("supported-windows.json")){stock=new JSONObject(new String(SafGame.bytes(input),"UTF-8"));}
        for(java.util.Iterator<String> keys=stock.keys();keys.hasNext();){String name=keys.next();require(game.hash(name.replace('\\','/')).equals(stock.getString(name)),"stock original "+name);}
        String app=context.getPackageManager().getApplicationInfo("com.droiddeck.launcher",0).dataDir;
        String prefix=app+"/files/linuxfs/root/.local/share/Steam/steamapps/compatdata/1966900/pfx";JSONObject hashes=new JSONObject();
        preserved(context,tree,prefix+"/drive_c/users/steamuser/AppData/LocalLow/Flanne",hashes);preserved(context,tree,root+"/BepInEx/config",hashes);
        for(String leaf:new String[]{"user.reg","system.reg","userdef.reg"})try(java.io.InputStream input=context.getContentResolver().openInputStream(DocumentsContract.buildDocumentUriUsingTree(tree,prefix+"/"+leaf))){hashes.put(prefix+"/"+leaf,SafGame.hash(input));}
        require(!new UpdateEngine(context).pending(),"no pending transaction");
        for(String path:new String[]{SafGame.PLUGIN,SafGame.MANIFEST})require(!game.exists(path+".revamped-next")&&!game.exists(path+".revamped-previous"),"no deployment leftovers");
        report.put("version",installed.getString("version")).put("source_commit",installed.getString("source_commit")).put("owned_verified",files.length()).put("stock_verified",stock.length()).put("preserved",hashes).put("detected_root",root).put("tree",tree.toString());
    }
    private void capture(Activity activity,JSONObject report,String key)throws Exception {
        runOnMainSync(()->{try{
            View decor=activity.getWindow().getDecorView();android.graphics.Bitmap bitmap=android.graphics.Bitmap.createBitmap(decor.getWidth(),decor.getHeight(),android.graphics.Bitmap.Config.ARGB_8888);
            decor.draw(new android.graphics.Canvas(bitmap));java.io.ByteArrayOutputStream bytes=new java.io.ByteArrayOutputStream();bitmap.compress(android.graphics.Bitmap.CompressFormat.PNG,100,bytes);bitmap.recycle();
            report.put(key,android.util.Base64.encodeToString(bytes.toByteArray(),android.util.Base64.NO_WRAP));
        }catch(Exception failure){throw new RuntimeException(failure);}});
    }
    private GitHubRelease fixtureRelease(SafGame game,String version,String code,boolean wrongAsset)throws Exception {
        JSONArray rows=UpdateEngine.installed(game).getJSONArray("files"); JSONObject files=new JSONObject();
        for(int i=0;i<rows.length();i++){JSONObject row=rows.getJSONObject(i);files.put(row.getString("path"),row.getString("sha256"));}
        byte[] bytes=code.getBytes("UTF-8");files.put(SafGame.PLUGIN,SafGame.hash(bytes));
        if(wrongAsset)files.put(SafGame.MOD+"Assets/fixture.json",SafGame.hash("different-assets".getBytes("UTF-8")));
        return new GitHubRelease(version,"synthetic recovery fixture","2222222222222222222222222222222222222222","fixture",new JSONObject().put("files",files),bytes);
    }
    private void recovery(JSONObject report)throws Exception {
        String name=arguments.getString("fixture_name");require(name!=null,"prepared fixture required");
        // Starting instrumentation force-stops its target and revokes temporary
        // URI grants. Prepare and grant the isolated fixture after that event.
        java.util.concurrent.CountDownLatch prepared=new java.util.concurrent.CountDownLatch(1); Bundle preparation=new Bundle();
        android.os.ResultReceiver receiver=new android.os.ResultReceiver(null){@Override protected void onReceiveResult(int code,Bundle data){preparation.putBoolean("ok",code==1);preparation.putAll(data);prepared.countDown();}};
        Intent prepare=new Intent().setClassName("org.darkaxt.revamped.updater.tests",PrepareFixture.class.getName()).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK).putExtra("fixture_name",name).putExtra("receiver",receiver);
        getContext().startActivity(prepare);prepared.await();require(preparation.getBoolean("ok"),"fixture prepared: "+preparation.getString("error"));
        SafGame game=new SafGame(getTargetContext().getContentResolver(),DocumentsContract.buildTreeDocumentUri(FixtureProvider.AUTHORITY,name));
        require(GameLocator.detect(getTargetContext(),game.tree).size()==1,"a directly selected game folder is detected");
        UpdateEngine engine=new UpdateEngine(getTargetContext());require(!engine.pending(),"preserve an existing deployment journal");
        String marker=game.hash("saves-marker.json"),stock=game.hash("MinutesTillDawn.exe");
        GitHubRelease first=fixtureRelease(game,"1.1.3","next-code",false);
        try { engine.deploy(game,first,message->{if(message.equals("Installing verified plugin…"))throw new Interrupted();});throw new AssertionError("Expected staged interruption"); }catch(Interrupted expected){}
        require(engine.pending(),"journal records interrupted staging");game.putNew(SafGame.PLUGIN+".revamped-next",Arrays.copyOf(first.plugin,2));
        MainActivity interrupted=(MainActivity)startActivitySync(new Intent(getTargetContext(),MainActivity.class).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK));await(interrupted);
        capture(interrupted,report,"interrupted_ui_png");runOnMainSync(()->interrupted.finish());
        engine.finish(message->{});require(UpdateEngine.installed(game).getString("version").equals("1.1.3"),"partial staged write recovers to the recorded payload");
        GitHubRelease second=fixtureRelease(game,"1.1.4","next-next-code",false);
        try { engine.deploy(game,second,message->{if(message.equals("Updating installation record…"))throw new Interrupted();});throw new AssertionError("Expected manifest-boundary interruption"); }catch(Interrupted expected){}
        require(engine.pending()&&game.hash(SafGame.PLUGIN).equals(SafGame.hash(second.plugin))&&UpdateEngine.installed(game).getString("version").equals("1.1.3"),"real plugin activation precedes pending manifest commit");
        engine.finish(message->{});require(UpdateEngine.installed(game).getString("version").equals("1.1.4"),"manifest-boundary interruption finishes deterministically");
        String before=game.hash(SafGame.MANIFEST);
        try { engine.deploy(game,fixtureRelease(game,"1.1.5","unsupported-code",true),message->{});throw new AssertionError("Expected asset requirement"); }catch(IllegalStateException expected){require(expected.getMessage().contains("prepared assets"),"concrete asset incompatibility reported");}
        require(!engine.pending()&&game.hash(SafGame.MANIFEST).equals(before),"unsupported assets cause no deployment");
        require(game.hash("saves-marker.json").equals(marker)&&game.hash("MinutesTillDawn.exe").equals(stock),"unowned save/stock markers preserved");
        SafGame delivered=new SafGame(getTargetContext().getContentResolver(),game.tree);
        for(String path:new String[]{SafGame.PLUGIN,SafGame.MANIFEST})require(!delivered.exists(path+".revamped-next")&&!delivered.exists(path+".revamped-previous"),"no completed transaction leftovers");
        report.put("fixture_only",true).put("recovery_after_partial_write",true).put("recovery_after_plugin_activation",true).put("assets_rejected_before_write",true).put("unowned_preserved",true);
    }
    private void click(MainActivity activity,int id) {
        runOnMainSync(()->{Button button=activity.findViewById(id);require(button.isShown()&&button.isEnabled(),"native action enabled "+id);button.performClick();});
    }
    private void await(MainActivity activity)throws Exception { Future<?> running=activity.running;if(running!=null)running.get();waitForIdleSync(); }
    private void ui(String mode,JSONObject report)throws Exception {
        MainActivity activity=(MainActivity)startActivitySync(new Intent(getTargetContext(),MainActivity.class).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK));await(activity);
        if(mode.equals("ui-check")||mode.equals("ui-deploy")||mode.equals("ui-compact")){click(activity,MainActivity.CHECK_BUTTON);await(activity);}
        if(mode.equals("ui-deploy")){click(activity,MainActivity.DEPLOY_BUTTON);await(activity);}
        if(mode.equals("ui-compact"))runOnMainSync(()->activity.getWindow().setLayout((int)(390*activity.getResources().getDisplayMetrics().density),WindowManager.LayoutParams.MATCH_PARENT));
        waitForIdleSync();
        runOnMainSync(()->{
            try {
                String status=((TextView)activity.findViewById(MainActivity.STATUS)).getText().toString();
                String installed=((TextView)activity.findViewById(MainActivity.INSTALLED)).getText().toString();
                String available=((TextView)activity.findViewById(MainActivity.AVAILABLE)).getText().toString();
                if(mode.equals("ui-check"))require(status.contains("Package verified")&&available.equals("1.1.3"),"real GitHub fetch and package verification");
                if(mode.equals("ui-deploy"))require(status.contains("Update verified")&&installed.equals("1.1.3"),"real native client deploy and version read-back");
                if(mode.equals("ui-inspect"))require(status.contains("Game connected")&&installed.equals("1.1.3"),"remembered folder grant and restored status");
                JSONArray nodes=new JSONArray();dump(activity.getWindow().getDecorView(),nodes);
                report.put("mode",mode).put("installed",installed).put("available",available).put("status",status).put("native_view_tree",nodes).put("density",activity.getResources().getDisplayMetrics().density);
            } catch(Exception failure){throw new RuntimeException(failure);}
        });
        capture(activity,report,"native_ui_png");
    }
    private void dump(View view,JSONArray nodes)throws Exception {
        if(view.isShown()&&(view instanceof TextView||view.getId()==MainActivity.DEPLOY_BUTTON)){
            int[] point=new int[2];view.getLocationOnScreen(point);
            JSONObject node=new JSONObject().put("id",view.getId()).put("type",view.getClass().getSimpleName()).put("enabled",view.isEnabled()).put("x",point[0]).put("y",point[1]).put("width",view.getWidth()).put("height",view.getHeight());
            if(view instanceof TextView)node.put("text",((TextView)view).getText()).put("text_size_px",((TextView)view).getTextSize());nodes.put(node);
        }
        if(view instanceof ViewGroup){ViewGroup group=(ViewGroup)view;for(int i=0;i<group.getChildCount();i++)dump(group.getChildAt(i),nodes);}
    }
}
