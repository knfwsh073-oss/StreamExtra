'use strict';
/* صفحة التحديث: عند فتح البرنامج نفحص GitHub، وإذا في إصدار أحدث تظهر صفحة "حدّث البرنامج".
   زر التحديث يقفل البرنامج تلقائياً، يحدّث الملفات في مجلد التثبيت، ثم يفتح البرنامج بالإصدار الجديد. */
(() => {
  const TX = {
    ar: { title: 'يوجد تحديث جديد', sub: 'حدّث البرنامج للحصول على أحدث الميزات والإصلاحات.', cur: 'إصدارك الحالي', nw: 'الإصدار الجديد', btn: 'تحديث الآن', later: 'لاحقاً',
          note: 'سيُغلق البرنامج تلقائياً، يحدّث الملفات، ثم يفتح من جديد.', live: '⚠ أنت في بث مباشر الآن — التحديث سيقطع البث. يُفضّل إنهاء البث أولاً.', wait: 'جاري التحديث…', fail: 'تعذّر بدء التحديث. شغّل StreamExtra.exe يدوياً.' },
    en: { title: 'A new update is available', sub: 'Update the app to get the latest features and fixes.', cur: 'Current version', nw: 'New version', btn: 'Update now', later: 'Later',
          note: 'The app will close automatically, update its files, then reopen.', live: '⚠ You are live right now — updating will cut the stream. Stop the stream first.', wait: 'Updating…', fail: 'Could not start the update. Run StreamExtra.exe manually.' }
  };

  Object.assign(I18N.ar, {
    updTab: 'التحديثات', grpAbout: 'حول البرنامج', pg_updates: 'إصدارك الحالي، وموعد آخر تحديث، وفحص التحديثات.',
    updCur: 'إصدارك الحالي', updCurDate: 'تاريخ إصدارك', updLatest: 'آخر إصدار متوفر', updLatestDate: 'موعد التحديث (تاريخ الإصدار الجديد)',
    updChecked: 'آخر فحص', updNext: 'الفحص التلقائي القادم', updCheckBtn: 'فحص التحديثات', updChecking: 'جاري الفحص…', updRunBtn: 'تحديث الآن',
    updPillOk: 'أنت على آخر إصدار ✓', updPillNew: 'يوجد تحديث جديد', updPillErr: 'تعذّر الفحص', updPillWait: 'جاري الفحص…',
    updNoSetup: 'التحديث التلقائي يعمل فقط للنسخة المثبّتة بملف StreamExtra-Setup.exe.', updAuto: 'يفحص البرنامج GitHub تلقائياً عند الفتح وكل 30 دقيقة.',
    updNone: '—', updIn: 'بعد', updMin: 'دقيقة', updNow: 'الآن', updDateUnknown: 'غير معروف'
  });
  Object.assign(I18N.en, {
    updTab: 'Updates', grpAbout: 'About', pg_updates: 'Your version, the latest release date, and update check.',
    updCur: 'Your current version', updCurDate: 'Your version date', updLatest: 'Latest available version', updLatestDate: 'Update date (new version release date)',
    updChecked: 'Last checked', updNext: 'Next automatic check', updCheckBtn: 'Check for updates', updChecking: 'Checking…', updRunBtn: 'Update now',
    updPillOk: 'You are up to date ✓', updPillNew: 'Update available', updPillErr: 'Check failed', updPillWait: 'Checking…',
    updNoSetup: 'Automatic update only works for the copy installed with StreamExtra-Setup.exe.', updAuto: 'The app checks GitHub automatically on start and every 30 minutes.',
    updNone: '—', updIn: 'in', updMin: 'min', updNow: 'now', updDateUnknown: 'Unknown'
  });
  const css = `
  #seUpd{position:fixed;inset:0;z-index:200;display:none;align-items:center;justify-content:center;background:var(--ovl,rgba(0,0,0,.6));backdrop-filter:blur(4px)}
  #seUpd .box{width:440px;max-width:92vw;background:var(--panel);color:var(--txt);border:1px solid var(--line2);border-radius:18px;box-shadow:var(--shm);padding:30px 28px 24px;text-align:center}
  #seUpd .ic{width:58px;height:58px;margin:0 auto 14px;border-radius:50%;display:grid;place-items:center;font-size:28px;color:var(--on-ac,#fff);background:linear-gradient(135deg,var(--ac-h),var(--ac))}
  #seUpd h2{margin:0 0 6px;font-size:20px}
  #seUpd p{margin:0 0 16px;opacity:.75;font-size:13.5px;line-height:1.6}
  #seUpd .vers{display:flex;gap:10px;justify-content:center;margin-bottom:16px}
  #seUpd .vers div{flex:1;border:1px solid var(--line2);border-radius:10px;padding:9px 6px;font-size:12px;opacity:.95}
  #seUpd .vers b{display:block;font-size:19px;margin-top:3px}
  #seUpd .vers .n b{color:var(--ac-t,var(--ac))}
  #seUpd .warn{display:none;margin:0 0 14px;padding:9px 11px;border-radius:9px;background:rgba(255,170,0,.14);border:1px solid rgba(255,170,0,.4);font-size:12.5px;line-height:1.5}
  #seUpd .go{width:100%;padding:12px;border:0;border-radius:11px;font-size:15px;font-weight:700;cursor:pointer;color:var(--on-ac,#fff);background:linear-gradient(135deg,var(--ac-h),var(--ac))}
  #seUpd .go:disabled{opacity:.6;cursor:default}
  #seUpd .later{margin-top:10px;background:none;border:0;color:inherit;opacity:.6;cursor:pointer;font-size:12.5px;padding:6px 10px}
  #seUpd .later:hover{opacity:1}
  #seUpd .note{margin-top:12px;font-size:11.5px;opacity:.55}`;
  const st = document.createElement('style'); st.textContent = css; document.head.appendChild(st);
  const el = document.createElement('div'); el.id = 'seUpd'; document.body.appendChild(el);
  const L = () => TX[(typeof lang !== 'undefined' && lang === 'en') ? 'en' : 'ar'];

  function show(info) {
    const x = L();
    el.innerHTML = `<div class="box">
      <div class="ic">⬇</div>
      <h2>${x.title}</h2><p>${x.sub}</p>
      <div class="vers"><div>${x.cur}<b>${info.current}</b></div><div class="n">${x.nw}<b>${info.latest}</b></div></div>
      <div class="warn" id="seUpdWarn">${x.live}</div>
      <button class="go" id="seUpdGo">${x.btn}</button>
      <div><button class="later" id="seUpdLater">${x.later}</button></div>
      <div class="note">${x.note}</div></div>`;
    el.style.display = 'flex';
    if (document.body.classList.contains('live')) el.querySelector('#seUpdWarn').style.display = 'block';
    el.querySelector('#seUpdLater').onclick = () => { dismissed = true; el.style.display = 'none'; };
    const go = el.querySelector('#seUpdGo');
    go.onclick = () => startUpdate(go, x.btn);
  }

  async function startUpdate(btn, label, txt) {
    const x = L(), tx = txt || btn;
    btn.disabled = true; tx.textContent = x.wait;
    let r; try { r = await api.updateRun(); } catch (e) { r = { ok: false }; }
    if (!r || !r.ok) { btn.disabled = false; tx.textContent = label; if (typeof toast === 'function') toast(x.fail, 6000); }
  }

  /* ---------- حالة الفحص + صفحة "التحديثات" في الإعدادات ---------- */
  let dismissed = false, last = null, checking = false;
  const EV = 30 * 60 * 1000;
  const g = id => document.getElementById(id);
  const tt = k => (typeof t === 'function' ? t(k) : k);
  const lg = () => (typeof lang !== 'undefined' && lang === 'en') ? 'en' : 'ar';
  function fmtDate(d) {
    if (!d) return tt('updDateUnknown');
    const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(d); const dt = m ? new Date(+m[1], +m[2] - 1, +m[3]) : new Date(d);
    if (isNaN(dt)) return d;
    return dt.toLocaleDateString(lg() === 'en' ? 'en-GB' : 'ar-EG-u-nu-latn', { year: 'numeric', month: 'long', day: 'numeric' });
  }
  const fmtTime = ms => new Date(ms).toLocaleTimeString(lg() === 'en' ? 'en-GB' : 'ar-EG-u-nu-latn', { hour: '2-digit', minute: '2-digit' });
  const esc2 = v => String(v).replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));

  function paintPage() {
    const rows = g('updRows'); if (!rows) return;
    const i = last, pill = g('updPill');
    g('updCheckT').textContent = checking ? tt('updChecking') : tt('updCheckBtn');
    g('updCheckBtn').disabled = checking;
    g('updRunT').textContent = tt('updRunBtn');
    if (!i) { pill.textContent = tt('updPillWait'); pill.className = 'sp-pill'; rows.innerHTML = ''; return; }
    const err = !!i.error, newer = !!i.available;
    pill.textContent = err ? tt('updPillErr') : newer ? tt('updPillNew') : tt('updPillOk');
    pill.className = 'sp-pill ' + (err ? 'bad' : newer ? 'warn' : 'ok');
    const mins = Math.max(0, Math.round((i.checkedAt + EV - Date.now()) / 60000));
    const R = [
      [tt('updCur'), i.current, ''],
      [tt('updCurDate'), fmtDate(i.currentDate), ''],
      [tt('updLatest'), err ? tt('updNone') : i.latest, newer ? 'warn' : (err ? '' : 'ok')],
      [tt('updLatestDate'), err ? tt('updNone') : fmtDate(i.latestDate), ''],
      [tt('updChecked'), fmtTime(i.checkedAt), ''],
      [tt('updNext'), mins <= 0 ? tt('updNow') : (lg() === 'en' ? tt('updIn') + ' ' + mins + ' ' + tt('updMin') : tt('updIn') + ' ' + mins + ' ' + tt('updMin')), '']
    ];
    rows.innerHTML = R.map(([k, v, c]) => `<div class="sp-r ${c}"><span>${esc2(k)}</span><b dir="ltr">${esc2(v)}</b></div>`).join('');
    g('updRunBtn').hidden = !(newer && i.canUpdate);
    g('updNote').textContent = !i.canUpdate ? tt('updNoSetup') : tt('updAuto');
    const dot = g('updDot'); if (dot) dot.hidden = !newer;
  }

  async function check(manual) {
    if (checking) return;
    checking = true; paintPage();
    try {
      const i = await api.updateCheck();
      if (i) { last = Object.assign({ checkedAt: Date.now() }, i); }
    } catch (e) { last = Object.assign(last || { current: '?', currentDate: '', latest: '?', latestDate: '', available: false, canUpdate: false }, { error: String(e), checkedAt: Date.now() }); }
    checking = false; paintPage();
    if (last && last.available && last.canUpdate && !dismissed && !manual) show(last);
  }

  document.addEventListener('click', e => {
    if (e.target.closest('#updCheckBtn')) check(true);
    const rb = e.target.closest('#updRunBtn'); if (rb && !rb.disabled) startUpdate(rb, tt('updRunBtn'), g('updRunT'));
  });

  // عند فتح صفحة "التحديثات": نرسم آخر نتيجة، ونفحص إن لم يُفحص من قبل
  const _page = window.onSettingsPage;
  window.onSettingsPage = function (n) {
    if (_page) _page(n);
    if (n === 'updates') { paintPage(); if (!last || Date.now() - last.checkedAt > 60 * 1000) check(true); }
  };
  const _ai = applyI18n;
  applyI18n = function () { _ai(); try { paintPage(); } catch (e) {} };

  setTimeout(() => check(false), 1500);                 // عند فتح البرنامج
  setInterval(() => check(false), EV);                  // وكل نصف ساعة أثناء بقائه مفتوحاً
})();
