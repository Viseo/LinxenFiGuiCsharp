@ECHO OFF
REM /******************************************************************************/
REM /* script      : PostConfig.bat	(non généré)                      */
REM /* Objet       : Setting of applciation                   */
REM /******************************************************************************/
REM /* Affaire : Toutes  	Produit : Tout            Version : 01.01         */
REM /******************************************************************************/
  
@echo off

set options=%~1
set options=%options:{="%
set options=%options:}="%



REM Loop to create all parameters variables
for %%O in (%options%) do for /f "tokens=1,* delims=:" %%A in ("%%O") do set "%%A=%%~B"
:loop
if not "%~3"=="" (
  setlocal enableDelayedExpansion
  set "test=!options:*%~3:=! "
  endlocal
  if "!test!"=="!options! " (
      echo Error: Invalid option %~3
  ) else if "!test:~0,1!"==" " (
      set "%~3=1"
  ) else (
      set "%~3=%~4"
      shift /3
  )
  shift /3
  goto :loop
)
set -


setlocal enableDelayedExpansion

SET USER=!-user!
SET QADPASSWD=!-password!
SET DOMAIN=!-domain!
SET QADURL=!-qadurl!

SET ROOTDIR=!-rootdir!
SET WORKDIR=!-workdir!
SET LOGDIR=!-logdir!
SET AUTORETRY=!-autoRetry!
SET EDITPASSWD=!-editPasswd!

SET EMAILTO=!-mailTo!
SET EMAILFROM=!-mailFrom!
SET EMAILSRV=!-mailServer!
SET EMAILPORT=!-mailPort!

ECHO.
ECHO  -========================================- 
ECHO     Instanciation des fichiers .config
ECHO  -========================================- 
attrib -r "..\app.Release.config"
replenv.exe "..\app.Release.config" "..\Linxens.Gui.exe.config"
del -f "..\app.Release.config"

REM attrib -r "%INSTDIR%\Linxens.Core.config"
REM replenv.exe %INSTDIR%\Linxens.Core.config %INSTDIR%\Linxens.Core.config

ECHO.
ECHO End of script

