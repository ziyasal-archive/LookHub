@echo off
call "%VS120COMNTOOLS%..\..\VC\vcvarsall.bat"
msbuild default.build /t:All /nologo
