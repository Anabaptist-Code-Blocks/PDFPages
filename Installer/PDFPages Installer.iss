; Script generated by the Inno Script Studio Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

[Setup]
SignTool=MsSign $f
SignedUninstaller=yes
AppName=PDF Pages
AppVersion=1.0.2
UninstallDisplayName=PDF Pages
UninstallDisplaySize=192364544
DefaultDirName={commonpf}\PDF Pages
OutputDir=..\Installer\Output
OutputBaseFilename=PDF Pages
DisableReadyMemo=True
DisableReadyPage=True
DisableProgramGroupPage=true
SetupIconFile=..\resources\appicon.ico
UninstallDisplayIcon={uninstallexe}
AllowNoIcons=yes
SolidCompression=yes
Compression=lzma
AppId={{F44D8949-C82A-4E7B-B3EE-A1D7C078D979}

[Files]
Source: "..\bin\Release\net5.0-windows\publish\*"; DestDir: "{app}"; Flags: recursesubdirs

[Registry]
Root: "HKCR"; Subkey: "SystemFileAssociations\.pdf\shell\Split PDF"; ValueType: string; ValueName: "MultiSelectModel"; ValueData: "Single"; Flags: deletekey uninsdeletekey
Root: "HKCR"; Subkey: "SystemFileAssociations\.pdf\shell\Split PDF\command"; ValueType: string; ValueData: """{app}\PDFPages.exe"" split ""%1"""; Flags: deletekey uninsdeletekey

[Icons]
Name: "{usersendto}\Merge PDF Files"; Filename: "{app}\PDFPages.exe"; Parameters: "merge"
Name: "{autoprograms}\Pdf Pages"; Filename: "{app}\PDFPages.exe"
