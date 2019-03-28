#define MyAppID "{FIAutoDataEntry}"
#define MyAppName "FIAutoDataEntry"
#define MyAppVersion "1.0.1.0"
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
  AlertMailInfoPage: TInputQueryWizardPage;
  GlobalParam2Page: TInputQueryWizardPage;
  GlobalParamPage: TInputDirWizardPage;
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
      'QAD web service', 'What is the QAD web service informations?',
      '');
    QadPage.Add('QAD Werbservice Url:', False);
    QadPage.Add('User:', False);
    QadPage.Add('Domain:', False);
    QadPage.Add('Auth key:', False);

  { OtherInfo }
    AlertMailInfoPage := CreateInputQueryPage(wpWelcome,
      'Alert mail parameters', 'What is the alert mail parameters?',
      '');
    AlertMailInfoPage.Add('User mail TO (use for errors notifications):', False);
    AlertMailInfoPage.Add('User mail FROM (use for errors notifications):', False);
    AlertMailInfoPage.Add('SMTP server:', False);
    AlertMailInfoPage.Add('SMTP Port:', False);

    AlertMailInfoPage.Values[0] := '';
    AlertMailInfoPage.Values[1] := 'FiAutoDataEntry@linxens.com';
    AlertMailInfoPage.Values[2] := 'smtp.linxens.com';
    AlertMailInfoPage.Values[3] := '25';

  { OtherInfo 2 }
    GlobalParam2Page := CreateInputQueryPage(wpWelcome,
      'Global parameters', 'What is the global parameters ?',
      '');
    GlobalParam2Page.Add('Password (use to activate data edit):', True);
    GlobalParam2Page.Add('Number of retry when QAD send error :', False);

    GlobalParam2Page.Values[1] := '2';

  { DirectoryPage }
    GlobalParamPage := CreateInputDirPage(wpWelcome,
      'Global parameters', 'What is the global parameters ?',
      '', False, 'New Folder');
    GlobalParamPage.Add('Root directory (where FI machine put files):');
    GlobalParamPage.Add('Working directory (where this application save FI machine files):');
    GlobalParamPage.Add('Logs directory:');

    GlobalParamPage.Values[1] := ExpandConstant('{userappdata}\Data\{#MyAppName}');
    GlobalParamPage.Values[2] := ExpandConstant('{userappdata}\Logs\{#MyAppName}');

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
  if(CurPageID = GlobalParam2Page.ID)
      AND ((GlobalParam2Page.Values[0] = '')
      OR (GlobalParam2Page.Values[1] = ''))
  then
      begin
        Result := False;
  end;
  if(CurPageID = AlertMailInfoPage.ID)
      AND ((AlertMailInfoPage.Values[0] = '')
      OR (AlertMailInfoPage.Values[1] = '')
      OR (AlertMailInfoPage.Values[2] = '')
      OR (AlertMailInfoPage.Values[3] = ''))
  then
      begin
        Result := False;
  end;
  if(CurPageID = GlobalParamPage.ID)
      AND ((GlobalParamPage.Values[0] = '')
      OR (GlobalParamPage.Values[1] = '')
      OR (GlobalParamPage.Values[2] = ''))
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

        ProgressPage := CreateOutputProgressPage('Installation...','Application configuration in progress. Please wait.');

        ProgressPage.SetText('Registering web application and pool information...', '');
        ProgressPage.SetProgress(0, 0);
        ProgressPage.Show;        

        //try
          		  
          Exec(ExpandConstant('{app}')+'\Inst\postConfig.bat ',
          '"-user:{'+QadPage.Values[1]+'} '
          +'-password:{'+QadPage.Values[3]+'} '
          +'-domain:{'+QadPage.Values[2]+'} '
          +'-qadurl:{'+QadPage.Values[0]+'} '
          +'-rootdir:{'+GlobalParamPage.Values[0]+'} '
          +'-workdir:{'+GlobalParamPage.Values[1]+'} '
          +'-logdir:{'+GlobalParamPage.Values[2]+'} '
          +'-autoRetry:{'+GlobalParam2Page.Values[1]+'} '
          +'-mailTo:{'+AlertMailInfoPage.Values[0]+'} '
          +'-mailFrom:{'+AlertMailInfoPage.Values[1]+'} '
          +'-mailServer:{'+AlertMailInfoPage.Values[2]+'} '
          +'-mailPort:{'+AlertMailInfoPage.Values[3]+'} '
          +'-editPasswd:{'+GlobalParam2Page.Values[0]+'} '
          +'"' ,
          '', SW_SHOW, ewWaitUntilTerminated, ResultCode);

          ProgressPage.SetProgress(10, 10); 

        //finally
          ProgressPage.Hide;
        //end;
    end;
end;