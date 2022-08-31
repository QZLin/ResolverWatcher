;NSIS Modern User Interface
;--------------------------------
;Include Modern UI
!include "MUI2.nsh"

;--------------------------------
;General
;Name and file
Name "Resolver Watcher"
OutFile "bin\installer.exe"
Unicode True

;Default installation folder
InstallDir "$PROGRAMFILES\$(^Name)"

;Get installation folder from registry if available
InstallDirRegKey HKCU "Software\$(^Name)" ""

;Request application privileges for Windows Vista
RequestExecutionLevel admin

;OnINIT
Function .onInit
FunctionEnd
;--------------------------------
;Interface Settings

!define MUI_ABORTWARNING

;--------------------------------
;Pages

!insertmacro MUI_PAGE_LICENSE "LICENSE"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
  
;--------------------------------
;Languages
 
!insertmacro MUI_LANGUAGE "English"

;--------------------------------
;Installer Sections

Section "Resolver Watcher" SecDummy
    nsExec::Exec "$INSTDIR\winsw install"
    nsExec::Exec "net stop rswatcher"
    SetOutPath "$INSTDIR"
  
    ;ADD YOUR OWN FILES HERE...
    File bin\Release\*.exe
    File winsw\*.*
    File LICENSE

    CreateShortcut "$DESKTOP\Unlocker.lnk" "$INSTDIR\unlocker.exe"
  
    ;Store installation folder
    WriteRegStr HKCU "Software\$(^Name) Installation" "" $INSTDIR
  
    ;Create uninstaller
    WriteUninstaller "$INSTDIR\Uninstall.exe"

SectionEnd

;BeforeExit
Function .onInstSuccess
    nsExec::Exec "net start rswatcher"
FunctionEnd

;--------------------------------
;Descriptions

;Language strings
LangString DESC_SecDummy ${LANG_ENGLISH} "A test section."

;Assign language strings to sections
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
!insertmacro MUI_DESCRIPTION_TEXT ${SecDummy} $(DESC_SecDummy)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

;--------------------------------
;Uninstaller Section

Section "Uninstall"
    nsExec::Exec "net stop rswatcher"
    nsExec::Exec "winsw uninstall"

    Delete "$INSTDIR\Uninstall.exe"
    Delete "$INSTDIR\winsw.exe"
    Delete "$INSTDIR\ResolverWatcher.exe"
    Delete "$INSTDIR\Unlocker.exe"
    Delete "$INSTDIR\LICENSE"
    Delete "$INSTDIR\winsw.xml"
    RMDir "$INSTDIR"
    DeleteRegKey /ifempty HKCU "Software\$(^Name) Installation"

SectionEnd
