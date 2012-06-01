@echo off
cd %~dp0

erase *.msi 
erase *.wixpdb

autopackage coapp.vse.autopkg  || goto EOF:

rem for %%v  in (*.msi) do curl -T  %%v http://coapp.org/upload/ || goto EOF:
rem echo "Uploaded to repository"