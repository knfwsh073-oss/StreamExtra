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
    updNone: '—', updIn: 'بعد', updMin: 'دقيقة', updNow: 'الآن', updDateUnknown: 'غير معروف',
    updNotes: 'ما الجديد', updSrc: 'مصدر الفحص', updRetry: 'إعادة المحاولة',
    updErrOffline: 'لا يوجد اتصال بالإنترنت أو أن GitHub غير متاح حالياً. تأكد من الاتصال ثم أعد المحاولة.',
    updErrTimeout: 'انتهت مهلة الاتصال. الشبكة بطيئة، أعد المحاولة بعد قليل.',
    updErrNotfound: 'لم يتم العثور على ملف الإصدار في المستودع (تأكد أن المستودع عام وأن الفرع main يحتوي app/package.json).',
    updErrHttp: 'رد الخادم برمز خطأ غير متوقع. أعد المحاولة لاحقاً.', updErrBad: 'ملف الإصدار على الخادم غير صالح.'
  });
  Object.assign(I18N.en, {
    updTab: 'Updates', grpAbout: 'About', pg_updates: 'Your version, the latest release date, and update check.',
    updCur: 'Your current version', updCurDate: 'Your version date', updLatest: 'Latest available version', updLatestDate: 'Update date (new version release date)',
    updChecked: 'Last checked', updNext: 'Next automatic check', updCheckBtn: 'Check for updates', updChecking: 'Checking…', updRunBtn: 'Update now',
    updPillOk: 'You are up to date ✓', updPillNew: 'Update available', updPillErr: 'Check failed', updPillWait: 'Checking…',
    updNoSetup: 'Automatic update only works for the copy installed with StreamExtra-Setup.exe.', updAuto: 'The app checks GitHub automatically on start and every 30 minutes.',
    updNone: '—', updIn: 'in', updMin: 'min', updNow: 'now', updDateUnknown: 'Unknown',
    updNotes: "What's new", updSrc: 'Checked via', updRetry: 'Try again',
    updErrOffline: 'No internet connection, or GitHub is currently unreachable. Check your connection and try again.',
    updErrTimeout: 'The connection timed out. Your network is slow, try again in a moment.',
    updErrNotfound: 'The release file was not found in the repository (make sure it is public and the main branch has app/package.json).',
    updErrHttp: 'The server answered with an unexpected error. Try again later.', updErrBad: 'The release file on the server is invalid.'
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
  #seUpd .note{margin-top:12px;font-size:11.5px;opacity:.55}
  .upd-notes{text-align:start;margin:0 0 14px;padding:10px 14px;border-radius:10px;background:var(--well,rgba(255,255,255,.04));border:1px solid var(--line,rgba(255,255,255,.08));font-size:12.5px;line-height:1.6}
  .upd-notes b{display:block;margin-bottom:4px;font-size:12px;opacity:.8}
  .upd-notes ul{margin:0;padding-inline-start:18px}
  #mUpdates [hidden],.upd-dot[hidden]{display:none!important}
  #mUpdates .upd-hero{display:flex;align-items:center;gap:14px;padding:14px;margin:0 0 12px;border-radius:14px;border:1px solid var(--line);background:var(--well)}
  #mUpdates .upd-hero .hi{width:46px;height:46px;flex:none;border-radius:50%;display:grid;place-items:center;font-size:22px;font-weight:800;background:var(--panel3,var(--panel));color:var(--mut)}
  #mUpdates .upd-hero.ok .hi{background:rgba(61,220,132,.15);color:#3ddc84}
  #mUpdates .upd-hero.warn .hi{background:rgba(255,176,32,.16);color:#ffb020}
  #mUpdates .upd-hero.bad .hi{background:rgba(255,107,128,.16);color:#ff6b80}
  #mUpdates .upd-hero .ht{min-width:0}
  #mUpdates .upd-hero .ht b{display:block;font-size:15px}
  #mUpdates .upd-hero .ht small{display:block;margin-top:3px;opacity:.7;line-height:1.5;font-size:12.5px}
  #mUpdates .upd-act{display:flex;gap:8px;flex-wrap:wrap;margin:14px 0 8px}
  #mUpdates .upd-act .sbtn{padding:9px 16px;font-size:13px}
  #mUpdates .upd-act .sbtn.on{background:linear-gradient(135deg,var(--ac-h,var(--ac)),var(--ac));color:var(--on-ac,#fff);border-color:var(--ac)}
  #mUpdates .upd-act .sbtn.spin svg{animation:updspin .9s linear infinite}
  @keyframes updspin{to{transform:rotate(360deg)}}
  .upd-dot{display:inline-block;width:8px;height:8px;border-radius:50%;background:#ffb020;margin-inline-start:auto;box-shadow:0 0 0 3px rgba(255,176,32,.25)}`;
  const st = document.createElement('style'); st.textContent = css; document.head.appendChild(st);
  const el = document.createElement('div'); el.id = 'seUpd'; document.body.appendChild(el);
  const L = () => TX[(typeof lang !== 'undefined' && lang === 'en') ? 'en' : 'ar'];

  const notesOf = i => { const n = i && i.notes; if (!n) return []; const a = n[(typeof lang !== 'undefined' && lang === 'en') ? 'en' : 'ar'] || n.ar || n.en; return Array.isArray(a) ? a.slice(0, 8) : []; };
  const notesHtml = (i, title) => { const a = notesOf(i); return a.length ? `<div class="upd-notes"><b>${title}</b><ul>${a.map(v => '<li>' + String(v).replace(/[&<>]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' }[c])) + '</li>').join('')}</ul></div>` : ''; };

  function show(info) {
    const x = L();
    el.innerHTML = `<div class="box">
      <div class="ic">⬇</div>
      <h2>${x.title}</h2><p>${x.sub}</p>
      <div class="vers"><div>${x.cur}<b>${info.current}</b></div><div class="n">${x.nw}<b>${info.latest}</b></div></div>
      ${notesHtml(info, (typeof lang !== 'undefined' && lang === 'en') ? "What's new" : 'ما الجديد')}
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

  function errText(e) {
    e = String(e || '');
    if (e === 'timeout') return tt('updErrTimeout');
    if (e === 'notfound') return tt('updErrNotfound');
    if (/^http/.test(e)) return tt('updErrHttp');
    if (e === 'badjson') return tt('updErrBad');
    return tt('updErrOffline');
  }
  function paintPage() {
    const rows = g('updRows'); if (!rows) return;
    const i = last, pill = g('updPill'), hero = g('updHero');
    g('updCheckT').textContent = checking ? tt('updChecking') : (i && i.error ? tt('updRetry') : tt('updCheckBtn'));
    g('updCheckBtn').disabled = checking;
    g('updCheckBtn').classList.toggle('spin', checking);
    g('updRunT').textContent = tt('updRunBtn');
    if (!i) {
      pill.textContent = tt('updPillWait'); pill.className = 'sp-pill'; rows.innerHTML = '';
      hero.className = 'upd-hero'; hero.innerHTML = `<span class="hi">…</span><span class="ht"><b>${esc2(tt('updChecking'))}</b></span>`;
      g('updNotes').innerHTML = ''; g('updRunBtn').hidden = true; return;
    }
    const err = !!i.error, newer = !!i.available;
    pill.textContent = err ? tt('updPillErr') : newer ? tt('updPillNew') : tt('updPillOk');
    pill.className = 'sp-pill ' + (err ? 'bad' : newer ? 'warn' : 'ok');
    hero.className = 'upd-hero ' + (err ? 'bad' : newer ? 'warn' : 'ok');
    const hT = err ? tt('updPillErr') : newer ? tt('updPillNew') + ' — ' + i.latest : tt('updPillOk');
    const hS = err ? errText(i.error) : newer ? (i.canUpdate ? '' : tt('updNoSetup')) : tt('updAuto');
    hero.innerHTML = `<span class="hi">${err ? '!' : newer ? '⬇' : '✓'}</span><span class="ht"><b>${esc2(hT)}</b>${hS ? `<small>${esc2(hS)}</small>` : ''}</span>`;
    const mins = Math.max(0, Math.round((i.checkedAt + EV - Date.now()) / 60000));
    const R = [
      [tt('updCur'), i.current, ''],
      [tt('updCurDate'), fmtDate(i.currentDate), ''],
      [tt('updLatest'), err ? tt('updNone') : i.latest, newer ? 'warn' : (err ? '' : 'ok')],
      [tt('updLatestDate'), err ? tt('updNone') : fmtDate(i.latestDate), ''],
      [tt('updChecked'), fmtTime(i.checkedAt), ''],
      [tt('updNext'), mins <= 0 ? tt('updNow') : tt('updIn') + ' ' + mins + ' ' + tt('updMin'), '']
    ];
    if (i.source && !err) R.push([tt('updSrc'), i.source, '']);
    rows.innerHTML = R.map(([k, v, c]) => `<div class="sp-r ${c}"><span>${esc2(k)}</span><b dir="ltr">${esc2(v)}</b></div>`).join('');
    g('updNotes').innerHTML = (newer && !err) ? notesHtml(i, tt('updNotes')) : '';
    g('updRunBtn').hidden = !(newer && i.canUpdate && !err);
    g('updNote').textContent = (!i.canUpdate && !newer) ? tt('updNoSetup') : '';
    const dot = g('updDot'); if (dot) dot.hidden = !(newer && !err);
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
