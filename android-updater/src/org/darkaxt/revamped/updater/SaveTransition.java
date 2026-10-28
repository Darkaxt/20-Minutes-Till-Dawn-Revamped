package org.darkaxt.revamped.updater;

import android.content.Context;
import org.json.*;
import java.util.*;

/** One recorded reset of this game's native/companion saves in its own prefix. */
final class SaveTransition {
    static final int EPOCH = 1;
    private static final String SUFFIX = "/steamapps/common/20MinuteTillDawn";
    private static final String[] NAMES = {"gamedata.json", "gamedata_backup.json", "revamped-achievements.json", "revamped-run.json"};
    private static final String[] EXTENSIONS = {"", ".writing", ".bak", ".backup", ".disabled"};
    static boolean needed(JSONObject installed, GitHubRelease release) {
        return release.update.optInt("save_epoch", 0) > installed.optInt("rune_save_epoch", 0);
    }
    static void checkPath(String path) {
        SafGame.safe(path);
        SafGame.check(path.matches("drive_c/users/[^/]+/AppData/LocalLow/Flanne/MinutesTillDawn/(gamedata\\.json|gamedata_backup\\.json|revamped-achievements\\.json|revamped-run\\.json)(\\.writing|\\.bak|\\.backup|\\.disabled)?"), "Outside the selected game's save ownership: " + path);
    }
    static JSONObject plan(Context context, SafGame game) throws Exception {
        SafGame.check(game.rootId.endsWith(SUFFIX), "Connect the game's Steam library through DroidDeck Files to reset its saves.");
        String library = game.rootId.substring(0, game.rootId.length()-SUFFIX.length());
        SafGame granted = new SafGame(context.getContentResolver(), game.tree);
        SafGame.check(library.equals(granted.rootId) || library.startsWith(granted.rootId+"/"), "Reconnect the DroidDeck folder including the Steam library. The game-folder grant cannot reach its saves.");
        String prefix = library+"/steamapps/compatdata/1966900/pfx";
        SafGame profile = GameLocator.at(context, granted, prefix); JSONArray files = new JSONArray();
        if (profile != null) for (String user : profile.childDirectories("drive_c/users")) {
            String directory = "drive_c/users/"+user+"/AppData/LocalLow/Flanne/MinutesTillDawn/";
            for (String name : NAMES) for (String extension : EXTENSIONS) {
                String path = directory+name+extension; checkPath(path);
                if (profile.exists(path)) files.put(new JSONObject().put("path",path).put("sha256",profile.hash(path)));
            }
        }
        return new JSONObject().put("epoch",EPOCH).put("prefix",profile==null?JSONObject.NULL:profile.rootId).put("files",files);
    }
    static void finish(Context context, SafGame game, JSONObject plan, GitHubRelease.Progress progress) throws Exception {
        if (plan == null) return;
        SafGame.check(plan.getInt("epoch")==EPOCH, "Unsupported save transition.");
        JSONArray files = plan.getJSONArray("files");
        if (plan.isNull("prefix")) { SafGame.check(files.length()==0,"Invalid empty-prefix reset record."); return; }
        SafGame granted = new SafGame(context.getContentResolver(),game.tree);
        SafGame profile = GameLocator.at(context,granted,plan.getString("prefix"));
        SafGame.check(profile!=null,"The recorded game prefix is missing; update state was preserved.");
        for (int i=0;i<files.length();i++) {
            JSONObject file=files.getJSONObject(i);profile.deleteSave(file.getString("path"),file.getString("sha256"));
            progress.show("Resetting old game saves "+(i+1)+" / "+files.length());
        }
        for(int i=0;i<files.length();i++)SafGame.check(!profile.exists(files.getJSONObject(i).getString("path")),"Save reset read-back failed.");
    }
}
