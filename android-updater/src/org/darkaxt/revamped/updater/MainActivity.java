package org.darkaxt.revamped.updater;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.*;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.net.Uri;
import android.os.Bundle;
import android.provider.DocumentsContract;
import android.view.*;
import android.widget.*;
import org.json.JSONObject;
import java.util.concurrent.*;
import java.util.List;

public final class MainActivity extends Activity {
    static final int PICK_FOLDER = 20;
    static final int FOLDER_BUTTON = 101, CHECK_BUTTON = 102, DEPLOY_BUTTON = 103, STATUS = 104, INSTALLED = 105, AVAILABLE = 106;
    private final ExecutorService worker = Executors.newSingleThreadExecutor();
    Future<?> running;
    private UpdateEngine engine;
    private GitHubRelease release;
    private SafGame game;
    private TextView status, installed, available, notes, location, headline, foot;
    private Button choose, check, deploy;
    private ProgressBar progress;
    private boolean busy, updatedHere;
    private String installedVersion;

    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        engine = new UpdateEngine(this);
        buildScreen();
        String tree = getPreferences(0).getString("gameTree", null);
        if (tree != null) {
            String root = getPreferences(0).getString("gameDocumentId", DocumentsContract.getTreeDocumentId(Uri.parse(tree)));
            game = new SafGame(getContentResolver(), Uri.parse(tree), root);
            location.setText("20MinuteTillDawn");
            execute(() -> {
                refreshInstalled();
                progress(engine.pending() ? "The previous update has not finished. Resume its recorded deployment." : "Game connected. Check GitHub for an update.");
            });
        }
        renderButtons();
    }

    private int dp(float value) { return (int)(getResources().getDisplayMetrics().density * value + .5f); }

    private TextView text(LinearLayout parent, String value, float size, int color) {
        TextView view = new TextView(this); view.setText(value); view.setTextSize(size); view.setTextColor(color);
        view.setPadding(0, dp(5), 0, dp(5)); parent.addView(view, new LinearLayout.LayoutParams(-1, -2)); return view;
    }

    private Button button(LinearLayout parent, String value, int id) {
        Button view = new Button(this); view.setId(id); view.setText(value); view.setAllCaps(false); view.setTextSize(17); view.setTextColor(0xffffe3c3);
        view.setBackgroundTintList(android.content.res.ColorStateList.valueOf(0xfffc525f));
        view.setTextColor(0xff242032);
        LinearLayout.LayoutParams layout = new LinearLayout.LayoutParams(-1, dp(56)); layout.topMargin = dp(12);
        parent.addView(view, layout); return view;
    }

    private void buildScreen() {
        int pad = dp(24);
        LinearLayout column = new LinearLayout(this); column.setOrientation(LinearLayout.VERTICAL);
        FrameLayout root = new FrameLayout(this) {
            @Override protected void onMeasure(int widthSpec, int heightSpec) {
                FrameLayout.LayoutParams layout = (FrameLayout.LayoutParams)column.getLayoutParams();
                layout.width = Math.min(MeasureSpec.getSize(widthSpec) - getPaddingLeft() - getPaddingRight(), dp(640));
                super.onMeasure(widthSpec, heightSpec);
            }
        };
        root.setBackgroundColor(0xff242032);
        root.addView(column, new FrameLayout.LayoutParams(-1, -1, Gravity.CENTER_HORIZONTAL));
        root.setOnApplyWindowInsetsListener((view, insets) -> {
            root.setPadding(insets.getSystemWindowInsetLeft(), insets.getSystemWindowInsetTop(), insets.getSystemWindowInsetRight(), insets.getSystemWindowInsetBottom()); return insets;
        });
        if (android.os.Build.VERSION.SDK_INT >= 30) getWindow().setDecorFitsSystemWindows(false);
        LinearLayout header = new LinearLayout(this); header.setGravity(Gravity.CENTER_VERTICAL); header.setPadding(pad, dp(18), pad, dp(12));
        column.addView(header, new LinearLayout.LayoutParams(-1, -2));
        TextView title = new TextView(this); title.setText("Revamped Updater"); title.setTextSize(23); title.setTextColor(0xffffe3c3); title.setTypeface(Typeface.create("sans-serif-medium", 0));
        header.addView(title, new LinearLayout.LayoutParams(0, -2, 1));
        check = new Button(this); check.setId(CHECK_BUTTON); check.setText("↻"); check.setTextSize(27); check.setTextColor(0xffffe3c3); check.setContentDescription("Check GitHub for updates");
        check.setBackgroundTintList(android.content.res.ColorStateList.valueOf(0x00242032));
        header.addView(check, new LinearLayout.LayoutParams(dp(48), dp(48))); check.setOnClickListener(view -> checkGitHub());
        ScrollView scroll = new ScrollView(this); scroll.setFillViewport(true); column.addView(scroll, new LinearLayout.LayoutParams(-1, 0, 1));
        LinearLayout content = new LinearLayout(this); content.setOrientation(LinearLayout.VERTICAL); content.setPadding(pad, dp(12), pad, dp(12)); scroll.addView(content);
        LinearLayout banner = new LinearLayout(this); banner.setOrientation(LinearLayout.VERTICAL); banner.setPadding(dp(18), dp(16), dp(18), dp(16)); banner.setBackground(surface(0xff34453f, 14));
        content.addView(banner, new LinearLayout.LayoutParams(-1, -2));
        text(banner, "20 Minutes Till Dawn", 14, 0xffc0cabb);
        headline = text(banner, "Connect your game", 25, 0xffffe3c3); headline.setTypeface(Typeface.create("sans-serif-medium", 0));
        status = text(banner, "Connect DroidDeck once. The game folder is found automatically.", 15, 0xffe0e2d5); status.setId(STATUS); status.setAccessibilityLiveRegion(View.ACCESSIBILITY_LIVE_REGION_POLITE);
        progress = new ProgressBar(this, null, android.R.attr.progressBarStyleHorizontal); progress.setIndeterminate(true); progress.setVisibility(View.GONE); banner.addView(progress, new LinearLayout.LayoutParams(-1, dp(4)));
        LinearLayout versions = new LinearLayout(this); LinearLayout.LayoutParams versionsLayout = new LinearLayout.LayoutParams(-1, -2); versionsLayout.topMargin = dp(20); content.addView(versions, versionsLayout);
        installed = versionCard(versions, "Installed", INSTALLED, false); available = versionCard(versions, "Latest", AVAILABLE, true);
        TextView changes = text(content, "What’s new", 19, 0xffffe3c3); changes.setPadding(0, dp(28), 0, dp(8)); changes.setTypeface(Typeface.create("sans-serif-medium", 0));
        notes = text(content, "Check GitHub to see the latest changes.", 16, 0xffe3d8cf); notes.setLineSpacing(dp(4), 1); notes.setTextIsSelectable(true);
        LinearLayout footer = new LinearLayout(this); footer.setOrientation(LinearLayout.VERTICAL); footer.setPadding(pad, dp(8), pad, dp(18)); column.addView(footer, new LinearLayout.LayoutParams(-1, -2));
        View divider = new View(this); divider.setBackgroundColor(0xff574d5b); footer.addView(divider, new LinearLayout.LayoutParams(-1, dp(1)));
        LinearLayout folder = new LinearLayout(this); folder.setGravity(Gravity.CENTER_VERTICAL); folder.setPadding(0, dp(10), 0, dp(8)); footer.addView(folder);
        ImageView icon = new ImageView(this);
        try { icon.setImageDrawable(getPackageManager().getApplicationIcon("com.droiddeck.launcher")); } catch (android.content.pm.PackageManager.NameNotFoundException missing) { icon.setImageResource(R.drawable.ic_launcher); }
        folder.addView(icon, new LinearLayout.LayoutParams(dp(32), dp(32)));
        LinearLayout folderText = new LinearLayout(this); folderText.setOrientation(LinearLayout.VERTICAL); folderText.setPadding(dp(12), 0, 0, 0); folder.addView(folderText, new LinearLayout.LayoutParams(0, -2, 1));
        text(folderText, "DroidDeck", 14, 0xffa9b5aa); location = text(folderText, "Not connected", 16, 0xffffe3c3);
        choose = new Button(this); choose.setId(FOLDER_BUTTON); choose.setAllCaps(false); choose.setText("Connect"); choose.setTextSize(15); choose.setTextColor(0xfffc7580); choose.setBackgroundTintList(android.content.res.ColorStateList.valueOf(0x00242032));
        folder.addView(choose, new LinearLayout.LayoutParams(dp(90), dp(52))); choose.setOnClickListener(view -> chooseFolder());
        deploy = button(footer, "Connect DroidDeck", DEPLOY_BUTTON); deploy.setOnClickListener(view -> primaryAction());
        foot = text(footer, "Updates are manual. Your saves stay in place.", 14, 0xffa9b5aa); foot.setGravity(Gravity.CENTER);
        setContentView(root);
    }

    private GradientDrawable surface(int color, int radius) { GradientDrawable shape = new GradientDrawable(); shape.setColor(color); shape.setCornerRadius(dp(radius)); return shape; }
    private TextView versionCard(LinearLayout row, String label, int id, boolean latest) {
        LinearLayout card = new LinearLayout(this); card.setOrientation(LinearLayout.VERTICAL); card.setPadding(dp(16), dp(12), dp(16), dp(14)); card.setBackground(surface(0xff302c40, 12));
        LinearLayout.LayoutParams layout = new LinearLayout.LayoutParams(0, -2, 1); if (latest) layout.leftMargin = dp(12); row.addView(card, layout);
        text(card, label, 14, 0xffa9b5aa); TextView value = text(card, "—", 26, latest ? 0xfffc7580 : 0xffffe3c3); value.setId(id); value.setTypeface(Typeface.create("sans-serif-medium", 0)); return value;
    }

    private void checkGitHub() {
        execute(() -> {
            if (game != null) refreshInstalled();
            GitHubRelease fetched = GitHubRelease.fetch(this, this::progress);
            runOnUiThread(() -> { release = fetched; available.setText(fetched.version); notes.setText(fetched.notes.replace("**", "").replace("`", "").replaceAll("(?m)^- ", "• ")); });
            progress("Package verified. Close the game in DroidDeck before deploying.");
        });
    }
    private void openDroidDeck() {
        Intent intent = getPackageManager().getLaunchIntentForPackage("com.droiddeck.launcher");
        if (intent != null) startActivity(intent); else progress("DroidDeck is not installed.");
    }
    private void primaryAction() {
        if (engine.pending()) execute(() -> { engine.finish(this::progress); updatedHere = true; refreshInstalled(); progress("Update verified. Open DroidDeck to play."); });
        else if (game == null) chooseFolder();
        else if (release == null) checkGitHub();
        else if (installedVersion != null && GitHubRelease.compareVersions(release.version, installedVersion) <= 0) openDroidDeck();
        else execute(() -> { engine.deploy(game, release, this::progress); updatedHere = true; refreshInstalled(); progress("Update verified. Open DroidDeck to play."); });
    }

    private void chooseFolder() {
        Intent intent = new Intent(Intent.ACTION_OPEN_DOCUMENT_TREE);
        intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_GRANT_WRITE_URI_PERMISSION | Intent.FLAG_GRANT_PERSISTABLE_URI_PERMISSION | Intent.FLAG_GRANT_PREFIX_URI_PERMISSION);
        String path;
        try { path = getPackageManager().getApplicationInfo("com.droiddeck.launcher", 0).dataDir; }
        catch (android.content.pm.PackageManager.NameNotFoundException missing) { progress("Install DroidDeck first."); return; }
        intent.putExtra(DocumentsContract.EXTRA_INITIAL_URI, DocumentsContract.buildRootUri("com.droiddeck.launcher.documents", "app"));
        startActivityForResult(intent, PICK_FOLDER);
    }

    @Override protected void onActivityResult(int request, int result, Intent data) {
        super.onActivityResult(request, result, data);
        if (request != PICK_FOLDER || result != RESULT_OK || data == null || data.getData() == null) return;
        Uri tree = data.getData(); int flags = data.getFlags() & (Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_GRANT_WRITE_URI_PERMISSION);
        try { getContentResolver().takePersistableUriPermission(tree, flags); }
        catch (SecurityException failure) { progress("Android did not grant lasting folder access. Connect DroidDeck again."); return; }
        execute(() -> {
            progress("Finding 20 Minutes Till Dawn in DroidDeck…");
            List<SafGame> matches = GameLocator.detect(this, tree);
            if (matches.size() == 1) connectGame(matches.get(0));
            else runOnUiThread(() -> {
                String[] names = new String[matches.size()]; for (int i = 0; i < names.length; i++) names[i] = matches.get(i).rootId;
                new AlertDialog.Builder(this).setTitle("Choose the installed copy").setItems(names, (dialog, index) -> execute(() -> connectGame(matches.get(index)))).show();
            });
        });
    }

    private void connectGame(SafGame selected) throws Exception {
        engine.validateGame(selected, this::progress);
        SafGame.check(getPreferences(0).edit().putString("gameTree", selected.tree.toString()).putString("gameDocumentId", selected.rootId).commit(), "Cannot remember the connected folder.");
        game = selected; runOnUiThread(() -> location.setText("20MinuteTillDawn")); refreshInstalled(); progress("Game found and verified. Check GitHub for an update.");
    }

    private void refreshInstalled() throws Exception {
        JSONObject data = UpdateEngine.installed(game);
        installedVersion = data.getString("version");
        runOnUiThread(() -> installed.setText(installedVersion));
    }

    private void progress(String message) { runOnUiThread(() -> status.setText(message)); }
    private interface Work { void run() throws Exception; }
    private void execute(Work work) {
        if (busy) return;
        busy = true; renderButtons(); getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        running = worker.submit(() -> {
            try { work.run(); }
            catch (Exception failure) { progress(failure.getMessage() == null ? failure.getClass().getSimpleName() : failure.getMessage()); }
            finally { runOnUiThread(() -> { busy = false; renderButtons(); getWindow().clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON); }); }
        });
    }

    private void renderButtons() {
        boolean interrupted = engine.pending();
        choose.setEnabled(!busy && !interrupted); check.setEnabled(!busy && !interrupted);
        choose.setText(game == null ? "Connect" : "Change");
        boolean current = release != null && installedVersion != null && GitHubRelease.compareVersions(release.version, installedVersion) <= 0;
        String action = interrupted ? "Finish interrupted update" : game == null ? "Connect DroidDeck" : release == null ? "Check GitHub" : current ? "Open DroidDeck" : "Install " + release.version;
        deploy.setText(action); deploy.setEnabled(!busy);
        headline.setText(interrupted ? "Finish your update" : game == null ? "Connect your game" : release == null ? "Game connected" : current ? "You’re up to date" : "Ready to update");
        progress.setVisibility(busy ? View.VISIBLE : View.GONE);
        foot.setText(updatedHere ? "Your saves and settings were preserved." : "Close the game in DroidDeck before updating.");
    }
    @Override protected void onDestroy() { worker.shutdown(); super.onDestroy(); }
}
