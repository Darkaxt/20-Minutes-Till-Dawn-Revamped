package org.darkaxt.revamped.updater;

import android.database.*;
import android.os.*;
import android.provider.*;
import android.provider.DocumentsContract.Document;
import android.provider.DocumentsContract.Root;
import java.io.*;

/** Test APK only. Its private fixture documents are never part of the released client. */
public final class FixtureProvider extends DocumentsProvider {
    static final String AUTHORITY = "org.darkaxt.revamped.updater.tests.documents";
    @Override public boolean onCreate() { return true; }
    private File file(String id) throws FileNotFoundException {
        File base = new File(getContext().getFilesDir(), "fixtures");
        try {
            if (id.startsWith("/") || id.contains("..") || id.contains("\\")) throw new FileNotFoundException("Invalid fixture ID");
            File result = new File(base, id).getCanonicalFile();
            if (!result.toPath().startsWith(base.getCanonicalFile().toPath())) throw new FileNotFoundException("Outside fixture");
            return result;
        } catch (Exception failure) { throw new FileNotFoundException(failure.toString()); }
    }
    private static String[] columns(String[] requested) { return requested == null ? new String[]{Document.COLUMN_DOCUMENT_ID,Document.COLUMN_DISPLAY_NAME,Document.COLUMN_MIME_TYPE,Document.COLUMN_FLAGS,Document.COLUMN_SIZE} : requested; }
    private void add(MatrixCursor cursor, String id, File file) {
        MatrixCursor.RowBuilder row = cursor.newRow();
        for (String column : cursor.getColumnNames()) {
            Object value = column.equals(Document.COLUMN_DOCUMENT_ID) ? id : column.equals(Document.COLUMN_DISPLAY_NAME) ? file.getName() : column.equals(Document.COLUMN_MIME_TYPE) ? (file.isDirectory() ? Document.MIME_TYPE_DIR : "application/octet-stream") : column.equals(Document.COLUMN_FLAGS) ? (file.isDirectory() ? Document.FLAG_DIR_SUPPORTS_CREATE : Document.FLAG_SUPPORTS_WRITE | Document.FLAG_SUPPORTS_DELETE | Document.FLAG_SUPPORTS_RENAME) : column.equals(Document.COLUMN_SIZE) ? file.length() : null;
            row.add(value);
        }
    }
    @Override public Cursor queryRoots(String[] projection) { return new MatrixCursor(projection == null ? new String[]{Root.COLUMN_ROOT_ID,Root.COLUMN_DOCUMENT_ID,Root.COLUMN_TITLE} : projection); }
    @Override public Cursor queryDocument(String id, String[] projection) throws FileNotFoundException { MatrixCursor cursor = new MatrixCursor(columns(projection)); File file = file(id); if (!file.exists()) throw new FileNotFoundException(id); add(cursor,id,file); return cursor; }
    @Override public Cursor queryChildDocuments(String id, String[] projection, String sort) throws FileNotFoundException { MatrixCursor cursor = new MatrixCursor(columns(projection)); File[] children = file(id).listFiles(); if (children == null) throw new FileNotFoundException(id); for (File child : children) add(cursor,id+"/"+child.getName(),child); return cursor; }
    @Override public ParcelFileDescriptor openDocument(String id, String mode, CancellationSignal signal) throws FileNotFoundException { return ParcelFileDescriptor.open(file(id), ParcelFileDescriptor.parseMode(mode)); }
    @Override public String createDocument(String parent, String mime, String name) throws FileNotFoundException {
        try { if (name.contains("/")) throw new FileNotFoundException("Invalid leaf"); String id = parent+"/"+name; File target = file(id); if (target.exists() || !(mime.equals(Document.MIME_TYPE_DIR) ? target.mkdir() : target.createNewFile())) throw new FileNotFoundException("Fixture create failed"); return id; }
        catch (Exception failure) { throw new FileNotFoundException(failure.toString()); }
    }
    @Override public String renameDocument(String id, String name) throws FileNotFoundException { String target = id.substring(0,id.lastIndexOf('/')+1)+name; if (!file(id).renameTo(file(target))) throw new FileNotFoundException("Fixture rename failed"); return target; }
    @Override public void deleteDocument(String id) throws FileNotFoundException { if (!file(id).delete()) throw new FileNotFoundException("Fixture delete failed"); }
    @Override public boolean isChildDocument(String parent, String child) { try { return file(child).toPath().startsWith(file(parent).toPath()); } catch (FileNotFoundException invalid) { return false; } }
}
