# A script that sets up a new computer.
#   For Local Group Policies, see https://www.microsoft.com/en-us/download/details.aspx?id=25250
#   For power configuration, see the output of `powercfg -Q`

function Set-Registry([string]$Path, [string]$Name, [string]$PropertyType, $Value) {
    if (!(Test-Path $Path)) {
        New-Item -Path $Path -Force | Out-Null
    }
    New-ItemProperty -Path $Path -Name $Name -Value $Value -PropertyType $PropertyType -Force | Out-Null
}

# Set PowerShell execution policy
Write-Output "Setting PowerShell execution policy to RemoteSigned."
Set-ExecutionPolicy RemoteSigned -Force

# Do not sleep when plugged in
Write-Output "Disabling power-off when plugged in."
powercfg /change standby-timeout-ac 0
powercfg /change hibernate-timeout-ac 0

# Do not turn off the PC when I close the lid
Write-Output "Disabling power-off when lid is closed."
powercfg -SETACVALUEINDEX SCHEME_BALANCED SUB_BUTTONS LIDACTION 0
powercfg -SETDCVALUEINDEX SCHEME_BALANCED SUB_BUTTONS LIDACTION 0

# Explorer -> View -> File Name Extensions
Write-Output "Showing file name extensions."
Set-Registry -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" -Name HideFileExt -Value 0

# No Sounds scheme
Write-Output "Turning off Windows sounds."
Set-Registry -Path "HKCU:\AppEvents\Schemes" -Name "(Default)" -Value ".None"
Get-ChildItem -Path "HKCU:\AppEvents\Schemes\Apps" | Get-ChildItem | Get-ChildItem | Where-Object {$_.PSChildName -eq ".Current"} | Set-ItemProperty -Name "(Default)" -Value ""

# Administrative Templates => Network => Lanman Workstation => Enable Insecure guest logons
Write-Output "Enabling anonymous network shares."
Set-Registry -Path "HKLM:\Software\Policies\Microsoft\Windows\LanmanWorkstation" -Name AllowInsecureGuestAuth -Value 1

# Turn off Bluetooth
Write-Output "Turning off Bluetooth..."
#   https://superuser.com/a/1293303/43669
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$winrtAsTaskWithResult = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]
Function WinrtWaitFor($WinRtTask, $ResultType) {
    $asTask = $winrtAsTaskWithResult.MakeGenericMethod($ResultType)
    $netTask = $asTask.Invoke($null, @($WinRtTask))
    $netTask.Wait(-1) | Out-Null
    $netTask.Result
}
[Windows.Devices.Radios.Radio,Windows.System.Devices,ContentType=WindowsRuntime] | Out-Null
[Windows.Devices.Radios.RadioAccessStatus,Windows.System.Devices,ContentType=WindowsRuntime] | Out-Null
WinrtWaitFor ([Windows.Devices.Radios.Radio]::RequestAccessAsync()) ([Windows.Devices.Radios.RadioAccessStatus]) | Out-Null
$radios = WinrtWaitFor ([Windows.Devices.Radios.Radio]::GetRadiosAsync()) ([System.Collections.Generic.IReadOnlyList[Windows.Devices.Radios.Radio]])
$bluetooth = $radios | Where-Object { $_.Kind -eq 'Bluetooth' }
[Windows.Devices.Radios.RadioState,Windows.System.Devices,ContentType=WindowsRuntime] | Out-Null
WinrtWaitFor ($bluetooth.SetStateAsync('Off')) ([Windows.Devices.Radios.RadioAccessStatus]) | Out-Null

# This is like an arms war or something...
Write-Output "Disabling Cortana and Web search from the taskbar."
Set-Registry -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search" -Name AllowCortana -Value 0
Set-Registry -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search" -Name AllowCortanaAboveLock -Value 0
Set-Registry -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search" -Name AllowCloudSearch -Value 0
Set-Registry -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search" -Name AllowSearchToUseLocation -Value 0
Set-Registry -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search" -Name ConnectedSearchUseWeb -Value 0
Set-Registry -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search" -Name DisableWebSearch -Value 1
Set-Registry -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Search" -Name BingSearchEnabled -Value 0
Set-Registry -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Search" -Name AllowSearchToUseLocation -Value 0
Set-Registry -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Search" -Name CortanaConsent -Value 0

Write-Output "Disabling Start menu ads."
Set-Registry -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" -Name SubscribedContent-338388Enabled -Value 0
Set-Registry -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" -Name SystemPaneSuggestionsEnabled -Value 0

Write-Output "Disabling Taskbar ads."
Set-Registry -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" -Name SubscribedContent-338389Enabled -Value 0

Write-Output "Disabling replacement app ads."
Set-Registry -Path "HKLM:\Software\Microsoft\Windows\CurrentVersion\Explorer" -Name AicEnabled -Value "Anywhere"

Write-Output "Disabling lock screen ads."
Set-Registry -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" -Name RotatingLockScreenEnabled -Value 0
Set-Registry -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" -Name RotatingLockScreenOverlayEnabled -Value 0

Write-Output "Uninstalling Microsoft bloatware."
Get-AppxPackage Microsoft.Microsoft3DViewer | Remove-AppxPackage
Get-AppxPackage king.com.CandyCrushSaga | Remove-AppxPackage
Get-AppxPackage NORDCURRENT.COOKINGFEVER | Remove-AppxPackage
Get-AppxPackage Fitbit.FitbitCoach | Remove-AppxPackage
Get-AppxPackage Microsoft.XboxGameOverlay | Remove-AppxPackage
Get-AppxPackage Microsoft.XboxGamingOverlay | Remove-AppxPackage
Get-AppxPackage Microsoft.ZuneMusic | Remove-AppxPackage # Groove music
Get-AppxPackage Microsoft.BingNews | Remove-AppxPackage # Microsoft news
Get-AppxPackage Microsoft.MixedReality.Portal | Remove-AppxPackage
Get-AppxPackage Microsoft.OneConnect | Remove-AppxPackage # Mobile plans
Get-AppxPackage Microsoft.MSPaint | Remove-AppxPackage # Paint 3D
Get-AppxPackage ThumbmunkeysLtd.PhototasticCollage | Remove-AppxPackage
Get-AppxPackage Microsoft.Print3D | Remove-AppxPackage
Get-AppxPackage Microsoft.MicrosoftStickyNotes | Remove-AppxPackage
Get-AppxPackage Microsoft.Getstarted | Remove-AppxPackage # Tips
Get-AppxPackage Microsoft.BingWeather | Remove-AppxPackage # Weather
Get-AppxPackage Microsoft.XboxSpeechToTextOverlay | Remove-AppxPackage # XBox Game Speech Window
Get-AppxPackage Microsoft.XboxApp | Remove-AppxPackage # XBox
Get-AppxPackage Microsoft.Xbox.TCUI | Remove-AppxPackage # XBox Live
Get-AppxPackage Microsoft.YourPhone | Remove-AppxPackage
Get-AppxPackage Microsoft.MicrosoftSolitaireCollection | Remove-AppxPackage

## Apply all preferences
Write-Output "Applying Group Policy updates."
gpupdate /force

## Install PowerShell modules
Install-PackageProvider -Name NuGet -Force
Install-Module -Name Az -AllowClobber -Force

## Install Chocolatey
Write-Output "Installing Chocolatey."
Set-ExecutionPolicy Bypass -Scope Process -Force; Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
. $profile
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

## Install Chocolatey packages
Write-Output "Installing applications."
choco install -y .\choco.config
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

## Install Node utilities
npm install -g npm-check-updates

# Enable Hyper-V
Write-Output "Enabling Hyper-V."
Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All -NoRestart

Write-Output "Done! Please reboot."
