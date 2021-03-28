@echo off
rem tag-filter.bat
rem run tag-filter.sh with git bash
for /f "tokens=*" %%A in ('where git') do (set gitpath=%%~dpA)
set gitpath=%gitpath:~0,-5%
"%gitpath%\bin\bash.exe" "%~dp0%tag-filter.sh"