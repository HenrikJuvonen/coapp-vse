@echo off
cd %~dp0

erase *.msi
erase *.wixpdb

rem autopackage coapp.vse.autopkg
ptk package