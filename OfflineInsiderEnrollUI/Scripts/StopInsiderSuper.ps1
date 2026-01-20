# StopInsiderSuper.ps1
# 对应 OfflineInsiderEnroll.cmd 的 STOP_INSIDER（Reset + delete flightsigning）

$ErrorActionPreference = 'Continue'

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

# RESET_INSIDER_CONFIG（与 Enroll 相同的删除项）
Remove-RegKey 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Account'
Remove-RegKey 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Applicability'
Remove-RegKey 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Cache'
Remove-RegKey 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\ClientState'
Remove-RegKey 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\UI'
Remove-RegKey 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\Restricted'
Remove-RegKey 'HKLM:\SOFTWARE\Microsoft\WindowsSelfHost\ToastNotification'
Remove-RegKey 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\SLS\Programs\WUMUDCat'
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

# DISABLE FLIGHT SIGNING（静默忽略不支持）
try {
    cmd /c "bcdedit /deletevalue {current} flightsigning" 2>$null
} catch {
    # 忽略
}

Write-Output "StopInsiderSuper.ps1: reset applied."