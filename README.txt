Stream Extra
============

A) For users  (Setup file)
--------------------------
1) Run  StreamExtra-Setup.exe
2) Pick language (Arabic / English) and theme (Light / Dark) -> Continue
3) Pick the folder to install into -> Download
   It downloads the engine + the app files from GitHub, then opens the app.
4) From now on, this same exe just opens the app (also a Desktop / Start-menu shortcut is created).
5) When a new version is published, the app shows an "Update" page (Settings -> Updates). Click Update now:
   the new files download into the folder you chose (progress is shown inside the app), the app closes by itself and re-opens. Nothing else.

B) For you (the developer) - ONE file does everything
-----------------------------------------------------
Double-click  Publish_Update.cmd  (same steps for the first upload and for every update):
  1) It asks for the new version (press Enter = next number, e.g. 1.1 -> 1.2)
  2) It writes the version + today's date into app\package.json
  3) It builds release\StreamExtra-Setup.exe
  4) It makes the folder  GitHub_Upload  and opens it + your GitHub upload page
  5) In GitHub_Upload press Ctrl+A, drag everything into the GitHub page, then press "Commit changes"
Everyone who opens the app gets the Update page. No Git needed.

First time only: create a PUBLIC repo on GitHub named StreamExtra (branch: main).
Repo used by Setup + updater:  b9storeb9-coder/StreamExtra  (main)
  -> if your account/repo is different, change it in setup\Setup.cs (Cfg.Repo) and app\main.js (GH_REPO).

In the app:  Settings -> Updates  shows your version, its date, the latest version + its release date,
when it last checked, a "Check for updates" button and an "Update now" button.

Download safety (v1.2): if GitHub is blocked/slow, Setup automatically tries a mirror (jsDelivr) and finally uses a copy of the app
embedded INSIDE StreamExtra-Setup.exe, so the last step no longer fails with the "internet / VPN" message.
The update check also tries several servers (GitHub, GitHub API, jsDelivr). Release notes: add them in app\package.json under "notes" (ar / en).

Updates only replace the  app  folder (small). Electron/ffmpeg are downloaded once at install.
Your Kick keys file (kick-app.json) and your settings in %AppData%\StreamExtra are kept.
Note: GitHub's web upload adds/overwrites files but does not delete files you removed locally.

C) Old local build (no updates)
-------------------------------
Build_StreamExtra.cmd still builds a portable "StreamExtra" folder for testing. The Update page is disabled there.

Your settings and Kick link are stored in %AppData%\StreamExtra (+ a backup in %LocalAppData%\StreamExtra-Backup).
