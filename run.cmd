@echo off
REM يبني ويشغّل Markdown Studio. مرّر مسار ملف اختيارياً:  run.cmd samples\demo.md
setlocal
cd /d "%~dp0"
dotnet run --project "src\MarkdownStudio" -- %*
endlocal
