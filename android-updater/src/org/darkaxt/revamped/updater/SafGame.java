package org.darkaxt.revamped.updater;

import android.content.ContentResolver;
import android.database.Cursor;
import android.net.Uri;
import android.provider.DocumentsContract;
import java.io.*;
import java.security.MessageDigest;
import java.util.*;

/** The single game tree chosen by the user. Mutations are limited to two owned files. */
final class SafGame {
    static final String MOD = "BepInEx/plugins/20MTDModernization/";
    static final String PLUGIN = MOD + "TwentyMinutes.Modernization.dll";
    static final String MANIFEST = MOD + "install-manifest.json";
    final Uri tree;
    final String rootId;
    private final ContentResolver resolver;
    private final Map<String, Map<String, Uri>> directories = new HashMap<>();

    SafGame(ContentResolver resolver, Uri tree) {
        this(resolver, tree, DocumentsContract.getTreeDocumentId(tree));
    }

    SafGame(ContentResolver resolver, Uri tree, String rootId) {
        this.resolver = resolver;
        this.tree = tree;
        this.rootId = rootId;
        check("content".equals(tree.getScheme()) && DocumentsContract.isTreeUri(tree), "Choose the game folder through Android Files.");
    }

    static void check(boolean condition, String message) {
        if (!condition) throw new IllegalStateException(message);
    }

    static String safe(String path) {
        check(!path.isEmpty() && !path.startsWith("/") && !path.contains("\\") && !path.contains(":"), "Invalid installation member.");
        for (String part : path.split("/", -1)) check(!part.isEmpty() && !part.equals(".") && !part.equals(".."), "Invalid installation member.");
        return path;
    }

    private static String parent(String path) { int i = path.lastIndexOf('/'); return i < 0 ? "" : path.substring(0, i); }
    private static String leaf(String path) { return path.substring(path.lastIndexOf('/') + 1); }

    private Uri directory(String path) throws Exception {
        if (path.isEmpty()) return DocumentsContract.buildDocumentUriUsingTree(tree, rootId);
        Uri result = find(path);
        check(result != null, "Missing game directory: " + path);
        return result;
    }

    private Map<String, Uri> children(String path) throws Exception {
        if (directories.containsKey(path)) return directories.get(path);
        Uri directory = directory(path);
        Uri listing = DocumentsContract.buildChildDocumentsUriUsingTree(tree, DocumentsContract.getDocumentId(directory));
        Map<String, Uri> result = new HashMap<>();
        try (Cursor cursor = resolver.query(listing, new String[]{DocumentsContract.Document.COLUMN_DOCUMENT_ID, DocumentsContract.Document.COLUMN_DISPLAY_NAME}, null, null, null)) {
            check(cursor != null, "The folder provider did not return a listing.");
            while (cursor.moveToNext()) {
                String name = cursor.getString(1);
                check(!result.containsKey(name), "Ambiguous document name: " + name);
                result.put(name, DocumentsContract.buildDocumentUriUsingTree(tree, cursor.getString(0)));
            }
        }
        directories.put(path, result);
        return result;
    }

    Uri find(String path) throws Exception {
        safe(path); String parent = parent(path);
        if (!parent.isEmpty() && find(parent) == null) return null;
        return children(parent).get(leaf(path));
    }
    boolean exists(String path) throws Exception { return find(path) != null; }

    InputStream open(String path) throws Exception {
        Uri uri = find(path);
        check(uri != null, "Missing installed file: " + path);
        InputStream stream = resolver.openInputStream(uri);
        check(stream != null, "Cannot read " + path);
        return stream;
    }

    byte[] read(String path) throws Exception { try (InputStream stream = open(path)) { return bytes(stream); } }
    String hash(String path) throws Exception { try (InputStream stream = open(path)) { return hash(stream); } }

    static byte[] bytes(InputStream stream) throws Exception {
        ByteArrayOutputStream output = new ByteArrayOutputStream();
        byte[] buffer = new byte[65536]; int count;
        while ((count = stream.read(buffer)) != -1) output.write(buffer, 0, count);
        return output.toByteArray();
    }

    static String hash(byte[] bytes) throws Exception { return hash(new ByteArrayInputStream(bytes)); }
    static String hash(InputStream stream) throws Exception {
        MessageDigest digest = MessageDigest.getInstance("SHA-256");
        byte[] buffer = new byte[65536]; int count;
        while ((count = stream.read(buffer)) != -1) digest.update(buffer, 0, count);
        StringBuilder result = new StringBuilder();
        for (byte value : digest.digest()) result.append(String.format(Locale.ROOT, "%02x", value & 255));
        return result.toString();
    }

    private static void mutable(String path) {
        safe(path);
        String base = path.replaceFirst("\\.revamped-(next|previous)$", "");
        check(base.equals(PLUGIN) || base.equals(MANIFEST), "Outside updater ownership: " + path);
    }

    void putNew(String path, byte[] content) throws Exception {
        mutable(path); check(!exists(path), "Existing update staging file: " + path);
        Uri uri = DocumentsContract.createDocument(resolver, directory(parent(path)), "application/octet-stream", leaf(path));
        check(uri != null, "Cannot create update staging file.");
        directories.remove(parent(path));
        check(uri.equals(find(path)), "The provider changed the update filename.");
        try (OutputStream stream = resolver.openOutputStream(uri, "wt")) {
            check(stream != null, "Cannot write update staging file."); stream.write(content);
        }
        check(hash(path).equals(hash(content)), "Staged file verification failed.");
    }

    void rename(String from, String to) throws Exception {
        mutable(from); mutable(to);
        check(parent(from).equals(parent(to)) && !exists(to), "Update rename would replace another file.");
        Uri renamed = DocumentsContract.renameDocument(resolver, find(from), leaf(to));
        check(renamed != null, "The provider could not activate the update.");
        directories.remove(parent(from));
        check(renamed.equals(find(to)), "The provider changed the activated filename.");
    }

    void deleteExpected(String path, String expected) throws Exception {
        mutable(path);
        if (!exists(path)) return;
        check(hash(path).equals(expected), "An update staging file changed; it was preserved: " + path);
        check(DocumentsContract.deleteDocument(resolver, find(path)), "Cannot remove completed update staging file.");
        directories.remove(parent(path));
    }
}
