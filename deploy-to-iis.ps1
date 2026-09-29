# Powershell Deployment Script for EduPulse ASP.NET Core API to IIS Enterprise Server
# Execute with Administrator Privileges

Param(
    [string]$SiteName = "EduPulseEdTech",
    [string]$AppPoolName = "EduPulseAppPool",
    [int]$Port = 8088,
    [string]$PublishDirectory = "C:\inetpub\wwwroot\EduPulseApi"
)

Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host "  EduPulse EdTech Platform - IIS Deployment Script      " -ForegroundColor Cyan
Write-Host "=========================================================" -ForegroundColor Cyan

# 1. Check for IIS Module
If (-not (Get-Module -ListAvailable -Name WebAdministration)) {
    Write-Error "IIS WebAdministration PowerShell module is missing. Please enable Web-Server (IIS) role."
    Exit 1
}

Import-Module WebAdministration

# 2. Build & Publish ASP.NET Core Project
Write-Host "`n[1/4] Publishing ASP.NET Core Release binaries..." -ForegroundColor Yellow
dotnet publish -c Release -o $PublishDirectory

If ($LASTEXITCODE -ne 0) {
    Write-Error "Dotnet publish failed. Deployment aborted."
    Exit 1
}

# 3. Create / Configure IIS Application Pool
Write-Host "`n[2/4] Configuring IIS Application Pool: $AppPoolName..." -ForegroundColor Yellow
If (Test-Path "IIS:\AppPools\$AppPoolName") {
    Write-Host "  AppPool '$AppPoolName' already exists. Reusing..." -ForegroundColor Gray
} Else {
    New-WebAppPool -Name $AppPoolName
    Write-Host "  Created AppPool '$AppPoolName'" -ForegroundColor Green
}

# ASP.NET Core requires No Managed Code in AppPool
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"

# 4. Create IIS Web Site
Write-Host "`n[3/4] Creating IIS Site '$SiteName' on Port $Port..." -ForegroundColor Yellow
If (Test-Path "IIS:\Sites\$SiteName") {
    Write-Host "  Site '$SiteName' exists. Updating physical path..." -ForegroundColor Gray
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name "physicalPath" -Value $PublishDirectory
} Else {
    New-Website -Name $SiteName -Port $Port -PhysicalPath $PublishDirectory -ApplicationPool $AppPoolName
    Write-Host "  Successfully created IIS Website '$SiteName'" -ForegroundColor Green
}

# 5. Set NTFS Permissions for AppPool Identity
Write-Host "`n[4/4] Setting File Permissions for AppPoolIdentity..." -ForegroundColor Yellow
$Acl = Get-Acl $PublishDirectory
$Ar = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS AppPool\$AppPoolName", "FullControl", "ContainerInherit, ObjectInherit", "None", "Allow")
$Acl.SetAccessRule($Ar)
Set-Acl $PublishDirectory $Acl

Write-Host "`n=========================================================" -ForegroundColor Green
Write-Host "  Deployment Completed Successfully!" -ForegroundColor Green
Write-Host "  Access Website: http://localhost:$Port" -ForegroundColor Green
Write-Host "  Swagger Docs:   http://localhost:$Port/swagger" -ForegroundColor Green
Write-Host "=========================================================" -ForegroundColor Green
