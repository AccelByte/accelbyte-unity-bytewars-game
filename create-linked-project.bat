@echo off
echo Enter linked project root folder name:
set /P game_dir=
mkdir ..\%game_dir%
mklink /D ..\%game_dir%\Assets %cd%\Assets
mklink /D ..\%game_dir%\Packages %cd%\Packages
mklink /D ..\%game_dir%\local-packages %cd%\local-packages
mklink /D ..\%game_dir%\ProjectSettings %cd%\ProjectSettings