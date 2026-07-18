<div align="center">

# 📝 MarkdownStudio

**A modern, IDE-style Markdown editor for Windows with live WYSIWYG editing**

**محرّر Markdown حديث بأسلوب بيئة تطوير متكاملة لويندوز مع تحرير مرئي حيّ**

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6?logo=windows&logoColor=white)
![WPF](https://img.shields.io/badge/UI-WPF-blueviolet)
![Version](https://img.shields.io/badge/version-1.0.0-brightgreen)

</div>

---

## English

### Overview
**MarkdownStudio** is a desktop Markdown editor built with **WPF** on **.NET 10**. It combines a code editor, a live preview, and a visual WYSIWYG mode in a single workspace-oriented interface — edit directly on the preview and the Markdown source is generated on the other side.

### ✨ Features
- **Visual (WYSIWYG) editor** — edit directly on the rendered preview; Markdown is generated live.
- **Rich formatting toolbar** — bold, italic, strikethrough, headings, lists, quotes, code, links, images, and font size.
- **Live preview** via WebView2 + Markdig, with cursor sync between the code pane and preview.
- **Code editor** powered by AvalonEdit — Markdown syntax highlighting, code folding, find/replace, and go-to-line.
- **Tables** in visual mode with a context menu to add/remove rows and columns.
- **Full folder workspace** (IDE-style) — tree file explorer with create/rename/delete/duplicate.
- **Document outline** + breadcrumb navigation.
- **Project-wide search & replace.**
- **Command palette**, quick open, and symbol search.
- **Export** to HTML, PDF, Word (DOCX), and plain text.
- **Windows integration** — single instance, file association, context menu, Jump List, and URI protocol.
- **Multi-language UI** with automatic text direction (RTL/LTR).
- **What's New** screen with a built-in changelog system.

### 🛠️ Tech Stack
| Area | Technology |
|------|-----------|
| Runtime | .NET 10 (Windows) |
| UI | WPF, WPF-UI, AvalonDock |
| Editor | AvalonEdit |
| Markdown | Markdig, ReverseMarkdown |
| Preview | Microsoft.Web.WebView2 |
| MVVM | CommunityToolkit.Mvvm |
| Export | DocumentFormat.OpenXml |

### 🚀 Getting Started

**Requirements**
- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (usually preinstalled on Windows 11)

**Build & Run**
```bash
git clone https://github.com/<your-username>/MarkdownStudio.git
cd MarkdownStudio
dotnet restore
dotnet build -c Release
dotnet run --project src/MarkdownStudio
```

### 📄 License
This project is released under the MIT License. See the `LICENSE` file for details.

---

<div dir="rtl">

## العربية

### نظرة عامة
**MarkdownStudio** هو محرّر Markdown لسطح المكتب مبني باستخدام **WPF** على **.NET 10**. يجمع بين محرّر أكواد، ومعاينة حيّة، ووضع تحرير مرئي (WYSIWYG) داخل واجهة واحدة قائمة على مساحة العمل — حرّر مباشرة على المعاينة ويتولّد كود Markdown في الجهة الأخرى.

### ✨ المزايا
- **محرّر مرئي (WYSIWYG)** — حرّر مباشرة على المعاينة ويتولّد الـ Markdown حيًّا.
- **شريط أدوات تنسيق غني** — عريض، مائل، يتوسّطه خط، عناوين، قوائم، اقتباس، كود، روابط، صور، وحجم الخط.
- **معاينة حيّة** عبر WebView2 و Markdig، مع مزامنة المؤشّر بين لوحة الكود والمعاينة.
- **محرّر أكواد** مبني على AvalonEdit — تلوين صياغة Markdown، طيّ الأكواد، بحث/استبدال، والانتقال لسطر.
- **جداول** في الوضع المرئي مع قائمة سياق لإضافة/حذف الصفوف والأعمدة.
- **مساحة عمل كاملة** على مستوى المجلد بأسلوب IDE — مستكشف ملفات شجري مع إنشاء/إعادة تسمية/حذف/تكرار.
- **مخطّط المستند (Outline)** وشريط تنقّل (Breadcrumb).
- **بحث واستبدال على مستوى المشروع كامله.**
- **لوحة الأوامر**، الفتح السريع، وبحث الرموز.
- **تصدير** إلى HTML و PDF و Word (DOCX) ونص عادي.
- **تكامل Windows** — نسخة واحدة، ربط الملفات، قائمة السياق، Jump List، وبروتوكول URI.
- **دعم تعدّد اللغات** مع اتجاه نص تلقائي (RTL/LTR).
- **شاشة "ما الجديد"** مع نظام سجل تغييرات مدمج.

### 🛠️ التقنيات المستخدمة
| المجال | التقنية |
|------|-----------|
| بيئة التشغيل | ‏.NET 10 (Windows) |
| الواجهة | WPF, WPF-UI, AvalonDock |
| المحرّر | AvalonEdit |
| Markdown | Markdig, ReverseMarkdown |
| المعاينة | Microsoft.Web.WebView2 |
| نمط MVVM | CommunityToolkit.Mvvm |
| التصدير | DocumentFormat.OpenXml |

### 🚀 البدء

**المتطلبات**
- ويندوز 10/11
- [‏.NET 10 SDK](https://dotnet.microsoft.com/download)
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (مثبّت مسبقًا غالبًا على ويندوز 11)

**البناء والتشغيل**
```bash
git clone https://github.com/<your-username>/MarkdownStudio.git
cd MarkdownStudio
dotnet restore
dotnet build -c Release
dotnet run --project src/MarkdownStudio
```

### 📄 الترخيص
هذا المشروع منشور تحت رخصة MIT. راجع ملف `LICENSE` للتفاصيل.

</div>
