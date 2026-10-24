package org.darkaxt.revamped.updater;

import android.app.Activity;
import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;
import android.provider.DocumentsContract;
import org.json.*;
import java.io.*;

public final class PrepareFixture extends Activity {
    private static final String MOD="BepInEx/plugins/20MTDModernization/";
    private static void require(boolean condition,String message) { if(!condition)throw new IllegalStateException(message); }
    private static String hash(byte[] bytes)throws Exception { StringBuilder value=new StringBuilder();for(byte b:java.security.MessageDigest.getInstance("SHA-256").digest(bytes))value.append(String.format(java.util.Locale.ROOT,"%02x",b&255));return value.toString(); }
    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        android.os.ResultReceiver receiver=getIntent().getParcelableExtra("receiver");
        try {
            String name = getIntent().getStringExtra("fixture_name"); require(name != null && name.matches("case-[a-z0-9-]+"),"Explicit fixture name required");
            File root = new File(getFilesDir(), "fixtures/"+name); require(!root.exists(),"Never overwrite an existing fixture");
            JSONObject manifest = new JSONObject().put("product","20MTD-Revamped").put("version","1.1.2").put("source_commit","1111111111111111111111111111111111111111").put("created_directories",new JSONArray());
            JSONArray rows = new JSONArray();
            String[] paths = {MOD+"TwentyMinutes.Modernization.dll",MOD+"Assets/fixture.json"}; String[] data = {"previous-code","retained-assets"};
            for (int i=0;i<paths.length;i++) { byte[] bytes=data[i].getBytes("UTF-8"); write(root,paths[i],bytes); rows.put(new JSONObject().put("path",paths[i]).put("sha256",hash(bytes)).put("owned",true)); }
            manifest.put("files",rows); write(root,MOD+"install-manifest.json",manifest.toString(2).getBytes("UTF-8"));
            write(root,"MinutesTillDawn.exe","stock-marker".getBytes("UTF-8")); write(root,"saves-marker.json","untouched-profile".getBytes("UTF-8"));
            Uri tree=DocumentsContract.buildTreeDocumentUri(FixtureProvider.AUTHORITY,name);
            grantUriPermission("org.darkaxt.revamped.updater",tree,Intent.FLAG_GRANT_READ_URI_PERMISSION|Intent.FLAG_GRANT_WRITE_URI_PERMISSION|Intent.FLAG_GRANT_PREFIX_URI_PERMISSION);
            if(receiver!=null)receiver.send(1,new Bundle());
        } catch (Exception failure) { android.util.Log.e("RevampedFixture","Fixture preparation failed",failure);if(receiver!=null){Bundle error=new Bundle();error.putString("error",failure.toString());receiver.send(0,error);} }
        finish();
    }
    private static void write(File root,String path,byte[] bytes)throws Exception { File file=new File(root,path); file.getParentFile().mkdirs(); try(OutputStream stream=new FileOutputStream(file)){stream.write(bytes);} }
}
