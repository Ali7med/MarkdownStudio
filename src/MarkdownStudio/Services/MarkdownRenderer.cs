using System.IO;
using System.Text;
using Markdig;

namespace MarkdownStudio.Services;

/// <summary>
/// تطبيق <see cref="IMarkdownRenderer"/> باستخدام Markdig مع خط أنابيب متقدّم
/// (جداول، قوائم مهام، حواشٍ، إيموجي، رياضيات، مخططات، روابط تلقائية).
/// </summary>
public sealed class MarkdownRenderer : IMarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()   // tables, footnotes, task lists, autolinks, emphasis extras…
        .UseEmojiAndSmiley()
        .UseMathematics()          // $...$ و $$...$$
        .UseGridTables()
        .UsePipeTables()
        .UseAutoLinks()
        .UseGenericAttributes()
        .UsePreciseSourceLocation()   // لمزامنة المؤشر (رقم سطر كل كتلة)
        .Build();

    public string RenderFragment(string markdown)
        => Markdown.ToHtml(markdown ?? string.Empty, _pipeline);

    /// <summary>رقم السطر (1-based) للكتلة العليا رقم <paramref name="blockIndex"/> — لمزامنة المؤشر.</summary>
    public int GetBlockLine(string markdown, int blockIndex)
    {
        if (string.IsNullOrEmpty(markdown)) return 1;
        var doc = Markdown.Parse(markdown, _pipeline);
        if (blockIndex < 0 || blockIndex >= doc.Count) return 1;
        return doc[blockIndex].Line + 1;
    }

    public string RenderDocument(string markdown, bool darkTheme, string? baseDirectory = null)
        => Build(markdown, darkTheme, baseDirectory, editable: false);

    public string RenderEditable(string markdown, bool darkTheme, string? baseDirectory = null)
        => Build(markdown, darkTheme, baseDirectory, editable: true);

    private string Build(string markdown, bool darkTheme, string? baseDirectory, bool editable)
    {
        var body = RenderFragment(markdown);
        var css = darkTheme ? DarkCss : LightCss;
        var baseTag = string.IsNullOrEmpty(baseDirectory)
            ? string.Empty
            : $"<base href=\"{new Uri(baseDirectory! + Path.DirectorySeparatorChar).AbsoluteUri}\">";

        var editAttr = editable ? " contenteditable=\"true\" spellcheck=\"false\"" : string.Empty;

        var sb = new StringBuilder(body.Length + 4096);
        sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\">");
        sb.Append(baseTag);
        sb.Append("<style>").Append(BaseCss).Append(css);
        if (editable) sb.Append(EditableCss);
        sb.Append("</style>");
        sb.Append("</head><body><article class=\"markdown-body\"").Append(editAttr).Append('>');
        sb.Append(body);
        sb.Append("</article>");
        // MathJax + Mermaid (تُعطَّل في وضع التحرير لتفادي إفساد الـ HTML القابل للتحويل).
        if (!editable) sb.Append(Scripts);
        else sb.Append(EditableScript);
        sb.Append("</body></html>");
        return sb.ToString();
    }

    private const string BaseCss = """
        :root { color-scheme: light dark; }
        * { box-sizing: border-box; }
        body { margin: 0; padding: 24px 32px; font-family: 'Segoe UI Variable', 'Segoe UI', Inter, system-ui, sans-serif;
               font-size: 15px; line-height: 1.7; -webkit-font-smoothing: antialiased; }
        .markdown-body { max-width: 900px; margin: 0 auto; }
        h1,h2,h3,h4,h5,h6 { font-weight: 600; line-height: 1.3; margin: 1.4em 0 .6em; }
        h1 { font-size: 2em; padding-bottom: .3em; border-bottom: 1px solid var(--border); }
        h2 { font-size: 1.5em; padding-bottom: .3em; border-bottom: 1px solid var(--border); }
        h3 { font-size: 1.25em; } h4 { font-size: 1em; }
        p { margin: .8em 0; }
        a { color: var(--accent); text-decoration: none; }
        a:hover { text-decoration: underline; }
        code { font-family: 'Cascadia Code', 'Consolas', monospace; font-size: .9em;
               background: var(--code-bg); padding: .2em .4em; border-radius: 6px; }
        pre { background: var(--code-bg); padding: 14px 16px; border-radius: 10px; overflow: auto; }
        pre code { background: none; padding: 0; }
        blockquote { margin: .8em 0; padding: .2em 1em; border-left: 4px solid var(--accent);
                     color: var(--muted); background: var(--quote-bg); border-radius: 0 8px 8px 0; }
        table { border-collapse: collapse; margin: 1em 0; width: 100%; }
        th, td { border: 1px solid var(--border); padding: 8px 12px; }
        th { background: var(--code-bg); font-weight: 600; }
        img { max-width: 100%; border-radius: 8px; }
        hr { border: none; border-top: 1px solid var(--border); margin: 1.6em 0; }
        ul.contains-task-list { list-style: none; padding-left: 1.2em; }
        .task-list-item input { margin-right: .5em; }
        """;

    private const string LightCss = """
        :root { --accent:#0969da; --border:#d0d7de; --code-bg:#f6f8fa; --quote-bg:#f6f8fa;
                --muted:#57606a; }
        body { background:#ffffff; color:#1f2328; }
        """;

    private const string DarkCss = """
        :root { --accent:#4493f8; --border:#30363d; --code-bg:#161b22; --quote-bg:#161b22;
                --muted:#8b949e; }
        body { background:#0d1117; color:#e6edf3; }
        """;

    private const string EditableCss = """
        article[contenteditable] { outline: none; min-height: 90vh; caret-color: var(--accent); }
        article[contenteditable]:empty::before { content: 'ابدأ الكتابة…'; opacity: .4; }
        ::selection { background: color-mix(in srgb, var(--accent) 30%, transparent); }
        """;

    // جسر التحرير المرئي: يرسل HTML عند التعديل، ويستقبل أوامر التنسيق من المضيف.
    private const string EditableScript = """
        <script>
          const art = document.querySelector('article');
          let timer, caretTimer;
          function pushHtml() {
            window.chrome.webview.postMessage(JSON.stringify({ type:'html', html: art.innerHTML }));
          }
          art.addEventListener('input', () => { clearTimeout(timer); timer = setTimeout(pushHtml, 350); });

          // مزامنة المؤشر: أبلغ المضيف بفهرس الكتلة العليا التي فيها المؤشّر.
          document.addEventListener('selectionchange', () => {
            clearTimeout(caretTimer);
            caretTimer = setTimeout(() => {
              const sel = window.getSelection();
              if (!sel || !sel.rangeCount) return;
              let node = sel.anchorNode;
              if (!node) return;
              if (node.nodeType === 3) node = node.parentElement;
              while (node && node.parentElement && node.parentElement !== art) node = node.parentElement;
              if (node && node.parentElement === art) {
                const idx = Array.prototype.indexOf.call(art.children, node);
                if (idx >= 0) window.chrome.webview.postMessage(JSON.stringify({ type:'caret', blockIndex: idx }));
              }
            }, 150);
          });

          function insertTable() {
            const r = prompt('عدد الصفوف × الأعمدة (مثال 3x3):', '3x3'); if (!r) return;
            const parts = r.split(/[x×*,]/);
            const rows = Math.max(1, parseInt(parts[0]) || 3);
            const cols = Math.max(1, parseInt(parts[1]) || 3);
            let h = '<table><thead><tr>';
            for (let c = 0; c < cols; c++) h += '<th>عنوان ' + (c + 1) + '</th>';
            h += '</tr></thead><tbody>';
            for (let i = 0; i < rows; i++) { h += '<tr>'; for (let c = 0; c < cols; c++) h += '<td>&nbsp;</td>'; h += '</tr>'; }
            h += '</tbody></table><p><br></p>';
            document.execCommand('insertHTML', false, h);
          }

          // ==== قائمة سياق الجداول (كليك يمين على خلية) ====
          let ctxCell = null;
          const tmenu = document.createElement('div');
          tmenu.style.cssText = 'position:fixed;z-index:9999;display:none;min-width:170px;padding:4px;'
            + 'background:var(--code-bg);border:1px solid var(--border);border-radius:8px;'
            + 'box-shadow:0 8px 24px rgba(0,0,0,.28);font-size:13px;';
          document.body.appendChild(tmenu);
          function hideMenu(){ tmenu.style.display = 'none'; }
          function tsep(){ const s=document.createElement('div'); s.style.cssText='height:1px;margin:4px 6px;background:var(--border);'; return s; }
          function titem(label, fn){
            const b=document.createElement('div'); b.textContent=label;
            b.style.cssText='padding:6px 12px;border-radius:6px;cursor:pointer;white-space:nowrap;';
            b.onmouseenter=()=>b.style.background='rgba(127,127,127,.22)';
            b.onmouseleave=()=>b.style.background='transparent';
            b.onmousedown=ev=>{ ev.preventDefault(); fn(); hideMenu(); clearTimeout(timer); timer=setTimeout(pushHtml,30); };
            return b;
          }
          function curTable(){ return ctxCell ? ctxCell.closest('table') : null; }
          function insertRow(dir){
            const t=curTable(); if(!t) return; const tr=ctxCell.closest('tr');
            const cols=t.rows[0].cells.length; const nr=document.createElement('tr');
            for(let i=0;i<cols;i++){ const td=document.createElement('td'); td.innerHTML='&nbsp;'; nr.appendChild(td); }
            const tb=t.tBodies[0]; if(!tb){ t.appendChild(nr); return; }
            if(tr.parentNode.tagName==='THEAD') tb.insertBefore(nr, tb.firstChild);
            else if(dir<0) tr.parentNode.insertBefore(nr, tr);
            else tr.parentNode.insertBefore(nr, tr.nextSibling);
          }
          function insertCol(side){
            const t=curTable(); if(!t) return; const at=ctxCell.cellIndex+side;
            for(const row of t.rows){
              const head=row.parentNode.tagName==='THEAD';
              const cell=document.createElement(head?'th':'td');
              cell.innerHTML= head?'عنوان':'&nbsp;';
              row.insertBefore(cell, row.cells[at]||null);
            }
          }
          function deleteRow(){ const t=curTable(); if(!t) return; const tr=ctxCell.closest('tr');
            if(tr.parentNode.tagName==='THEAD') return; tr.remove(); }
          function deleteCol(){ const t=curTable(); if(!t) return; const i=ctxCell.cellIndex;
            if(t.rows[0].cells.length<=1){ t.remove(); return; }
            for(const row of t.rows){ if(row.cells[i]) row.cells[i].remove(); } }
          function deleteTable(){ const t=curTable(); if(t) t.remove(); }
          function buildMenu(){
            tmenu.innerHTML='';
            tmenu.append(
              titem('إدراج صف فوق', ()=>insertRow(-1)),
              titem('إدراج صف تحت', ()=>insertRow(1)),
              titem('إدراج عمود يسار', ()=>insertCol(0)),
              titem('إدراج عمود يمين', ()=>insertCol(1)),
              tsep(),
              titem('حذف الصف', deleteRow),
              titem('حذف العمود', deleteCol),
              titem('حذف الجدول', deleteTable));
          }
          art.addEventListener('contextmenu', ev=>{
            const cell = ev.target.closest ? ev.target.closest('td,th') : null;
            if(cell && art.contains(cell)){
              ev.preventDefault(); ctxCell=cell; buildMenu();
              tmenu.style.display='block';
              tmenu.style.left=Math.min(ev.clientX, window.innerWidth-190)+'px';
              tmenu.style.top=Math.min(ev.clientY, window.innerHeight-280)+'px';
            }
          });
          document.addEventListener('mousedown', ev=>{ if(!tmenu.contains(ev.target)) hideMenu(); });
          document.addEventListener('scroll', hideMenu, true);

          function escapeHtml(s){ const d=document.createElement('div'); d.textContent=s; return d.innerHTML; }
          function inlineCode(){
            const sel=window.getSelection(); const text=(sel && sel.rangeCount)? sel.toString() : '';
            document.execCommand('insertHTML', false, '<code>'+(text?escapeHtml(text):'code')+'</code>');
          }
          function taskList(){
            document.execCommand('insertHTML', false,
              '<ul class="contains-task-list"><li class="task-list-item"><input type="checkbox"> مهمة</li></ul>');
          }

          window.chrome.webview.addEventListener('message', e => {
            let m; try { m = JSON.parse(e.data); } catch { return; }
            art.focus();
            if (m.fontSize) { art.style.fontSize = m.fontSize + 'px'; return; }
            let cmd = m.cmd, val = m.value ?? null;
            if (cmd === 'table')      { insertTable(); clearTimeout(timer); timer = setTimeout(pushHtml, 50); return; }
            if (cmd === 'inlineCode') { inlineCode(); clearTimeout(timer); timer = setTimeout(pushHtml, 50); return; }
            if (cmd === 'tasklist')   { taskList();  clearTimeout(timer); timer = setTimeout(pushHtml, 50); return; }
            if (cmd === 'link')  { val = prompt('أدخل الرابط:', 'https://'); if (!val) return; cmd = 'createLink'; }
            if (cmd === 'image') { val = prompt('رابط الصورة:', 'https://'); if (!val) return; cmd = 'insertImage'; }
            document.execCommand(cmd, false, val);
            clearTimeout(timer); timer = setTimeout(pushHtml, 50);
          });
        </script>
        """;

    private const string Scripts = """
        <script>
          window.MathJax = { tex: { inlineMath: [['$','$']], displayMath: [['$$','$$']] } };
        </script>
        <script async src="https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-mml-chtml.js"></script>
        <script type="module">
          try {
            const m = await import('https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs');
            const dark = matchMedia('(prefers-color-scheme: dark)').matches;
            m.default.initialize({ startOnLoad:false, theme: dark ? 'dark' : 'default' });
            document.querySelectorAll('pre>code.language-mermaid').forEach((el,i)=>{
              const d=document.createElement('div'); d.className='mermaid'; d.textContent=el.textContent;
              el.parentElement.replaceWith(d);
            });
            await m.default.run();
          } catch(e) { /* offline: تجاهل */ }
        </script>
        """;
}
