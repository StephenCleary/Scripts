# A script that sets preferences, to be run once after Chocolatey is done.
#   For Local Group Policies, see https://www.microsoft.com/en-us/download/details.aspx?id=25250

function Set-Registry([string]$Path, [string]$Name, [string]$PropertyType, $Value) {
    if (!(Test-Path $Path)) {
        New-Item -Path $Path -Force | Out-Null
    }
    New-ItemProperty -Path $Path -Name $Name -Value $Value -PropertyType $PropertyType -Force | Out-Null
}

# Explorer -> View -> File Name Extensions
Write-Output "Showing file name extensions."
Set-Registry -Path HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced -Name HideFileExt -Value 0

# Administrative Templates => Network => Lanman Workstation => Enable Insecure guest logons
Write-Output "Enabling anonymous network shares."
Set-Registry -Path HKLM:\Software\Policies\Microsoft\Windows\LanmanWorkstation -Name AllowInsecureGuestAuth -Value 1

# This is like an arms war or something...
Write-Output "Disabling Cortana and Search."
Set-Registry -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search" -Name AllowCortana -Value 0
Set-Registry -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search" -Name AllowCortanaAboveLock -Value 0
Set-Registry -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search" -Name AllowCloudSearch -Value 0
Set-Registry -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search" -Name AllowSearchToUseLocation -Value 0
Set-Registry -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search" -Name ConnectedSearchUseWeb -Value 0
Set-Registry -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows\Windows Search" -Name DisableWebSearch -Value 1
Set-Registry -Path HKCU:\Software\Microsoft\Windows\CurrentVersion\Search -Name BingSearchEnabled -Value 0
Set-Registry -Path HKCU:\Software\Microsoft\Windows\CurrentVersion\Search -Name AllowSearchToUseLocation -Value 0
Set-Registry -Path HKCU:\Software\Microsoft\Windows\CurrentVersion\Search -Name CortanaConsent -Value 0

Write-Output "Disabling Start menu ads."
Set-Registry -Path HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager -Name SubscribedContent-338388Enabled -Value 0

# TODO: Uninstall Microsoft bloatware

## Final operations
Write-Output "Applying Group Policy updates..."
gpupdate /force
Write-Output "Done!"
Write-Output "Please remember to reboot."
