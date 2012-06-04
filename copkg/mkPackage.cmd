@echo off
cd %~dp0

erase *.msi
erase *.wixpdb

msbuild ../coapp-vse.sln /p:Configuration=Release /verbosity:q

autopackage coapp.vse.autopkg
