#define MyAppID "{FIAutoDataEntry}"
#define MyAppName "FIAutoDataEntry"
#define MyAppVersion "1.0.0.0"
#define MyAppPublisher "Viseo Technologies"
#define MyAppExeName "Linxens.Gui.exe"

[Setup]
AppId={{85A7E014-8C9E-41A1-8EEA-EBA59286F992}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppComments={#MyAppName} Application
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={pf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=Out
OutputBaseFilename={#MyAppName}.Setup{#MyAppVersion}
Compression=lzma2
SolidCompression=yes

[Files]
Source: "..\Sources\Linxens.Gui\bin\x86\Release\Linxens.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Sources\Linxens.Gui\bin\x86\Release\Linxens.Gui.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Sources\Linxens.Gui\bin\x86\Release\Linxens.Gui.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Sources\Linxens.Gui\bin\x86\Release\app.Release.config"; DestDir: "{app}"; Flags: ignoreversion
Source: ".\Inst\postConfig.bat"; DestDir: "{app}\Inst"; Flags: ignoreversion
Source: ".\Inst\replenv.exe"; DestDir: "{app}\Inst"; Flags: ignoreversion

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Code]
 var
  QadPage: TInputQueryWizardPage;
  DirectoryPage: TInputDirWizardPage;
  InstallFolderPage: TInputDirWizardPage;
  ProgressPage: TOutputProgressWizardPage;

function ProductInstalled(): Boolean;
begin
  Result := RegKeyExists(HKEY_LOCAL_MACHINE,
  'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppID}_is1');
end;

procedure InitializeWizard;
begin
  { Create the pages }
  
  { QadPage }
    QadPage := CreateInputQueryPage(wpWelcome,
      'QAD web services', 'What is the QAD web services informations?',
      '');
    QadPage.Add('QAD Url:', False);
    QadPage.Add('User:', False);
    QadPage.Add('Domain:', False);
    QadPage.Add('Auth key:', False);

  { DirectoryPage }
    DirectoryPage := CreateInputDirPage(wpWelcome,
      'Working directories', 'Please fill the working directories informations',
      '', False, 'New Folder');
    DirectoryPage.Add('Root directory (where FI machine put files):');
    DirectoryPage.Add('Working directory (where this applications save FI files):');
    DirectoryPage.Add('Logs directory:');

    DirectoryPage.Values[1] := ExpandConstant('{userappdata}\Data\{#MyAppName}');
    DirectoryPage.Values[2] := ExpandConstant('{userappdata}\Logs\{#MyAppName}');


end;

// Check if required field are filled
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if(CurPageID = QadPage.ID)
      AND ((QadPage.Values[0] = '')
      OR (QadPage.Values[1] = '')
      OR (QadPage.Values[2] = '')
      OR (QadPage.Values[3] = ''))
  then
      begin
        Result := False;
      end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  uninstaller: String;
  ErrorCode: Integer; 
begin
  if CurStep=ssInstall then
    begin
	  if ProductInstalled then
		begin
			// remove old version
			RegQueryStringValue(HKEY_LOCAL_MACHINE,
			'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppID}_is1',
			'UninstallString', uninstaller);
			ShellExec('runas', uninstaller, '/VERYSILENT /SUPPRESSMSGBOXES', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
		end;
	end; 

  if  CurStep=ssPostInstall then
    begin
		
        WizardForm.StatusLabel.Caption := 'Installation...';

        ProgressPage := CreateOutputProgressPage('Installation...','Web application and pool configuration in progress. Please wait.');

        ProgressPage.SetText('Registering web application and pool information...', '');
        ProgressPage.SetProgress(0, 0);
        ProgressPage.Show;        

        //try
          		  
          Exec(ExpandConstant('{app}')+'\Inst\postConfig.bat ',
          '"-user:{'+QadPage.Values[1]+'} '
          +'-password:{'+QadPage.Values[3]+'} '
          +'-domain:{'+QadPage.Values[2]+'} '
          +'-qadurl:{'+QadPage.Values[0]+'} '
          +'-rootdir:{'+DirectoryPage.Values[0]+'} '
          +'-workdir:{'+DirectoryPage.Values[1]+'} '
          +'-logdir:{'+DirectoryPage.Values[2]+'} '
          +'"' ,
          '', SW_SHOW, ewWaitUntilTerminated, ResultCode);

          ProgressPage.SetProgress(10, 10); 

        //finally
          ProgressPage.Hide;
        //end;
    end;
end;