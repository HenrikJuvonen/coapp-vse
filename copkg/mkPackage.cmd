@echo off
cd %~dp0

erase *.msi
erase *.wixpdb

ptk package
