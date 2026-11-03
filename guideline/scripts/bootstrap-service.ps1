<#
.SYNOPSIS
    Tu dong hoa khoi tao (bootstrap) mot microservice moi tu mau chuan .NET 10.

.DESCRIPTION
    Kich ban nay sao chep mau kien truc phan tang tu templates/dotnet-service,
    doi ten toan bo namespace, project, solution va cac tep ma nguon theo ten dich vu
    duoc chi dinh, sau do bien dich va chay kiem thu de dam bao tinh doc lap.

.PARAMETER ServiceName
    Ten cua dich vu moi viet theo quy uoc PascalCase (vi du: ForumService, UserService, ChatService).
    Chi chap nhan cac ky tu chu cai va so, bat dau bang chu cai.

.PARAMETER TargetDirectory
    Duong dan thu muc dich noi dich vu moi se duoc tao ra.

.PARAMETER Force
    Ghi de thu muc dich neu thu muc da ton tai va khong rong.

.PARAMETER SkipBuild
    Bo qua buoc xac minh bien dich (dotnet build) va chay kiem thu (dotnet test).

.EXAMPLE
    .\scripts\bootstrap-service.ps1 -ServiceName ForumService -TargetDirectory ..\fpt-alumni-forum-service

.EXAMPLE
    .\scripts\bootstrap-service.ps1 -ServiceName UserService -TargetDirectory C:\PRN232\UserService -Force
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $true, HelpMessage = "New service name (e.g. ForumService, UserService)")]
    [string]$ServiceName,

    [Parameter(Mandatory = $true, HelpMessage = "Target directory path for the new service")]
    [string]$TargetDirectory,

    [Parameter(Mandatory = $false)]
    [switch]$Force,

    [Parameter(Mandatory = $false)]
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-WarningMsg {
    param([string]$Message)
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Write-ErrorMsg {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

Write-Host "=================================================================" -ForegroundColor DarkCyan
Write-Host "   FPT Alumni Connect -- Bootstrap Microservice Generator Tool   " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor DarkCyan

# 1. Xac thuc ten dich vu (ServiceName)
if ([string]::IsNullOrWhiteSpace($ServiceName)) {
    Write-ErrorMsg "Ten dich vu (-ServiceName) khong duoc de trong."
    exit 1
}

if ($ServiceName -notmatch '^[A-Za-z][A-Za-z0-9_]*$') {
    Write-ErrorMsg "Ten dich vu '$ServiceName' khong hop le! Ten phai bat dau bang chu cai, chi chua chu, so hoac dau gach duoi (PascalCase: UserService, ForumService)."
    exit 1
}

# 2. Xac dinh thu muc nguon (Template source)
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir
$TemplateDir = Join-Path $RepoRoot "templates\dotnet-service"

if (-not (Test-Path $TemplateDir)) {
    Write-ErrorMsg "Khong tim thay thu muc mau tai: $TemplateDir"
    exit 1
}

# 3. Chuan hoa duong dan dich
$ResolvedTarget = [System.IO.Path]::GetFullPath($TargetDirectory)
Write-Info "Ten dich vu moi: $ServiceName"
Write-Info "Thu muc dich:    $ResolvedTarget"

# 4. Kiem tra bao ve thu muc dich (Overwrite protection)
if (Test-Path $ResolvedTarget) {
    $ExistingFiles = Get-ChildItem -Path $ResolvedTarget -Force
    if ($ExistingFiles.Count -gt 0) {
        if (-not $Force) {
            Write-ErrorMsg "Thu muc dich '$ResolvedTarget' da ton tai va khong rong! Su dung tham so -Force neu ban muon ghi de."
            exit 1
        }
        else {
            Write-WarningMsg "Tham so -Force duoc kich hoat: Tien hanh don dep thu muc dich..."
            Remove-Item -Path "$ResolvedTarget\*" -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
else {
    New-Item -ItemType Directory -Path $ResolvedTarget -Force | Out-Null
}

# 5. Sao chep tep mau sach (Bo qua bin, obj, .vs)
Write-Info "Dang sao chep ma nguon mau sach tu template..."

$FilesToCopy = Get-ChildItem -Path $TemplateDir -Recurse | Where-Object {
    $_.FullName -notmatch '\\bin(\\|$)' -and
    $_.FullName -notmatch '\\obj(\\|$)' -and
    $_.FullName -notmatch '\\\.vs(\\|$)'
}

foreach ($Item in $FilesToCopy) {
    $RelativePath = $Item.FullName.Substring($TemplateDir.Length).TrimStart('\', '/')
    $DestinationPath = Join-Path $ResolvedTarget $RelativePath

    if ($Item.PSIsContainer) {
        if (-not (Test-Path $DestinationPath)) {
            New-Item -ItemType Directory -Path $DestinationPath -Force | Out-Null
        }
    }
    else {
        $DestDir = Split-Path -Parent $DestinationPath
        if (-not (Test-Path $DestDir)) {
            New-Item -ItemType Directory -Path $DestDir -Force | Out-Null
        }
        Copy-Item -Path $Item.FullName -Destination $DestinationPath -Force
    }
}

# 6. Doi noi dung cac tep van ban (Find & Replace "Template" -> "$ServiceName")
Write-Info "Dang cap nhat namespaces, class names, va chuoi cau hinh..."

$TextExtensions = @(".cs", ".csproj", ".sln", ".slnx", ".json", ".md", ".http")
$AllTargetFiles = Get-ChildItem -Path $ResolvedTarget -Recurse -File

foreach ($File in $AllTargetFiles) {
    if ($TextExtensions -contains $File.Extension.ToLower() -or $File.Name -eq ".gitignore") {
        $Content = [System.IO.File]::ReadAllText($File.FullName)
        
        # Thay the Template -> $ServiceName
        $NewContent = $Content.Replace("Template", $ServiceName)
        
        # Chuan hoa ten database trong appsettings.json
        $LowerService = $ServiceName.ToLower()
        $NewContent = $NewContent.Replace("template_db", "${LowerService}_db")
        $NewContent = $NewContent.Replace("TemplateDb", "${ServiceName}Db")
        
        [System.IO.File]::WriteAllText($File.FullName, $NewContent, [System.Text.Encoding]::UTF8)
    }
}

# 7. Doi ten tep tin va thu muc (tu sau nhat tro ra ngoai)
Write-Info "Dang doi ten cac du an va thu muc theo '$ServiceName'..."

# Doi ten tep truoc
$FilesToRename = Get-ChildItem -Path $ResolvedTarget -Recurse -File | Where-Object { $_.Name -like "*Template*" }
foreach ($File in $FilesToRename) {
    $NewName = $File.Name.Replace("Template", $ServiceName)
    $TargetFilePath = Join-Path $File.DirectoryName $NewName
    Move-Item -Path $File.FullName -Destination $TargetFilePath -Force
}

# Doi ten thu muc (sap xep theo do dai duong dan giam dan de doi thu muc con truoc)
$DirsToRename = Get-ChildItem -Path $ResolvedTarget -Recurse -Directory | Where-Object { $_.Name -like "*Template*" } | Sort-Object { $_.FullName.Length } -Descending
foreach ($Dir in $DirsToRename) {
    $NewName = $Dir.Name.Replace("Template", $ServiceName)
    $TargetDirPath = Join-Path (Split-Path -Parent $Dir.FullName) $NewName
    Move-Item -Path $Dir.FullName -Destination $TargetDirPath -Force
}

Write-Success "Khoi tao cau truc microservice '$ServiceName' thanh cong tai: $ResolvedTarget"

# 8. Xac minh bien dich & kiem thu (neu khong dung -SkipBuild)
if (-not $SkipBuild) {
    Write-Info "Bat dau xac minh doc lap: dotnet restore & build..."
    $SlnFile = Join-Path $ResolvedTarget "$ServiceName.sln"

    if (Test-Path $SlnFile) {
        & dotnet restore "$SlnFile"
        if ($LASTEXITCODE -ne 0) {
            Write-ErrorMsg "Loi khi khoi phuc goi NuGet (dotnet restore) cho $ServiceName!"
            exit $LASTEXITCODE
        }

        & dotnet build "$SlnFile" --no-restore --configuration Release
        if ($LASTEXITCODE -ne 0) {
            Write-ErrorMsg "Loi khi bien dich dich vu (dotnet build) cho $ServiceName!"
            exit $LASTEXITCODE
        }

        Write-Info "Dang chay bo kiem thu don vi doc lap..."
        & dotnet test "$SlnFile" --no-build --configuration Release
        if ($LASTEXITCODE -ne 0) {
            Write-ErrorMsg "Mot so kiem thu don vi bi that bai cho $ServiceName!"
            exit $LASTEXITCODE
        }

        Write-Success "Bien dich va kiem thu thanh cong 100%!"
    }
    else {
        Write-WarningMsg "Khong tim thay tep $SlnFile de xac minh bien dich."
    }
}

# 9. Huong dan cac buoc tiep theo
Write-Host ""
Write-Host "=================================================================" -ForegroundColor DarkGreen
Write-Host "  HUONG DAN CAC BUOC TIEP THEO CHO DICH VU $ServiceName  " -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor DarkGreen
Write-Host "1. Di chuyen vao thu muc dich vu:"
Write-Host "   cd `"$ResolvedTarget`"" -ForegroundColor Yellow
Write-Host "2. Mo solution bang Visual Studio hoac VS Code:"
Write-Host "   code ." -ForegroundColor Yellow
Write-Host "3. Cau hinh chuoi ket noi co so du lieu trong src/$ServiceName.Api/appsettings.Development.json"
Write-Host "   `"DefaultConnection`": `"Server=localhost;Database=FptAlumni_${ServiceName}_Db;...`"" -ForegroundColor Yellow
Write-Host "4. Sinh khoa JWT ngau nhien va thiet lap qua User Secrets hoac bien moi truong (khong commit vao Git):"
Write-Host "   `$bytes = New-Object byte[] 32; [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes(`$bytes); `$randomKey = [Convert]::ToBase64String(`$bytes)" -ForegroundColor Yellow
Write-Host "   dotnet user-secrets init --project src/$ServiceName.Api" -ForegroundColor Yellow
Write-Host "   dotnet user-secrets set `"Jwt:Key`" `$randomKey --project src/$ServiceName.Api" -ForegroundColor Yellow
Write-Host "   hoac tren PowerShell: `$env:Jwt__Key = `$randomKey" -ForegroundColor Yellow
Write-Host "5. Doc huong dan chi tiet tao Entity va Migration tai:"
Write-Host "   docs/04-dotnet-standards.md va docs/05-database.md" -ForegroundColor Yellow
Write-Host "6. Su dung checklist tai checklists/new-service.md de kiem tra day du cac buoc."
Write-Host ""
exit 0
