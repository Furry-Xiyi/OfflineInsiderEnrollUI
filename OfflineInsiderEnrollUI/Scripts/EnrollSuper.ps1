# EnrollSuper.ps1
# 100% 等价 OfflineInsiderEnroll.cmd 的 PowerShell 版本（模板）
# 占位符：__CHANNEL__, __FANCY__, __CONTENT__, __RING__, __RID__, __SCRIPTVER__

$ErrorActionPreference = 'Continue'

function Set-RegValue {
    param($Path, $Name, $Value, $Type = 'DWord')
    try {
        if (-not (Test-Path $Path)) {
            New-Item -Path $Path -Force | Out-Null
        }
        if ($Type -eq 'String') {
            New-ItemProperty -Path $Path -Name $Name -Value $Value -PropertyType String -Force | Out-Null
        } else {
            New-ItemProperty -Path $Path -Name $Name -Value $Value -PropertyType DWord -Force | Out-Null
        }
    } catch {
        # 静默忽略写入错误
    }
}

function Remove-RegKey {
    param($Path)
    try {
        if (Test-Path $Path) {
            Remove-Item -Path $Path -Recurse -Force -ErrorAction SilentlyContinue
        }
    } catch {
        # 忽略
    }
}

function Remove-RegValue {
    param($Path, $Name)
    try {
        if (Test-Path $Path) {
            Remove-ItemProperty -Path $Path -Name $Name -ErrorAction SilentlyContinue
        }
    } catch {
        # 忽略
    }
}

# 获取系统 build（尽量与 CMD 脚本行为一致）
try {
    $build = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion' -ErrorAction Stop).CurrentBuild
    $build = [int]$build
} catch {
    $build = 0
}

if ($build -lt 17763) {
    Write-Output "This script is compatible only with Windows 10 v1809 and later. Build: $build"
    return
}

# 记录原始 FlightSigning 状态（尽量与 CMD 行为一致）
$FlightSigningEnabled = 0
try {
    $bcd = & bcdedit /enum {current} 2>$null
    if ($bcd -match 'flightsigning\s+Yes') {
        $FlightSigningEnabled = 1
    }
} catch {
    # 忽略
}

# ============================
# RESET_INSIDER_CONFIG
# ============================

Remove-RegKey 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Account'
Remove-RegKey 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Applicability'
Remove-RegKey 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Cache'
Remove-RegKey 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\ClientState'
Remove-RegKey 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\UI'
Remove-RegKey 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Restricted'
Remove-RegKey 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\ToastNotification'
Remove-RegKey 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\SLS\Programs\WUMUDCat'
Remove-RegKey "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\SLS\Programs\Ring__RING__"
Remove-RegKey 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\SLS\Programs\RingExternal'
Remove-RegKey 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\SLS\Programs\RingPreview'
Remove-RegKey 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\SLS\Programs\RingInsiderSlow'
Remove-RegKey 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\SLS\Programs\RingInsiderFast'

Remove-RegValue 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection' 'AllowTelemetry'
Remove-RegValue 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection' 'AllowTelemetry'
Remove-RegValue 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate' 'BranchReadinessLevel'
Remove-RegValue 'HKLM:\SYSTEM\Setup\WindowsUpdate' 'AllowWindowsUpdate'
Remove-RegValue 'HKLM:\SYSTEM\Setup\MoSetup' 'AllowUpgradesWithUnsupportedTPMOrCPU'
Remove-RegValue 'HKLM:\SYSTEM\Setup\LabConfig' 'BypassRAMCheck'
Remove-RegValue 'HKLM:\SYSTEM\Setup\LabConfig' 'BypassSecureBootCheck'
Remove-RegValue 'HKLM:\SYSTEM\Setup\LabConfig' 'BypassStorageCheck'
Remove-RegValue 'HKLM:\SYSTEM\Setup\LabConfig' 'BypassTPMCheck'
Remove-RegValue 'HKCU:\SOFTWARE\Microsoft\PCHC' 'UpgradeEligibility'

# ============================
# ADD_INSIDER_CONFIG
# ============================

Set-RegValue 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Orchestrator' 'EnableUUPScan' 1
Set-RegValue "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\SLS\Programs\Ring__RING__" 'Enabled' 1
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\SLS\Programs\WUMUDCat' 'WUMUDCATEnabled' 1

Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Applicability' 'EnablePreviewBuilds' 2
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Applicability' 'IsBuildFlightingEnabled' 1
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Applicability' 'IsConfigSettingsFlightingEnabled' 1
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Applicability' 'IsConfigExpFlightingEnabled' 0
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Applicability' 'TestFlags' 32
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Applicability' 'RingId' __RID__
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Applicability' 'Ring' '__RING__' 'String'
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Applicability' 'ContentType' '__CONTENT__' 'String'
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Applicability' 'BranchName' '__CHANNEL__' 'String'

if ($build -lt 21990) {
    $xaml = '<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"><TextBlock Style="{StaticResource BodyTextBlockStyle }">This device has been enrolled to the Windows Insider program using OfflineInsiderEnroll v__SCRIPTVER__. If you want to change settings of the enrollment or stop receiving Windows Insider builds, please use the script. <Hyperlink NavigateUri="https://github.com/abbodi1406/offlineinsiderenroll" TextDecorations="None">Learn more</Hyperlink></TextBlock><TextBlock Text="Applied configuration" Margin="0,20,0,10" Style="{StaticResource SubtitleTextBlockStyle}" /><TextBlock Style="{StaticResource BodyTextBlockStyle }" Margin="0,0,0,5"><Run FontFamily="Segoe MDL2 Assets">&#xECA7;</Run> <Span FontWeight="SemiBold">__FANCY__</Span></TextBlock><TextBlock Text="Channel: __CHANNEL__" Style="{StaticResource BodyTextBlockStyle }" /><TextBlock Text="Content: __CONTENT__" Style="{StaticResource BodyTextBlockStyle }" /><TextBlock Text="Telemetry settings notice" Margin="0,20,0,10" Style="{StaticResource SubtitleTextBlockStyle}" /><TextBlock Style="{StaticResource BodyTextBlockStyle }">Windows Insider Program requires your diagnostic data collection settings to be set to <Span FontWeight="SemiBold">Full</Span>. You can verify or modify your current settings in <Span FontWeight="SemiBold">Diagnostics &amp; feedback</Span>.</TextBlock><Button Command="{StaticResource ActivateUriCommand}" CommandParameter="ms-settings:privacy-feedback" Margin="0,10,0,0"><TextBlock Margin="5,0,5,0">Open Diagnostics &amp; feedback</TextBlock></Button></StackPanel>'
    Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\UI\Strings' 'StickyXaml' $xaml 'String'
}

Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\UI\Visibility' 'UIHiddenElements' 65535
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\UI\Visibility' 'UIDisabledElements' 65535
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\UI\Visibility' 'UIServiceDrivenElementVisibility' 0
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\UI\Visibility' 'UIErrorMessageVisibility' 192
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection' 'AllowTelemetry' 3
if ($env:BRL) {
    # BRL 由外部追加或 C# 追加
    Set-RegValue 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate' 'BranchReadinessLevel' ([int]$env:BRL)
}
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\UI\Visibility' 'UIHiddenElements_Rejuv' 65534
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\UI\Visibility' 'UIDisabledElements_Rejuv' 65535

Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\UI\Selection' 'UIRing' '__RING__' 'String'
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\UI\Selection' 'UIContentType' '__CONTENT__' 'String'
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\UI\Selection' 'UIBranch' '__CHANNEL__' 'String'
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\UI\Selection' 'UIOptin' 1
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Applicability' 'RingBackup' '__RING__' 'String'
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Applicability' 'RingBackupV2' '__RING__' 'String'
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Applicability' 'BranchBackup' '__CHANNEL__' 'String'

Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Cache' 'PropertyIgnoreList' 'AccountsBlob;;CTACBlob;FlightIDBlob;ServiceDrivenActionResults' 'String'
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Cache' 'RequestedCTACAppIds' 'WU;FSS' 'String'

Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Account' 'SupportedTypes' 3
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Account' 'Status' 8

Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Applicability' 'UseSettingsExperience' 0
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\ClientState' 'AllowFSSCommunications' 0
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\ClientState' 'UICapabilities' 1
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\ClientState' 'IgnoreConsolidation' 1
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\ClientState' 'MsaUserTicketHr' 0
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\ClientState' 'MsaDeviceTicketHr' 0
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\ClientState' 'ValidateOnlineHr' 0
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\ClientState' 'LastHR' 0
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\ClientState' 'ErrorState' 0
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\ClientState' 'PilotInfoRing' 3
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\ClientState' 'RegistryAllowlistVersion' 4
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\ClientState' 'FileAllowlistVersion' 1

Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\UI' 'UIControllableState' 0
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\UI\Selection' 'UIDialogConsent' 0
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\UI\Selection' 'UIUsage' 26
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\UI\Selection' 'OptOutState' 25
Set-RegValue 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\UI\Selection' 'AdvancedToggleState' 24

Set-RegValue 'HKLM:\SYSTEM\Setup\WindowsUpdate' 'AllowWindowsUpdate' 1
Set-RegValue 'HKLM:\SYSTEM\Setup\MoSetup' 'AllowUpgradesWithUnsupportedTPMOrCPU' 1
Set-RegValue 'HKLM:\SYSTEM\Setup\LabConfig' 'BypassRAMCheck' 1
Set-RegValue 'HKLM:\SYSTEM\Setup\LabConfig' 'BypassSecureBootCheck' 1
Set-RegValue 'HKLM:\SYSTEM\Setup\LabConfig' 'BypassStorageCheck' 1
Set-RegValue 'HKLM:\SYSTEM\Setup\LabConfig' 'BypassTPMCheck' 1
Set-RegValue 'HKCU:\SOFTWARE\Microsoft\PCHC' 'UpgradeEligibility' 1

# 如果 build >= 21990，写入 StickyMessage（REG 文件导入）
if ($build -ge 21990) {
    try {
        $regPath = Join-Path $env:SystemRoot 'oie.reg'
        $regContent = @'
Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsSelfHost\UI\Strings]
"StickyMessage"="{\"Message\":\"Device Enrolled Using OfflineInsiderEnroll\",\"LinkTitle\":\"\",\"LinkUrl\":\"\",\"DynamicXaml\":\"^<StackPanel xmlns=\\\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\\\"^>^<TextBlock Style=\\\"{StaticResource BodyTextBlockStyle }\\\"^>This device has been enrolled to the Windows Insider program using OfflineInsiderEnroll v__SCRIPTVER__. If you want to change settings of the enrollment or stop receiving Windows Insider builds, please use the script. ^<Hyperlink NavigateUri=\\\"https://github.com/abbodi1406/offlineinsiderenroll\\\" TextDecorations=\\\"None\\\"^>Learn more^</Hyperlink^>^</TextBlock^>^<TextBlock Text=\\\"Applied configuration\\\" Margin=\\\"0,20,0,10\\\" Style=\\\"{StaticResource SubtitleTextBlockStyle}\\\" /^>^<TextBlock Style=\\\"{StaticResource BodyTextBlockStyle }\\\" Margin=\\\"0,0,0,5\\\"^>^<Run FontFamily=\\\"Segoe MDL2 Assets\\\"^>^&#xECA7;^</Run^> ^<Span FontWeight=\\\"SemiBold\\\"^>__FANCY__^</Span^>^</TextBlock^>^<TextBlock Text=\\\"Channel: __CHANNEL__\\\" Style=\\\"{StaticResource BodyTextBlockStyle }\\\" /^>^<TextBlock Text=\\\"Content: __CONTENT__\\\" Style=\\\"{StaticResource BodyTextBlockStyle }\\\" /^>^<TextBlock Text=\\\"Telemetry settings notice\\\" Margin=\\\"0,20,0,10\\\" Style=\\\"{StaticResource SubtitleTextBlockStyle}\\\" /^>^<TextBlock Style=\\\"{StaticResource BodyTextBlockStyle }\\\"^>Windows Insider Program requires your diagnostic data collection settings to be set to ^<Span FontWeight=\\\"SemiBold\\\"^>Full^</Span^>. You can verify or modify your current settings in ^<Span FontWeight=\\\"SemiBold\\\"^>Diagnostics ^&amp; feedback^</Span^>.^</TextBlock^>^<Button Command=\\\"{StaticResource ActivateUriCommand}\\\" CommandParameter=\\\"ms-settings:privacy-feedback\\\" Margin=\\\"0,10,0,0\\\"^>^<TextBlock Margin=\\\"5,0,5,0\\\"^>Open Diagnostics ^&amp; feedback^</TextBlock^>^</Button^>^</StackPanel^>\",\"Severity\":0}"
'@
        # 替换占位符
        $regContent = $regContent -replace '__FANCY__', '__FANCY__'  # 占位符保留，C# 替换
        $regContent = $regContent -replace '__CHANNEL__', '__CHANNEL__'
        $regContent = $regContent -replace '__CONTENT__', '__CONTENT__'
        $regContent = $regContent -replace '__SCRIPTVER__', '__SCRIPTVER__'

        $regContent | Out-File -FilePath $regPath -Encoding Unicode -Force
        & reg.exe import $regPath | Out-Null
        Remove-Item $regPath -Force -ErrorAction SilentlyContinue
    } catch {
        # 忽略导入错误
    }
}

# ENABLE FLIGHT SIGNING（静默忽略不支持）
try {
    cmd /c "bcdedit /set {current} flightsigning yes" 2>$null
} catch {
    # 忽略
}

# 完成
Write-Output "EnrollSuper.ps1: applied configuration. FlightSigningEnabled: $FlightSigningEnabled"