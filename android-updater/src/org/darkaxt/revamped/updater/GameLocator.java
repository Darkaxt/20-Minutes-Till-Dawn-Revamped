package org.darkaxt.revamped.updater;

import android.content.Context;
import android.net.Uri;
import android.provider.DocumentsContract;
import java.util.*;
import java.util.regex.*;

/** Reads only known Steam locations and its library catalogue within the granted tree. */
final class GameLocator {
    static SafGame at(Context context, SafGame granted, String absolute) throws Exception {
        if (absolute.equals(granted.rootId)) return granted;
        if (!absolute.startsWith(granted.rootId + "/")) return null;
        Uri folder = granted.find(absolute.substring(granted.rootId.length() + 1));
        return folder == null ? null : new SafGame(context.getContentResolver(), granted.tree, DocumentsContract.getDocumentId(folder));
    }

    private static boolean game(SafGame candidate) throws Exception {
        return candidate != null && candidate.exists("MinutesTillDawn.exe") && candidate.exists(SafGame.MANIFEST);
    }

    static List<SafGame> detect(Context context, Uri tree) throws Exception {
        SafGame granted = new SafGame(context.getContentResolver(), tree);
        LinkedHashMap<String, SafGame> found = new LinkedHashMap<>();
        if (game(granted)) found.put(granted.rootId, granted);
        if ("com.droiddeck.launcher.documents".equals(tree.getAuthority())) {
            String app = context.getPackageManager().getApplicationInfo("com.droiddeck.launcher", 0).dataDir;
            String linux = app + "/files/linuxfs";
            String steam = linux + "/root/.local/share/Steam";
            List<String> libraries = new ArrayList<>(); libraries.add(steam);
            SafGame steamFolder = at(context, granted, steam);
            if (steamFolder != null && steamFolder.exists("steamapps/libraryfolders.vdf")) {
                String catalogue = new String(steamFolder.read("steamapps/libraryfolders.vdf"), "UTF-8");
                Matcher paths = Pattern.compile("\"path\"\\s*\"([^\"]+)\"").matcher(catalogue);
                while (paths.find()) {
                    String path = paths.group(1).replace("\\\\", "\\");
                    if (path.startsWith("/") && !path.contains("/../") && !path.contains("/./")) libraries.add(linux + path);
                }
            }
            for (String library : libraries) {
                SafGame candidate = at(context, granted, library + "/steamapps/common/20MinuteTillDawn");
                if (game(candidate)) found.put(candidate.rootId, candidate);
            }
        }
        SafGame.check(!found.isEmpty(), "No installed Revamped game was found in this folder. Connect the DroidDeck folder, or select the installed game folder.");
        return new ArrayList<>(found.values());
    }
}
