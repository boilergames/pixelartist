@echo off
if not exist "Build" mkdir "Build"
set unity="C:\Program Files\Unity\Editor\Unity.exe"
echo Located unity at %unity%
echo Building...
%unity% -batchmode -nographics -logFile Build/buildlog.txt -buildWindowsPlayer Build/Win32/PixelArtist.exe -buildWindows64Player Build/Win64/PixelArtist.exe -buildLinuxUniversalPlayer Build/Linux/PixelArtist -buildOSX64Player Build/MacOSX/PixelArtist.app -quit