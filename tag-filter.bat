@echo off

for /f "tokens=*" %%A in ('where git') do (set gitpath=%%~dpA)
set gitpath=%gitpath:~0,-5%

"%gitpath%\bin\bash.exe" "%~dp0%tag-filter.sh"
