echo off
set targets=CbzMage_Win CbzMage_Linux CbzMage_macOS
set publish=dotnet publish -c Release -p:PublishProfile=
set zip="C:\Program Files\7-Zip\7z.exe" a -tzip
cd ..\Publish
for %%v in (*.minor) do set oldminor=%%~nv
set /a newminor=oldminor + 1
set /a oldestminor=newminor - 5 
move %oldminor%.minor %newminor%.minor >nul:
for %%v in (*.major) do set major=%%~nv
set oldversion=%major%.%oldminor%
set newversion=%major%.%newminor%
echo Version: %newversion%
for %%t in (%targets%) do if exist %%t rmdir /s /q %%t
cd ..\Source\CbzMage
for %%t in (%targets%) do (
	echo Publish %%t
	%publish%%%t >nul:
)
cd ..\..\Publish
for %%t in (%targets%) do call :create_target %%t
del CbzMage%major%.%oldestminor%_*.zip
cd ..\Source
echo Done
pause
exit /b
:create_target
setlocal enabledelayedexpansion
set base=%1
del %base%\*.pdb
del %base%\*.development.json
set old=!base:_=%oldversion%_! 
if exist %old% rmdir /s /q %old%
set new=!base:_=%newversion%_!
if exist %new% rmdir /s /q %new%
move %base% %new% >nul:
echo Create %new%.zip
if exist %new%.zip del %new%.zip
%zip% %new%.zip %new% >nul:
