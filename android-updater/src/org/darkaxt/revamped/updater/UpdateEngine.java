package org.darkaxt.revamped.updater;

import android.content.Context;
import android.net.Uri;
import android.util.AtomicFile;
import org.json.*;
import java.io.*;
import java.util.*;

/** A single two-file deployment, with an app-private journal and provider-side originals. */
final class UpdateEngine {
    private final Context context;
    private final File journal, plugin, manifest;
    UpdateEngine(Context context) {
        this.context = context;
        journal = new File(context.getFilesDir(), "update-transaction.json");
        plugin = new File(context.getFilesDir(), "verified-plugin.dll");
        manifest = new File(context.getFilesDir(), "verified-manifest.json");
    }
    boolean pending() { return journal.exists(); }
    private static JSONObject json(byte[] bytes) throws Exception { return new JSONObject(new String(bytes, "UTF-8")); }
    private static byte[] read(File file) throws Exception { try (InputStream stream = new FileInputStream(file)) { return SafGame.bytes(stream); } }
    private static void write(File file, byte[] bytes) throws Exception {
        AtomicFile atomic = new AtomicFile(file); FileOutputStream stream = atomic.startWrite();
        try { stream.write(bytes); atomic.finishWrite(stream); } catch (Exception failure) { atomic.failWrite(stream); throw failure; }
    }

    static JSONObject installed(SafGame game) throws Exception {
        JSONObject data = json(game.read(SafGame.MANIFEST));
        SafGame.check(data.getString("product").equals("20MTD-Revamped"), "Choose an existing Revamped game installation.");
        GitHubRelease.compareVersions(data.getString("version"), "0.0.0");
        JSONArray rows = data.getJSONArray("files"); Set<String> seen = new HashSet<>();
        for (int i = 0; i < rows.length(); i++) {
            JSONObject row = rows.getJSONObject(i); String name = SafGame.safe(row.getString("path"));
            boolean allowed = name.equals("winhttp.dll") || name.equals("doorstop_config.ini") || name.equals(SafGame.PLUGIN) || name.startsWith("BepInEx/core/") || name.startsWith(SafGame.MOD + "Assets/");
            SafGame.check(allowed && seen.add(name) && row.get("owned") instanceof Boolean && row.getString("sha256").matches("[a-f0-9]{64}"), "Invalid installed ownership manifest.");
        }
        return data;
    }

    void validateGame(SafGame game, GitHubRelease.Progress progress) throws Exception {
        installed(game);
        JSONObject stock;
        try (InputStream stream = context.getAssets().open("supported-windows.json")) { stock = json(SafGame.bytes(stream)); }
        int count = 0;
        for (Iterator<String> it = stock.keys(); it.hasNext();) {
            String key = it.next(), name = key.replace('\\', '/');
            SafGame.check(game.hash(name).equals(stock.getString(key)), "Unsupported or modified Windows game: " + name);
            if (++count % 20 == 0) progress.show("Checking Windows installation " + count + " / " + stock.length());
        }
    }

    void deploy(SafGame game, GitHubRelease release, GitHubRelease.Progress progress) throws Exception {
        SafGame.check(!pending(), "Finish the interrupted update first.");
        // Directory listings belong to this transaction. A prior finish used
        // its own provider view and may have removed staging entries.
        game = new SafGame(context.getContentResolver(), game.tree, game.rootId);
        JSONObject before = installed(game); byte[] beforeBytes = game.read(SafGame.MANIFEST);
        SafGame.check(GitHubRelease.compareVersions(release.version, before.getString("version")) >= 0, "The installed game is newer than this release. It was preserved.");
        JSONObject expected = release.update.getJSONObject("files"); JSONArray rows = before.getJSONArray("files");
        SafGame.check(expected.length() == rows.length(), "This release changes prepared assets or loader files. Prepare it with the Windows installer first.");
        int pluginRow = -1;
        for (int i = 0; i < rows.length(); i++) {
            JSONObject row = rows.getJSONObject(i); String name = row.getString("path");
            SafGame.check(expected.has(name), "This release changes the installed file set. Windows preparation is required.");
            SafGame.check(game.hash(name).equals(row.getString("sha256")), "Installed file changed; preserved: " + name);
            if (name.equals(SafGame.PLUGIN)) { pluginRow = i; SafGame.check(row.getBoolean("owned"), "The existing plugin belongs to another installation."); }
            else SafGame.check(expected.getString(name).equals(row.getString("sha256")), "New prepared assets or loader files are required. Use the Windows installer before updating this phone.");
            if (i % 40 == 0) progress.show("Verifying installed mod " + (i + 1) + " / " + rows.length());
        }
        SafGame.check(pluginRow >= 0, "The installed plugin is not recorded.");
        String oldPlugin = rows.getJSONObject(pluginRow).getString("sha256");
        JSONObject after = new JSONObject(before.toString());
        JSONObject reset = SaveTransition.needed(before,release) ? SaveTransition.plan(context,game) : null;
        if (reset != null) after.put("rune_save_epoch",SaveTransition.EPOCH);
        after.put("version", release.version); after.put("source_commit", release.source);
        after.getJSONArray("files").getJSONObject(pluginRow).put("sha256", SafGame.hash(release.plugin));
        byte[] afterBytes = (after.toString(2) + "\n").getBytes("UTF-8");
        for (String path : new String[]{SafGame.PLUGIN, SafGame.MANIFEST}) {
            SafGame.check(!game.exists(path + ".revamped-next") && !game.exists(path + ".revamped-previous"), "An unrecorded update staging file exists; it was preserved.");
        }
        write(plugin, release.plugin); write(manifest, afterBytes);
        JSONObject record = new JSONObject().put("tree", game.tree.toString()).put("root_id", game.rootId).put("version", release.version).put("source_commit", release.source)
            .put("old_plugin", oldPlugin).put("new_plugin", SafGame.hash(release.plugin)).put("old_manifest", SafGame.hash(beforeBytes)).put("new_manifest", SafGame.hash(afterBytes)).put("files", expected);
        if(reset!=null)record.put("save_reset",reset);
        write(journal, record.toString().getBytes("UTF-8"));
        finish(progress);
    }

    private void activate(SafGame game, String path, String oldHash, String newHash, byte[] bytes) throws Exception {
        SafGame.check(SafGame.hash(bytes).equals(newHash), "Private update payload changed; staging was preserved.");
        String next = path + ".revamped-next", previous = path + ".revamped-previous";
        if (game.exists(path) && game.hash(path).equals(newHash)) {
            if (game.exists(next)) game.deleteExpected(next, newHash);
            return;
        }
        if (game.exists(path)) {
            SafGame.check(game.hash(path).equals(oldHash) && !game.exists(previous), "The installed file changed during deployment; originals were preserved.");
        } else SafGame.check(game.exists(previous) && game.hash(previous).equals(oldHash), "An interrupted original is missing; staging was preserved.");
        if (game.exists(next) && !game.hash(next).equals(newHash)) {
            // The journal was committed after proving this name absent. A
            // killed write can leave an exact prefix of its recorded payload.
            // Preserve anything that cannot be attributed to that write.
            byte[] partial = game.read(next);
            SafGame.check(partial.length < bytes.length && Arrays.equals(partial, Arrays.copyOf(bytes, partial.length)), "An interrupted staging file changed; it was preserved.");
            game.deleteExpected(next, SafGame.hash(partial));
        }
        if (!game.exists(next)) game.putNew(next, bytes);
        if (game.exists(path)) game.rename(path, previous);
        game.rename(next, path);
        SafGame.check(game.hash(path).equals(newHash), "Activated file verification failed; original retained for recovery.");
    }

    void finish(GitHubRelease.Progress progress) throws Exception {
        SafGame.check(pending(), "There is no interrupted update.");
        JSONObject record = json(read(journal)); SafGame game = new SafGame(context.getContentResolver(), Uri.parse(record.getString("tree")), record.getString("root_id"));
        // The exact deletion plan was committed before any reset or activation.
        // Missing recorded files mean a completed deletion after interruption.
        SaveTransition.finish(context,game,record.optJSONObject("save_reset"),progress);
        progress.show("Installing verified plugin…");
        activate(game, SafGame.PLUGIN, record.getString("old_plugin"), record.getString("new_plugin"), read(plugin));
        progress.show("Updating installation record…");
        activate(game, SafGame.MANIFEST, record.getString("old_manifest"), record.getString("new_manifest"), read(manifest));
        JSONObject files = record.getJSONObject("files"); int count = 0;
        for (Iterator<String> it = files.keys(); it.hasNext();) {
            String name = it.next(); SafGame.check(game.hash(name).equals(files.getString(name)), "Delivered file verification failed: " + name);
            if (++count % 40 == 0) progress.show("Verifying delivered mod " + count + " / " + files.length());
        }
        JSONObject installed = installed(game);
        SafGame.check(installed.getString("version").equals(record.getString("version")) && installed.getString("source_commit").equals(record.getString("source_commit")), "Installed version read-back failed.");
        if(record.has("save_reset"))SafGame.check(installed.getInt("rune_save_epoch")==SaveTransition.EPOCH,"Save transition marker read-back failed.");
        game.deleteExpected(SafGame.PLUGIN + ".revamped-previous", record.getString("old_plugin"));
        game.deleteExpected(SafGame.MANIFEST + ".revamped-previous", record.getString("old_manifest"));
        JSONObject receipt = new JSONObject().put("version", record.getString("version")).put("source_commit", record.getString("source_commit"))
            .put("plugin_sha256", record.getString("new_plugin")).put("verified_files", count).put("completed_at", System.currentTimeMillis());
        write(new File(context.getFilesDir(), "last-update.json"), receipt.toString(2).getBytes("UTF-8"));
        new AtomicFile(journal).delete(); new AtomicFile(plugin).delete(); new AtomicFile(manifest).delete();
    }
}
