package org.darkaxt.revamped.updater;

import android.content.Context;
import org.json.*;
import javax.net.ssl.HttpsURLConnection;
import java.io.*;
import java.net.URL;
import java.util.*;
import java.util.zip.*;

/** Public, pinned repository releases. No account, token, or executable scripts. */
final class GitHubRelease {
    static final String REPOSITORY = "Darkaxt/20-Minutes-Till-Dawn-Revamped";
    interface Progress { void show(String message); }
    final String version, notes, source, archiveHash;
    final JSONObject update;
    final byte[] plugin;

    GitHubRelease(String version, String notes, String source, String hash, JSONObject update, byte[] plugin) {
        this.version = version; this.notes = notes; this.source = source; archiveHash = hash; this.update = update; this.plugin = plugin;
    }

    private static HttpsURLConnection connect(String url) throws Exception {
        SafGame.check(url.startsWith("https://"), "GitHub returned a non-HTTPS download.");
        HttpsURLConnection connection = (HttpsURLConnection)new URL(url).openConnection();
        connection.setRequestProperty("User-Agent", "Revamped-Updater/0.2.0");
        connection.setRequestProperty("Accept", "application/vnd.github+json");
        connection.setRequestProperty("X-GitHub-Api-Version", "2026-03-10");
        return connection;
    }

    static GitHubRelease fetch(Context context, Progress progress) throws Exception {
        progress.show("Checking GitHub releases…");
        HttpsURLConnection api = connect("https://api.github.com/repos/" + REPOSITORY + "/releases?per_page=30");
        JSONArray releases;
        try {
            SafGame.check(api.getResponseCode() == 200, "GitHub returned HTTP " + api.getResponseCode() + ". Check the connection and try again.");
            try (InputStream stream = api.getInputStream()) { releases = new JSONArray(new String(SafGame.bytes(stream), "UTF-8")); }
        } finally { api.disconnect(); }
        JSONObject selected = null, asset = null;
        for (int i = 0; i < releases.length() && asset == null; i++) {
            JSONObject release = releases.getJSONObject(i);
            if (release.getBoolean("draft") || release.getBoolean("prerelease") || !release.getString("tag_name").matches("v[0-9]+\\.[0-9]+\\.[0-9]+")) continue;
            String name = "20-Minutes-Till-Dawn-Revamped-" + release.getString("tag_name").substring(1) + ".zip";
            JSONArray assets = release.getJSONArray("assets");
            for (int n = 0; n < assets.length(); n++) if (assets.getJSONObject(n).getString("name").equals(name)) {
                selected = release; asset = assets.getJSONObject(n); break;
            }
        }
        SafGame.check(asset != null, "No published Revamped game package was found.");
        String version = selected.getString("tag_name").substring(1);
        String digest = asset.getString("digest");
        SafGame.check(digest.matches("sha256:[a-f0-9]{64}") && asset.getString("state").equals("uploaded"), "GitHub has not supplied a complete package checksum.");
        String url = asset.getString("browser_download_url");
        SafGame.check(url.startsWith("https://github.com/" + REPOSITORY + "/releases/download/v" + version + "/"), "The asset is outside the expected release.");
        File archive = new File(context.getCacheDir(), "github-release.zip");
        progress.show("Downloading Revamped " + version + "…");
        HttpsURLConnection download = connect(url);
        try {
            SafGame.check(download.getResponseCode() == 200, "Package download returned HTTP " + download.getResponseCode());
            try (InputStream stream = download.getInputStream(); OutputStream output = new FileOutputStream(archive)) {
                long count = 0; byte[] buffer = new byte[65536]; int read;
                while ((read = stream.read(buffer)) != -1) {
                    count += read; SafGame.check(count <= asset.getLong("size"), "Download exceeds GitHub's recorded size."); output.write(buffer, 0, read);
                }
                SafGame.check(count == asset.getLong("size"), "The package download is incomplete.");
            }
        } finally { download.disconnect(); }
        try (InputStream stream = new FileInputStream(archive)) {
            SafGame.check(SafGame.hash(stream).equals(digest.substring(7)), "GitHub package checksum mismatch. Nothing was installed.");
        }
        progress.show("Verifying package contents…");
        try (ZipFile zip = new ZipFile(archive)) {
            String prefix = "20-Minutes-Till-Dawn-Revamped-" + version + "/";
            Set<String> names = new HashSet<>();
            for (Enumeration<? extends ZipEntry> entries = zip.entries(); entries.hasMoreElements();) {
                ZipEntry entry = entries.nextElement();
                SafGame.check(!entry.isDirectory() && entry.getName().startsWith(prefix) && names.add(entry.getName()), "Unexpected or duplicate package member.");
                SafGame.safe(entry.getName().substring(prefix.length()));
            }
            JSONObject inventory = json(zip, prefix + "PACKAGE.json");
            JSONObject files = inventory.getJSONObject("files");
            SafGame.check(names.size() == files.length() + 1, "The package file inventory is incomplete.");
            for (Iterator<String> it = files.keys(); it.hasNext();) {
                String name = SafGame.safe(it.next());
                ZipEntry entry = zip.getEntry(prefix + name);
                SafGame.check(entry != null, "Missing package member: " + name);
                try (InputStream stream = zip.getInputStream(entry)) { SafGame.check(SafGame.hash(stream).equals(files.getString(name)), "Changed package member: " + name); }
            }
            JSONObject release = json(zip, prefix + "RELEASE.json");
            JSONObject update = json(zip, prefix + "UPDATE.json");
            SafGame.check(!release.getBoolean("candidate") && (update.getInt("schema") == 1 || update.getInt("schema") == 2) && update.getString("product").equals("20MTD-Revamped"), "Unsupported update contract.");
            if(compareVersions(version,"1.3.0")>=0)SafGame.check(update.getInt("schema")==2&&update.getInt("save_epoch")==SaveTransition.EPOCH&&update.getString("minimum_updater").equals("0.2.0"),"This game requires a different save-transition updater.");
            String source = release.getString("source_commit");
            SafGame.check(version.equals(release.getString("version")) && version.equals(update.getString("version")) && version.equals(inventory.getString("version")), "Package versions disagree.");
            SafGame.check(source.equals(update.getString("source_commit")) && source.equals(inventory.getString("source_commit")), "Package revisions disagree.");
            JSONObject payload = update.getJSONObject("payload");
            SafGame.check(payload.length() == 1 && payload.getString(SafGame.PLUGIN).equals("Mod/TwentyMinutes.Modernization.dll"), "This release needs a newer updater or Windows preparation.");
            byte[] plugin;
            try (InputStream stream = zip.getInputStream(zip.getEntry(prefix + payload.getString(SafGame.PLUGIN)))) { plugin = SafGame.bytes(stream); }
            SafGame.check(SafGame.hash(plugin).equals(release.getString("plugin_sha256")) && SafGame.hash(plugin).equals(update.getJSONObject("files").getString(SafGame.PLUGIN)), "Plugin checksum differs from the release contract.");
            return new GitHubRelease(version, selected.optString("body", ""), source, digest.substring(7), update, plugin);
        }
    }

    private static JSONObject json(ZipFile zip, String name) throws Exception {
        ZipEntry entry = zip.getEntry(name);
        SafGame.check(entry != null, "This release has no Android update contract; use the Windows installer.");
        try (InputStream stream = zip.getInputStream(entry)) { return new JSONObject(new String(SafGame.bytes(stream), "UTF-8")); }
    }

    static int compareVersions(String a, String b) {
        SafGame.check(a.matches("[0-9]+\\.[0-9]+\\.[0-9]+") && b.matches("[0-9]+\\.[0-9]+\\.[0-9]+"), "Unrecognized installed version.");
        String[] left = a.split("\\."), right = b.split("\\.");
        for (int i = 0; i < 3; i++) { int comparison = Integer.compare(Integer.parseInt(left[i]), Integer.parseInt(right[i])); if (comparison != 0) return comparison; }
        return 0;
    }
}
