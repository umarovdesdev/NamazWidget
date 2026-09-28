@echo off
rem Builds NamazWidget.exe with the C# compiler of .NET Framework 4 (Windows 7 / 8 / 10 / 11)
rem Result: ..\release\NamazWidget.exe (committed to git)
setlocal
if not exist "%~dp0..\release" mkdir "%~dp0..\release"
set "FW=%WINDIR%\Microsoft.NET\Framework\v4.0.30319"
if not exist "%FW%\csc.exe" (
  echo .NET Framework 4 not found. Install .NET Framework 4.8: https://dotnet.microsoft.com/download/dotnet-framework
  exit /b 1
)
"%FW%\csc.exe" /nologo /target:winexe /optimize+ /codepage:65001 ^
  /out:"%~dp0..\release\NamazWidget.exe" /win32icon:"%~dp0NamazWidget.ico" ^
  /r:"%FW%\WPF\PresentationFramework.dll" /r:"%FW%\WPF\PresentationCore.dll" /r:"%FW%\WPF\WindowsBase.dll" ^
  /r:System.Xaml.dll /r:System.Web.Extensions.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll /r:System.Xml.dll ^
  "%~dp0NamazWidget.cs"
