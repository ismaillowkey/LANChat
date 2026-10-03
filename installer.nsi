; ==============================================================================
; NSIS Modern Installer Script for LAN Chat
; ==============================================================================

!include "MUI2.nsh"
!include "FileFunc.nsh"
!include "LogicLib.nsh"

; General definitions
!define APP_NAME "LAN Chat"
!define APP_VERSION "0.2.3"
!define APP_PUBLISHER "LAN Chat P2P"
!define APP_EXE "LanChat.Wpf.exe"
!define APP_ICON "src\LanChat.Wpf\Resources\app_logo.ico"

Name "${APP_NAME} ${APP_VERSION}"
OutFile "publish\Installer\LAN_Chat_Setup_v${APP_VERSION}.exe"
InstallDir "$PROGRAMFILES\${APP_NAME}"
InstallDirRegKey HKLM "Software\${APP_NAME}" "InstallDir"
RequestExecutionLevel admin

; Visual Styles
!define MUI_ICON "${APP_ICON}"
!define MUI_UNICON "${APP_ICON}"
!define MUI_HEADERIMAGE
!define MUI_ABORTWARNING

; Language selection dialog settings
!define MUI_LANGDLL_ALWAYSSHOW

; Installer Pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES

; Finish Page with Run option
!define MUI_FINISHPAGE_RUN "$INSTDIR\${APP_EXE}"
!define MUI_FINISHPAGE_RUN_TEXT "Jalankan ${APP_NAME} sekarang / Run ${APP_NAME} now"
!insertmacro MUI_PAGE_FINISH

; Uninstaller Pages
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; Languages (Indonesian and English)
!insertmacro MUI_LANGUAGE "Indonesian"
!insertmacro MUI_LANGUAGE "English"

Function .onInit
    !insertmacro MUI_LANGDLL_DISPLAY
FunctionEnd

; ==============================================================================
; Installation Section
; ==============================================================================
Section "LAN Chat Core Files" SecMain
    SetOutPath "$INSTDIR"
    
    ; Copy all published desktop files
    File /r "publish\Desktop\*.*"
    
    ; Detect chosen installer language and save default app language
    ${If} $LANGUAGE == ${LANG_ENGLISH}
        StrCpy $0 "en"
    ${Else}
        StrCpy $0 "id"
    ${EndIf}

    ; Write config.ini in Application Directory
    WriteINIStr "$INSTDIR\config.ini" "General" "Language" "$0"
    WriteINIStr "$INSTDIR\config.ini" "General" "DeviceName" ""

    ; Also write config.ini in user AppData directory so user session immediately matches installer choice
    SetShellVarContext current
    CreateDirectory "$APPDATA\LanChat"
    WriteINIStr "$APPDATA\LanChat\config.ini" "General" "Language" "$0"

    ; Also write legacy install_config.json for backward compatibility
    FileOpen $1 "$INSTDIR\install_config.json" w
    FileWrite $1 '{"Language":"$0"}'
    FileClose $1

    ; Create uninstaller
    WriteUninstaller "$INSTDIR\Uninstall.exe"
    
    ; Create Desktop Shortcut
    CreateShortcut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\${APP_EXE}" 0
    
    ; Create Start Menu Shortcuts in dedicated LAN Chat folder
    CreateDirectory "$SMPROGRAMS\${APP_NAME}"
    CreateShortcut "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\${APP_EXE}" 0
    CreateShortcut "$SMPROGRAMS\${APP_NAME}\Uninstall ${APP_NAME}.lnk" "$INSTDIR\Uninstall.exe" "" "$INSTDIR\Uninstall.exe" 0
    
    ; Write Registry for Add/Remove Programs
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "DisplayName" "${APP_NAME}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "DisplayIcon" "$INSTDIR\${APP_EXE},0"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "DisplayVersion" "${APP_VERSION}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "Publisher" "${APP_PUBLISHER}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
    WriteRegStr HKLM "Software\${APP_NAME}" "InstallDir" "$INSTDIR"
    WriteRegStr HKLM "Software\${APP_NAME}" "Language" "$0"

    ; Calculate estimated size
    ${GetSize} "$INSTDIR" "/S=K" $0 $1 $2
    IntFmt $0 "0x%08X" $0
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "EstimatedSize" "$0"
SectionEnd

; ==============================================================================
; Uninstaller Section
; ==============================================================================
Section "Uninstall"
    ; Remove desktop shortcut
    Delete "$DESKTOP\${APP_NAME}.lnk"
    
    ; Remove start menu shortcuts
    Delete "$SMPROGRAMS\${APP_NAME}\*.*"
    RMDir "$SMPROGRAMS\${APP_NAME}"
    
    ; Remove registry keys
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}"
    DeleteRegKey HKLM "Software\${APP_NAME}"
    
    ; Remove installed files & directory
    Delete "$INSTDIR\config.ini"
    Delete "$INSTDIR\install_config.json"
    RMDir /r "$INSTDIR"
SectionEnd
