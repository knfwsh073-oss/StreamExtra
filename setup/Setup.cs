// Stream Extra - Setup / Launcher / Updater  (C# 5 compatible: builds with the csc.exe that ships with Windows)
//  * First run  : wizard  (language + theme -> install folder -> download -> opens the app)
//  * Next runs  : this exe just opens the installed app
//  * --update   : (started by the app) waits for the app to close, downloads the new files from GitHub, reopens the app
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

static class Cfg
{
    public const string Repo = "b9storeb9-coder/StreamExtra";
    public const string Branch = "main";
    public const string AppExe = "StreamExtraApp.exe";      // Electron, renamed
    public const string LauncherExe = "StreamExtra.exe";    // this program, copied into the install folder

    public static string DataDir
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StreamExtra"); }
    }
    public static string File { get { return Path.Combine(DataDir, "install.json"); } }

    public static string Read(string key)
    {
        try
        {
            if (!System.IO.File.Exists(File)) return null;
            string t = System.IO.File.ReadAllText(File, Encoding.UTF8);
            Match m = Regex.Match(t, "\"" + key + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
            return m.Success ? Regex.Unescape(m.Groups[1].Value) : null;
        }
        catch { return null; }
    }
    static string Esc(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\""); }
    public static void Write(string dir, string lang, string theme)
    {
        Directory.CreateDirectory(DataDir);
        string j = "{\n  \"installDir\": \"" + Esc(dir) + "\",\n  \"lang\": \"" + Esc(lang) + "\",\n  \"theme\": \"" + Esc(theme) + "\"\n}\n";
        System.IO.File.WriteAllText(File, j, new UTF8Encoding(false));
    }
}

static class Strings
{
    static Dictionary<string, string[]> T = new Dictionary<string, string[]>();   // key -> { ar, en }
    static Strings()
    {
        Add("title", "Stream Extra - التثبيت", "Stream Extra - Setup");
        Add("p1h", "أهلاً بك في Stream Extra", "Welcome to Stream Extra");
        Add("lang", "اللغة", "Language");
        Add("theme", "المظهر", "Theme");
        Add("ar", "العربية", "Arabic");
        Add("en", "English", "English");
        Add("light", "نهاري ☀", "Light ☀");
        Add("dark", "ليلي ☾", "Dark ☾");
        Add("next", "إكمال", "Continue");
        Add("p2h", "اختر مجلد التحميل", "Choose the install folder");
        Add("p2s", "سيتم تحميل ملفات البرنامج إلى هذا المجلد، وكل التحديثات القادمة ستُحمَّل إليه.", "The app files will be downloaded to this folder, and all future updates will go there too.");
        Add("browse", "استعراض…", "Browse…");
        Add("install", "تحميل", "Download");
        Add("back", "رجوع", "Back");
        Add("pick", "اختر المجلد الذي تريد تحميل البرنامج فيه", "Select the folder to install the app into");
        Add("p3h", "جاري التحميل…", "Downloading…");
        Add("u3h", "جاري تحديث البرنامج…", "Updating the app…");
        Add("s_wait", "بانتظار إغلاق البرنامج…", "Waiting for the app to close…");
        Add("s_electron", "تحميل المحرك (Electron)…", "Downloading the engine (Electron)…");
        Add("s_ffmpeg", "تحميل ffmpeg…", "Downloading ffmpeg…");
        Add("s_ytdlp", "تحميل yt-dlp (اختياري)…", "Downloading yt-dlp (optional)…");
        Add("s_app", "تحميل ملفات البرنامج من GitHub…", "Downloading the app files from GitHub…");
        Add("s_unpack", "فك الضغط…", "Extracting…");
        Add("s_finish", "إنهاء التثبيت…", "Finishing…");
        Add("s_done", "تم! جاري فتح البرنامج…", "Done! Opening the app…");
        Add("err", "حدث خطأ", "Something went wrong");
        Add("retry", "إعادة المحاولة", "Retry");
        Add("close", "إغلاق", "Close");
        Add("noinet", "تعذّر التحميل. تأكد من الإنترنت (جرّب VPN إذا كان الموقع محجوباً) ثم أعد المحاولة.", "Download failed. Check your internet (try a VPN if the site is blocked) and retry.");
        Add("noffmpeg", "تعذّر الحصول على ffmpeg يدعم RTMPS.", "Could not get an ffmpeg build with RTMPS support.");
        Add("badzip", "ملف GitHub لا يحتوي مجلد app. تأكد أن المستودع يحتوي app\\main.js على الفرع main.", "The GitHub archive has no app folder. Make sure the repo has app\\main.js on the main branch.");
    }
    static void Add(string k, string ar, string en) { T[k] = new string[] { ar, en }; }
    public static string Get(string k, string lang)
    {
        string[] v;
        if (!T.TryGetValue(k, out v)) return k;
        return lang == "en" ? v[1] : v[0];
    }
}

class Net
{
    public static void Init()
    {
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)768 | (SecurityProtocolType)192; // TLS 1.2 / 1.1 / 1.0 (3072 = Tls12)
        ServicePointManager.DefaultConnectionLimit = 8;
        ServicePointManager.Expect100Continue = false;
    }

    // Downloads to destFile (via .part). progress: 0..1 (or -1 when size unknown). Returns true on success.
    public static bool Download(string[] urls, string destFile, long minBytes, bool mustBeZip, Action<double> progress, Action<string> log)
    {
        foreach (string url in urls)
        {
            string part = destFile + ".part";
            try
            {
                if (log != null) log("GET " + url);
                try { File.Delete(part); } catch { }
                HttpWebRequest rq = (HttpWebRequest)WebRequest.Create(url);
                rq.UserAgent = "StreamExtra-Setup";
                rq.Timeout = 30000;
                rq.ReadWriteTimeout = 30000;
                rq.AllowAutoRedirect = true;
                using (HttpWebResponse rs = (HttpWebResponse)rq.GetResponse())
                {
                    long total = rs.ContentLength;
                    using (Stream ins = rs.GetResponseStream())
                    using (FileStream fs = new FileStream(part, FileMode.Create, FileAccess.Write))
                    {
                        byte[] buf = new byte[81920];
                        long got = 0; int n;
                        while ((n = ins.Read(buf, 0, buf.Length)) > 0)
                        {
                            fs.Write(buf, 0, n);
                            got += n;
                            if (progress != null) progress(total > 0 ? (double)got / total : -1);
                        }
                    }
                }
                if (new FileInfo(part).Length < minBytes) { if (log != null) log("  file too small, skipping"); File.Delete(part); continue; }
                if (mustBeZip && !IsZip(part)) { if (log != null) log("  corrupt zip, skipping"); File.Delete(part); continue; }
                try { File.Delete(destFile); } catch { }
                File.Move(part, destFile);
                return true;
            }
            catch (Exception ex)
            {
                if (log != null) log("  failed: " + ex.Message);
                try { File.Delete(part); } catch { }
            }
        }
        return false;
    }

    public static bool IsZip(string path)
    {
        try { using (ZipArchive z = ZipFile.OpenRead(path)) { return z.Entries.Count > 0; } }
        catch { return false; }
    }
}

class Installer
{
    public string Dir, Lang, Theme;
    public Action<string, double> Report;      // (status key or text, overall 0..1)
    public Action<string> Log;
    string tmp;

    static string SafeJoin(string root, string rel)
    {
        string full = Path.GetFullPath(Path.Combine(root, rel));
        string r = Path.GetFullPath(root).TrimEnd('\\') + "\\";
        if (!full.StartsWith(r, StringComparison.OrdinalIgnoreCase)) throw new IOException("bad zip path: " + rel);
        return full;
    }

    static void ExtractAll(string zip, string dest, string stripPrefix)   // stripPrefix: only entries under it, with it removed
    {
        using (ZipArchive z = ZipFile.OpenRead(zip))
        {
            foreach (ZipArchiveEntry e in z.Entries)
            {
                string name = e.FullName.Replace('\\', '/');
                if (stripPrefix != null)
                {
                    if (!name.StartsWith(stripPrefix, StringComparison.Ordinal)) continue;
                    name = name.Substring(stripPrefix.Length);
                }
                if (name.Length == 0) continue;
                string target = SafeJoin(dest, name);
                if (name.EndsWith("/")) { Directory.CreateDirectory(target); continue; }
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                e.ExtractToFile(target, true);
            }
        }
    }

    static bool HasRtmps(string exe)
    {
        try
        {
            ProcessStartInfo si = new ProcessStartInfo(exe, "-hide_banner -protocols");
            si.UseShellExecute = false; si.CreateNoWindow = true;
            si.RedirectStandardOutput = true; si.RedirectStandardError = true;
            using (Process p = Process.Start(si))
            {
                string o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                p.WaitForExit(15000);
                return o.IndexOf("rtmps", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }
        catch { return false; }
    }

    double Slice(double a, double b, double f) { return a + (b - a) * (f < 0 ? 0.5 : f); }

    // ---------- full install ----------
    public void Install()
    {
        Directory.CreateDirectory(Dir);
        tmp = Path.Combine(Dir, "_download");
        Directory.CreateDirectory(tmp);
        string res = Path.Combine(Dir, "resources");

        // 1) Electron
        Report("s_electron", 0.0);
        string eZip = null;
        string[] versions = new string[] { "33.4.11", "33.3.2", "32.3.3" };
        foreach (string v in versions)
        {
            string f = "electron-v" + v + "-win32-x64.zip";
            string o = Path.Combine(tmp, f);
            string[] urls = new string[] {
                "https://cdn.npmmirror.com/binaries/electron/v" + v + "/" + f,
                "https://github.com/electron/electron/releases/download/v" + v + "/" + f,
                "https://registry.npmmirror.com/-/binary/electron/v" + v + "/" + f };
            if (Net.Download(urls, o, 50L * 1024 * 1024, true, delegate(double p) { Report("s_electron", Slice(0.0, 0.50, p)); }, Log)) { eZip = o; break; }
        }
        if (eZip == null) throw new Exception(Strings.Get("noinet", Lang));
        Report("s_unpack", 0.50);
        ExtractAll(eZip, Dir, null);
        string ee = Path.Combine(Dir, "electron.exe");
        string ae = Path.Combine(Dir, Cfg.AppExe);
        if (File.Exists(ae)) File.Delete(ae);
        File.Move(ee, ae);
        try { File.Delete(Path.Combine(res, "default_app.asar")); } catch { }
        Directory.CreateDirectory(res);

        // 2) ffmpeg (must support RTMPS)
        Report("s_ffmpeg", 0.52);
        string ffTarget = Path.Combine(res, "ffmpeg.exe");
        bool ffOk = false;
        string gzFile = Path.Combine(tmp, "ffmpeg-win32-x64.gz");
        if (Net.Download(new string[] {
                "https://cdn.npmmirror.com/binaries/ffmpeg-static/b6.0/ffmpeg-win32-x64.gz",
                "https://github.com/eugeneware/ffmpeg-static/releases/download/b6.0/ffmpeg-win32-x64.gz" },
                gzFile, 8L * 1024 * 1024, false, delegate(double p) { Report("s_ffmpeg", Slice(0.52, 0.72, p)); }, Log))
        {
            try
            {
                using (FileStream i = File.OpenRead(gzFile))
                using (GZipStream g = new GZipStream(i, CompressionMode.Decompress))
                using (FileStream o = File.Create(ffTarget)) { g.CopyTo(o); }
                ffOk = HasRtmps(ffTarget);
            }
            catch (Exception ex) { Log("gz extract failed: " + ex.Message); }
        }
        if (!ffOk)
        {
            string[][] zips = new string[][] {
                new string[] { "ffmpeg-release-essentials.zip", "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip", "https://github.com/GyanD/codexffmpeg/releases/download/7.1/ffmpeg-7.1-essentials_build.zip" },
                new string[] { "ffmpeg-master-latest-win64-gpl.zip", "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip" } };
            foreach (string[] z in zips)
            {
                string zf = Path.Combine(tmp, z[0]);
                string[] urls = new string[z.Length - 1];
                Array.Copy(z, 1, urls, 0, urls.Length);
                if (!Net.Download(urls, zf, 40L * 1024 * 1024, true, delegate(double p) { Report("s_ffmpeg", Slice(0.52, 0.72, p)); }, Log)) continue;
                try
                {
                    using (ZipArchive za = ZipFile.OpenRead(zf))
                    {
                        foreach (ZipArchiveEntry e in za.Entries)
                        {
                            if (e.FullName.EndsWith("/ffmpeg.exe", StringComparison.OrdinalIgnoreCase) || e.FullName.Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase))
                            { e.ExtractToFile(ffTarget, true); break; }
                        }
                    }
                    if (HasRtmps(ffTarget)) { ffOk = true; break; }
                }
                catch (Exception ex) { Log("zip extract failed: " + ex.Message); }
            }
        }
        if (!ffOk) throw new Exception(Strings.Get("noffmpeg", Lang));

        // 3) yt-dlp (optional)
        Report("s_ytdlp", 0.73);
        string yt = Path.Combine(tmp, "yt-dlp.exe");
        if (Net.Download(new string[] { "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe" }, yt, 5L * 1024 * 1024, false,
                delegate(double p) { Report("s_ytdlp", Slice(0.73, 0.85, p)); }, Log))
        { try { File.Copy(yt, Path.Combine(res, "yt-dlp.exe"), true); } catch { } }

        // 4) app files from GitHub
        InstallAppFiles(0.86, 0.95);

        // 5) finish: launcher copy, icon, shortcut, config
        Report("s_finish", 0.96);
        string self = Assembly.GetExecutingAssembly().Location;
        string launcher = Path.Combine(Dir, Cfg.LauncherExe);
        if (!string.Equals(Path.GetFullPath(self), Path.GetFullPath(launcher), StringComparison.OrdinalIgnoreCase)) File.Copy(self, launcher, true);
        try { File.Copy(Path.Combine(res, "app", "assets", "icon.ico"), Path.Combine(Dir, "icon.ico"), true); } catch { }
        Cfg.Write(Dir, Lang, Theme);
        MakeShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Stream Extra.lnk"), launcher, Dir);
        MakeShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Stream Extra.lnk"), launcher, Dir);
        try { Directory.Delete(tmp, true); } catch { }
        Report("s_done", 1.0);
    }

    // ---------- update only the app code ----------
    public void Update()
    {
        tmp = Path.Combine(Dir, "_download");
        Directory.CreateDirectory(tmp);
        InstallAppFiles(0.05, 0.95);
        Cfg.Write(Dir, Lang, Theme);
        try { Directory.Delete(tmp, true); } catch { }
        Report("s_done", 1.0);
    }

    void InstallAppFiles(double a, double b)
    {
        Report("s_app", a);
        string zf = Path.Combine(tmp, "app-main.zip");
        string[] urls = new string[] {
            "https://codeload.github.com/" + Cfg.Repo + "/zip/refs/heads/" + Cfg.Branch,
            "https://github.com/" + Cfg.Repo + "/archive/refs/heads/" + Cfg.Branch + ".zip" };
        if (!Net.Download(urls, zf, 10L * 1024, true, delegate(double p) { Report("s_app", Slice(a, b - 0.05, p)); }, Log))
            throw new Exception(Strings.Get("noinet", Lang));

        // find "<root>/app/main.js" to learn the prefix
        string prefix = null;
        using (ZipArchive z = ZipFile.OpenRead(zf))
        {
            foreach (ZipArchiveEntry e in z.Entries)
            {
                string n = e.FullName.Replace('\\', '/');
                if (n.EndsWith("/app/main.js", StringComparison.Ordinal) || n == "app/main.js")
                { prefix = n.Substring(0, n.Length - "main.js".Length); break; }
            }
        }
        if (prefix == null) throw new Exception(Strings.Get("badzip", Lang));

        Report("s_unpack", b - 0.04);
        string res = Path.Combine(Dir, "resources");
        string appNew = Path.Combine(res, "app_new");
        string appDir = Path.Combine(res, "app");
        string appOld = Path.Combine(res, "app_old");
        try { if (Directory.Exists(appNew)) Directory.Delete(appNew, true); } catch { }
        try { if (Directory.Exists(appOld)) Directory.Delete(appOld, true); } catch { }
        Directory.CreateDirectory(appNew);
        ExtractAll(zf, appNew, prefix);

        // keep the user's local Kick app keys (kick-app.json) if they exist
        string oldKick = Path.Combine(appDir, "kick-app.json");
        if (File.Exists(oldKick)) { try { File.Copy(oldKick, Path.Combine(appNew, "kick-app.json"), true); } catch { } }

        // swap folders (rollback on failure)
        if (Directory.Exists(appDir)) Directory.Move(appDir, appOld);
        try { Directory.Move(appNew, appDir); }
        catch
        {
            try { if (Directory.Exists(appOld) && !Directory.Exists(appDir)) Directory.Move(appOld, appDir); } catch { }
            throw;
        }
        try { if (Directory.Exists(appOld)) Directory.Delete(appOld, true); } catch { }
    }

    static void MakeShortcut(string lnkPath, string target, string workDir)
    {
        try
        {
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            object sh = Activator.CreateInstance(t);
            object lnk = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, sh, new object[] { lnkPath });
            Type lt = lnk.GetType();
            lt.InvokeMember("TargetPath", BindingFlags.SetProperty, null, lnk, new object[] { target });
            lt.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, lnk, new object[] { workDir });
            lt.InvokeMember("IconLocation", BindingFlags.SetProperty, null, lnk, new object[] { Path.Combine(workDir, "icon.ico") });
            lt.InvokeMember("Save", BindingFlags.InvokeMethod, null, lnk, null);
        }
        catch { }
    }
}

class MainForm : Form
{
    string lang, theme, dir;
    bool updateMode; int waitPid;
    Panel p1, p2, p3;
    Label title, sub;
    Label l1h, lLang, lTheme, l2h, l2s, l3h, status, errLbl;
    RadioButton rAr, rEn, rLight, rDark;
    TextBox tbDir;
    Button bNext, bBrowse, bInstall, bBack, bRetry, bClose;
    ProgressBar bar;
    Thread worker;
    bool finished;

    public MainForm(string initLang, string initTheme, string installDir, bool update, int pid)
    {
        lang = initLang == "en" ? "en" : "ar";
        theme = initTheme == "light" ? "light" : "dark";
        dir = installDir;
        updateMode = update; waitPid = pid;

        Font = new Font("Segoe UI", 10f);
        ClientSize = new Size(560, 380);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        try { Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location); } catch { }

        // page 1
        p1 = NewPanel(); l1h = Head(p1); lLang = Lbl(p1, 36, 90); lTheme = Lbl(p1, 36, 190);
        rAr = Choice(p1, 36, 120); rEn = Choice(p1, 200, 120);
        rLight = Choice(p1, 36, 220); rDark = Choice(p1, 200, 220);
        bNext = Btn(p1, 36, 310, 488);
        rAr.CheckedChanged += delegate { if (rAr.Checked) { lang = "ar"; Apply(); } };
        rEn.CheckedChanged += delegate { if (rEn.Checked) { lang = "en"; Apply(); } };
        rLight.CheckedChanged += delegate { if (rLight.Checked) { theme = "light"; Apply(); } };
        rDark.CheckedChanged += delegate { if (rDark.Checked) { theme = "dark"; Apply(); } };
        bNext.Click += delegate { ShowPage(p2); };

        // page 2
        p2 = NewPanel(); l2h = Head(p2); l2s = Lbl(p2, 36, 84); l2s.Size = new Size(488, 50);
        tbDir = new TextBox(); tbDir.Location = new Point(36, 150); tbDir.Size = new Size(380, 28); tbDir.Text = dir; p2.Controls.Add(tbDir);
        bBrowse = Btn(p2, 424, 148, 100); bBrowse.Height = 32;
        bBack = Btn(p2, 36, 310, 140);
        bInstall = Btn(p2, 184, 310, 340);
        bBrowse.Click += delegate
        {
            using (FolderBrowserDialog fb = new FolderBrowserDialog())
            {
                fb.Description = Strings.Get("pick", lang);
                if (Directory.Exists(tbDir.Text)) fb.SelectedPath = tbDir.Text;
                if (fb.ShowDialog(this) == DialogResult.OK) tbDir.Text = fb.SelectedPath;
            }
        };
        bBack.Click += delegate { ShowPage(p1); };
        bInstall.Click += delegate { StartWork(); };

        // page 3
        p3 = NewPanel(); l3h = Head(p3);
        status = Lbl(p3, 36, 110); status.Size = new Size(488, 50);
        bar = new ProgressBar(); bar.Location = new Point(36, 170); bar.Size = new Size(488, 22); bar.Minimum = 0; bar.Maximum = 1000; bar.Style = ProgressBarStyle.Continuous; p3.Controls.Add(bar);
        errLbl = Lbl(p3, 36, 205); errLbl.Size = new Size(488, 90); errLbl.ForeColor = Color.FromArgb(230, 70, 70);
        bRetry = Btn(p3, 36, 310, 240); bClose = Btn(p3, 284, 310, 240);
        bRetry.Visible = false; bClose.Visible = false;
        bRetry.Click += delegate { bRetry.Visible = false; bClose.Visible = false; errLbl.Text = ""; StartWork(); };
        bClose.Click += delegate { Environment.Exit(1); };

        Controls.Add(p1); Controls.Add(p2); Controls.Add(p3);
        rAr.Checked = lang == "ar"; rEn.Checked = lang == "en";
        rLight.Checked = theme == "light"; rDark.Checked = theme == "dark";
        Apply();
        FormClosing += delegate(object s, FormClosingEventArgs e) { if (worker != null && worker.IsAlive && !finished) Environment.Exit(1); };
        Shown += delegate { if (updateMode) StartWork(); };
        ShowPage(updateMode ? p3 : p1);
    }

    Panel NewPanel() { Panel p = new Panel(); p.Dock = DockStyle.Fill; p.Visible = false; return p; }
    Label Head(Panel p) { Label l = new Label(); l.AutoSize = false; l.Location = new Point(36, 28); l.Size = new Size(488, 40); l.Font = new Font("Segoe UI", 16f, FontStyle.Bold); p.Controls.Add(l); return l; }
    Label Lbl(Panel p, int x, int y) { Label l = new Label(); l.AutoSize = false; l.Location = new Point(x, y); l.Size = new Size(488, 26); p.Controls.Add(l); return l; }
    RadioButton Choice(Panel p, int x, int y)
    {
        RadioButton r = new RadioButton(); r.Appearance = Appearance.Button; r.FlatStyle = FlatStyle.Flat;
        r.Location = new Point(x, y); r.Size = new Size(150, 44); r.TextAlign = ContentAlignment.MiddleCenter; r.Cursor = Cursors.Hand;
        r.FlatAppearance.BorderSize = 1; r.CheckedChanged += delegate { StyleChoice(r); };
        p.Controls.Add(r); return r;
    }
    Button Btn(Panel p, int x, int y, int w)
    {
        Button b = new Button(); b.Location = new Point(x, y); b.Size = new Size(w, 44); b.FlatStyle = FlatStyle.Flat; b.Cursor = Cursors.Hand;
        b.FlatAppearance.BorderSize = 0; b.Font = new Font("Segoe UI", 11f, FontStyle.Bold); p.Controls.Add(b); return b;
    }

    Color Bg { get { return theme == "light" ? Color.FromArgb(244, 244, 246) : Color.FromArgb(18, 18, 20); } }
    Color Fg { get { return theme == "light" ? Color.FromArgb(24, 24, 27) : Color.FromArgb(242, 242, 244); } }
    Color Panel2 { get { return theme == "light" ? Color.FromArgb(255, 255, 255) : Color.FromArgb(30, 30, 34); } }
    Color Accent { get { return Color.FromArgb(161, 20, 44); } }

    void StyleChoice(RadioButton r)
    {
        r.BackColor = r.Checked ? Accent : Panel2;
        r.ForeColor = r.Checked ? Color.White : Fg;
        r.FlatAppearance.BorderColor = r.Checked ? Accent : (theme == "light" ? Color.FromArgb(210, 210, 216) : Color.FromArgb(60, 60, 66));
    }

    void Apply()
    {
        bool rtl = lang == "ar";
        RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No;
        RightToLeftLayout = rtl;
        BackColor = Bg; ForeColor = Fg;
        foreach (Panel p in new Panel[] { p1, p2, p3 }) { p.BackColor = Bg; p.ForeColor = Fg; p.RightToLeft = RightToLeft; }
        Text = Strings.Get("title", lang);
        l1h.Text = Strings.Get("p1h", lang); lLang.Text = Strings.Get("lang", lang); lTheme.Text = Strings.Get("theme", lang);
        rAr.Text = Strings.Get("ar", lang); rEn.Text = Strings.Get("en", lang); rLight.Text = Strings.Get("light", lang); rDark.Text = Strings.Get("dark", lang);
        bNext.Text = Strings.Get("next", lang);
        l2h.Text = Strings.Get("p2h", lang); l2s.Text = Strings.Get("p2s", lang);
        bBrowse.Text = Strings.Get("browse", lang); bBack.Text = Strings.Get("back", lang); bInstall.Text = Strings.Get("install", lang);
        l3h.Text = Strings.Get(updateMode ? "u3h" : "p3h", lang);
        bRetry.Text = Strings.Get("retry", lang); bClose.Text = Strings.Get("close", lang);
        foreach (Button b in new Button[] { bNext, bInstall }) { b.BackColor = Accent; b.ForeColor = Color.White; }
        foreach (Button b in new Button[] { bBack, bBrowse, bClose, bRetry }) { b.BackColor = Panel2; b.ForeColor = Fg; }
        tbDir.BackColor = Panel2; tbDir.ForeColor = Fg; tbDir.BorderStyle = BorderStyle.FixedSingle;
        tbDir.TextAlign = HorizontalAlignment.Left; tbDir.RightToLeft = RightToLeft.No;
        l1h.ForeColor = Fg; l2h.ForeColor = Fg; l3h.ForeColor = Fg;
        foreach (RadioButton r in new RadioButton[] { rAr, rEn, rLight, rDark }) StyleChoice(r);
    }

    void ShowPage(Panel p) { p1.Visible = p == p1; p2.Visible = p == p2; p3.Visible = p == p3; }

    void SetStatus(string keyOrText, double frac)
    {
        BeginInvoke(new MethodInvoker(delegate
        {
            string s = Strings.Get(keyOrText, lang);
            status.Text = s;
            int v = (int)(frac * 1000); if (v < 0) v = 0; if (v > 1000) v = 1000; bar.Value = v;
        }));
    }

    void Fail(string msg)
    {
        BeginInvoke(new MethodInvoker(delegate
        {
            errLbl.Text = Strings.Get("err", lang) + ":\n" + msg;
            bRetry.Visible = true; bClose.Visible = true;
        }));
    }

    void StartWork()
    {
        string chosen = tbDir.Text.Trim();
        if (!updateMode)
        {
            if (chosen.Length == 0) return;
            try
            {
                chosen = Path.GetFullPath(chosen);
                // if the folder already has other files (and is not a previous install), install into a "StreamExtra" subfolder
                if (Directory.Exists(chosen) && Directory.GetFileSystemEntries(chosen).Length > 0 && !File.Exists(Path.Combine(chosen, Cfg.AppExe)))
                    chosen = Path.Combine(chosen, "StreamExtra");
            }
            catch { MessageBox.Show(this, "Invalid folder", Text); return; }
            dir = chosen;
        }
        ShowPage(p3);
        bar.Value = 0; status.Text = "";
        worker = new Thread(delegate()
        {
            try
            {
                Installer ins = new Installer();
                ins.Dir = dir; ins.Lang = lang; ins.Theme = theme;
                ins.Log = delegate(string s) { };
                ins.Report = delegate(string k, double f) { SetStatus(k, f); };
                if (updateMode)
                {
                    SetStatus("s_wait", 0.0);
                    Launcher.WaitForAppExit(waitPid, dir);
                    ins.Update();
                }
                else ins.Install();
                finished = true;
                Thread.Sleep(600);
                Launcher.StartApp(dir);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                if (updateMode) { try { Launcher.StartApp(dir); } catch { } }   // never leave the user without the app
                Fail(ex.Message);
            }
        });
        worker.IsBackground = true;
        worker.Start();
    }
}

static class Launcher
{
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

    public static void StartApp(string dir)
    {
        string exe = Path.Combine(dir, Cfg.AppExe);
        ProcessStartInfo si = new ProcessStartInfo(exe);
        si.WorkingDirectory = dir;
        si.UseShellExecute = false;
        Process.Start(si);
    }

    public static void WaitForAppExit(int pid, string dir)
    {
        if (pid > 0)
        {
            try { Process p = Process.GetProcessById(pid); p.WaitForExit(30000); } catch { }
        }
        // make sure no leftover process of this install keeps files locked
        string full = Path.GetFullPath(dir).TrimEnd('\\') + "\\";
        foreach (string name in new string[] { "StreamExtraApp", "ffmpeg", "yt-dlp" })
        {
            foreach (Process p in Process.GetProcessesByName(name))
            {
                try
                {
                    string path = p.MainModule.FileName;
                    if (path != null && path.StartsWith(full, StringComparison.OrdinalIgnoreCase)) { p.Kill(); p.WaitForExit(5000); }
                }
                catch { }
            }
        }
        Thread.Sleep(500);
    }

    [STAThread]
    static void Main(string[] args)
    {
        try { SetProcessDPIAware(); } catch { }
        Net.Init();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        bool update = false; int pid = 0;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--update") update = true;
            else if (args[i] == "--pid" && i + 1 < args.Length) int.TryParse(args[i + 1], out pid);
        }

        string dir = Cfg.Read("installDir");
        string lang = Cfg.Read("lang") ?? "ar";
        string theme = Cfg.Read("theme") ?? "dark";
        bool installed = !string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, Cfg.AppExe));

        if (update && installed)
        {
            Application.Run(new MainForm(lang, theme, dir, true, pid));
            return;
        }
        if (installed)
        {
            // this file "became" the app: just open it
            try { StartApp(dir); } catch (Exception ex) { MessageBox.Show(ex.Message, "Stream Extra"); }
            return;
        }
        string def = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "StreamExtra");
        Application.Run(new MainForm(lang, theme, def, false, 0));
    }
}
