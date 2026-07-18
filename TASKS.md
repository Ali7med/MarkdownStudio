# Markdown Studio — Task Tracker

> **Documentation IDE** احترافي على Windows | .NET 10 · WPF · MVVM
> آخر تحديث: 2026-07-17

---

## Legend
- [ ] Not started · [~] In progress · [x] Done · [!] Blocked

---

## Milestone 0 — Foundation (Setup) ✅
- [x] إنشاء Solution + Project structure (.NET 10 WPF)
- [x] إضافة NuGet packages (WPF-UI 4.3, AvalonEdit 6.3.1, Markdig 1.3.2, WebView2, CommunityToolkit.Mvvm 8.4.2, AvalonDock 4.74.1, Hosting 10.0.10)
- [x] إعداد Dependency Injection (Generic Host)
- [x] هيكل المجلدات (Views / ViewModels / Services / Models / Assets)
- [x] MVVM base (ObservableObject, RelayCommand عبر CommunityToolkit)
- [x] بناء ناجح + تشغيل التطبيق (0 errors / 0 warnings)

## Milestone 1 — App Shell & UI Skeleton
- [x] Custom Window (Mica, Custom Title Bar, WPF-UI FluentWindow)
- [x] Layout أساسي (Editor | Preview + Splitter، Toolbar، Tabs، Status Bar)
- [x] نظام ثيمات بأربعة قوالب جاهزة (Fluent داكن · Mica شفاف · مينمل دافئ · جريء بنفسجي)
      لكلٍّ أساس + لون مميّز + نوع خلفية + تلوين أسطح · مبدّل بالشريط + Palette · يُحفظ الاختيار
- [x] Toolbar مبسّط (جديد/فتح/حفظ/حفظ باسم/ثيم) + Status Bar (Ln/Col)
- [x] Empty State / Welcome Screen (عنوان + جديد/فتح + قائمة الأخيرة + تلميح السحب)
- [x] تنبيه الحفظ عند الإغلاق (حوار WPF-UI: مستند مفرد + كل النافذة، Save/Don't/Cancel)
- [x] Fade transition عند تغيير الثيم (200ms)
- [x] تحكّم اللوحات (dock-like): إظهار/إخفاء المستكشف والمعاينة مستقلاً + تحجيم
- [x] أيقونة تطبيق مخصّصة (ICO متعدّد الأحجام 16→256) — نافذة + شريط عنوان + exe
- [ ] AvalonDock float/drag/auto-hide كامل — مؤجّل (تعويم/سحب فقط؛ الإظهار/الإخفاء متوفّر)

## Milestone 2 — Editor Core (Phase 1)
- [x] دمج AvalonEdit (bound للمستند النشط، two-way sync)
- [x] Line Numbers · Word Wrap
- [x] Markdown Syntax Highlighting (xshd: عناوين/عريض/مائل/كود/روابط/صور/اقتباس/قوائم/HR/HTML)
- [x] Current Line Highlight
- [x] Find · Replace (لوحة منزلقة: Case/Word/Regex، عدّاد تطابقات، Enter/Shift+Enter، Replace All)
- [x] اختصارات: Ctrl+F / Ctrl+H / Ctrl+G / Ctrl+S / Ctrl+O / Ctrl+N / Esc
- [x] Go To Line (Ctrl+G) — لوحة قفز مع تحقّق من المدى
- [x] Code Folding (طيّ حسب العناوين + كتل الأكواد، تحديث debounced)
- [x] Document model + Dirty state

## Milestone 3 — Live Preview (Phase 1)
- [x] دمج WebView2 (NavigateToString)
- [x] Markdig pipeline (Tables, TaskLists, Footnotes, Emoji, Math, Mermaid, AutoLinks)
- [x] Live refresh (يتحدّث مع الكتابة)
- [x] Theme-matched preview CSS (GitHub-like، فاتح/داكن)
- [ ] Debounce (أداء الملفات الكبيرة) — التالي
- [ ] Scroll Sync

## Milestone 4 — File Operations (Phase 1)
- [x] Open / Save / Save As (حوارات Win32)
- [x] Drag & Drop (ملف / عدة ملفات)
- [x] Encoding / Line Ending detection (BOM)
- [x] فتح ملفات من سطر الأوامر (CLI args — ملف/مجلد/URI)
- [x] Recent Files (قائمة "الأخيرة" + حفظ JSON في %AppData%)
- [x] File Watcher (Auto Reload عند تغيّر الملف خارجياً، مع حماية التعديلات غير المحفوظة)
- [x] Folder Drop → Workspace (سحب مجلد يفتح مساحة عمل، سحب ملف يفتح تبويب)

## Milestone 5 — Export (Phase 1) ✅
- [x] Export HTML (يعيد استخدام مُصيّر Markdig مع CSS الثيم)
- [x] Export PDF (WebView2 خارج الشاشة → PrintToPdfAsync، ثيم فاتح)
- [x] Export DOCX (OpenXML altChunk — Word يحوّل HTML لمحتوى أصلي)
- [x] Export Plain Text (Markdig ToPlainText)
- ملاحظة: HTML/TXT/DOCX متحقَّقة آلياً؛ PDF يحتاج نقرة يدوية للتأكيد النهائي.

## Milestone 5.7 — ما الجديد + تعدّد اللغات ✅ جديد
### What's New / Changelog
- [x] مصدر إصدار وحيد [AppVersion.cs](src/MarkdownStudio/AppVersion.cs) (أُزيل رقم الإصدار من .csproj)
- [x] [CHANGELOG.md](src/MarkdownStudio/CHANGELOG.md) بصياغة صارمة (HIGHLIGHTS/NEW/IMPROVED/FIXED)
- [x] محلّل صارم + قراءة وقت التشغيل (مورد مضمّن + احتياط قرص)
- [x] ظهور تلقائي مرّة واحدة عند تغيّر الإصدار (LastShownVersion في الإعدادات) + فتح يدوي (زر + Palette)
- [x] لوحة عرض التاريخ كاملاً مع HIGHLIGHTS بارز وأقسام ملوّنة
- [x] الدستور: [docs/RELEASE-PROCESS.md](docs/RELEASE-PROCESS.md)
### تعدّد اللغات
- [x] محرّك Localizer (اكتشاف تلقائي: مضمّن + مجلد lang على القرص) + تبديل حيّ عبر {loc:Loc}
- [x] العربية + الإنجليزية، مع اتجاه نص/نوافذ تلقائي (RTL/LTR) وأرقام لاتينية دائماً
- [x] توطين شامل: كل الرسائل + عدّاد الكلمات + سطر/عمود + المحتوى الافتراضي — تتفاعل فوراً مع التبديل (98 مفتاح متطابق)
- [x] قائمة اختيار اللغة + حفظ الاختيار + تطبيق لغة النظام عند أول تشغيل
- [x] إضافة لغة بإسقاط ملف JSON: [docs/ADDING-A-LANGUAGE.md](docs/ADDING-A-LANGUAGE.md)
- [x] إصلاح: حوارات التنبيه تظهر متوسّطة على نافذة البرنامج (شاشتين) بتخطيط RTL

## Milestone 5.5 — Visual Studio Mode (WYSIWYG) ✅ جديد
- [x] وضع "استوديو": تحرير مرئي مباشر على المعاينة (WebView2 contenteditable)
- [x] شريط أدوات غني كامل:
  - تراجع/إعادة · عريض/مائل/يتوسّطه خط · **كود سطري**
  - H1/H2/H3/فقرة · قائمة نقطية/مرقّمة/**مهام** · **إزاحة +/−** · اقتباس · كتلة كود
  - رابط · **إزالة رابط** · صورة · جدول · فاصل · إزالة تنسيق · حجم الخط
  - الشريط قابل للتمرير أفقياً
- [x] توليد كود Markdown تلقائياً من التحرير المرئي (HTML→MD عبر ReverseMarkdown) يظهر بلوحة الكود
- [x] جسر JS↔C# (postMessage) + لوحة الكود للعرض فقط أثناء الوضع المرئي
- [x] **جداول**: زر يُدرج جدولاً (صفوف×أعمدة) قابل التحرير → GFM تلقائياً
- [x] **قائمة سياق الجداول** (كليك يمين): إدراج/حذف صف وعمود · حذف الجدول (يحمي صف الرأس)
- [x] **مزامنة المؤشر**: موضع المؤشّر المرئي يُظلّل السطر المقابل بلوحة الكود (فهرس كتلة دقيق)
- التحقق: HTML→MD (6) + HTML قابل للتحرير (5) + GetBlockLine/جدول (8) — كلها نجحت آلياً

## Milestone 6 — Workspace & Tabs (Phase 2)
- [x] فتح مجلد كامل (Explorer tree — فحص async، تجاهل node_modules/.git/bin…)
- [x] File ops (New File/Folder · Rename inline · Delete · Duplicate · Copy Path · Reveal)
- [x] Multiple Tabs + Pin + Close Others/All (قائمة سياق على التبويب)
- [x] Outline (شجرة Headers) + Breadcrumb (مسار نسبي فوق المحرّر)
- [x] Project Search + Replace in Files (Regex · Case، نتائج قابلة للنقر — Ctrl+Shift+F)
- [ ] Split View · Minimap — مؤجّلة (polish ثقيل)

## Milestone 7 — Productivity (Phase 3)
- [x] Command Palette (Ctrl+Shift+P — ترشيح ضبابي، كل الأوامر)
- [x] Quick Open (Ctrl+P — ملفات المشروع)
- [x] Symbol Search (Ctrl+Shift+O — عناوين المستند)
- [x] TOC generator · Auto Numbering (هرمي، idempotent) · Auto Formatting (idempotent)
- [x] Smart Paste (URL→رابط) · Image Paste (لصق صورة→/images) · Link Checker
- [x] Word Counter (كلمات/أحرف/أسطر/زمن قراءة) · Focus Mode · Reading Mode · Zoom (Ctrl±/عجلة)
- [ ] Spell Checker (NHunspell + قواميس خارجية) — مؤجّل

## Milestone 8 — Windows Integration
- [x] Single Instance (Mutex + Named Pipe يمرّر الوسائط ويُبرز النافذة) + CLI args (ملف/مجلد)
- [x] File Associations / Open With (HKCU ProgId + OpenWithProgids)
- [x] Explorer Context Menu (SystemFileAssociations لكل امتداد)
- [x] Jump List (ملفات أخيرة بشريط المهام) · Recent Docs
- [x] URI Protocol (markdownstudio://open?file=… / ?folder=…)
- [x] Portable Mode (portable.marker يعطّل كل تعديلات Registry)
- [x] Send To (اختصار WScript.Shell) · Recent Docs / Windows Search (SHAddToRecentDocs)
- [ ] Explorer Thumbnail/Preview — يحتاج shell extension أصلي (مؤجّل)

## Later Phases (Backlog)
- [ ] Phase 4 — Git (Status/Commit/History/Diff/Branch)
- [ ] Phase 5 — AI Assistant (Rewrite/Summarize/Translate/…)
- [ ] Phase 6 — Plugins API
- [ ] Phase 7 — Custom Themes / Shortcuts / Settings
- [ ] Phase 8 — Documentation Tools (Linter, YAML, Analyzers)
- [ ] Phase 9 — Large Files performance
- [ ] Phase 10 — Extra (Snippets, Templates, Bookmarks, Terminal…)

---

## Decisions Log
- **2026-07-17**: التزام بـ **.NET 10** (LTS) بدل .NET 9 المذكور بالخطة — أحدث إصدار متاح (SDK 10.0.302).
- UI stack: **Wpf.Ui** (Fluent/Mica) + **AvalonDock** (docking) + **AvalonEdit** (editor) + **WebView2** (preview) + **Markdig** (parser).
- Architecture: MVVM عبر CommunityToolkit.Mvvm + Generic Host DI.

## Now Working On
✓ اكتمل الهدف: M1 · M4 · M6 · M7 · M8 (نواة كل ميلستون + غالبية الفرعيات)
→ التالي المقترح: Phase 5 (AI Assistant) · Split View · Spell Checker · AvalonDock كامل

## اختصارات لوحة المفاتيح
- Ctrl+Shift+P لوحة الأوامر · Ctrl+P فتح سريع · Ctrl+Shift+O رموز
- Ctrl+Shift+F بحث بالمشروع · Ctrl+F بحث · Ctrl+H استبدال · Ctrl+G سطر
- Ctrl+S/O/N · Ctrl+± / Ctrl+عجلة تكبير · Ctrl+0 إعادة تعيين · F2 تسمية · Del حذف

## Build & Run
```
cd src/MarkdownStudio
dotnet run
# أو افتح ملفاً مباشرة:
dotnet run -- README.md
```
- SDK: .NET 10 (10.0.302) · TFM: net10.0-windows
- بناء نظيف: 0 errors / 0 warnings
