@echo off
echo Enter linked project root folder name:
set /P game_dir=
mkdir ..\%game_dir%
mklink /J ..\%game_dir%\Assets %cd%\Assets
mklink /J ..\%game_dir%\Packages %cd%\Packages
mklink /J ..\%game_dir%\local-packages %cd%\local-packages
mklink /J ..\%game_dir%\ProjectSettings %cd%\ProjectSettings