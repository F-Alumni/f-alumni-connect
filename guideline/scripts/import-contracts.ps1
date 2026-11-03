<#
.SYNOPSIS
    Nhap khau (import) cac hop dong Kafka va gRPC tu repository huong dan vao repository dich vu doc lap.

.DESCRIPTION
    Kich ban nay thuc hien:
    1. Kiem tra tinh toan ven va trang thai commit cua repository huong dan nguon (Guideline Source).
    2. Sao chep duy nhat cac tep hop dong duoc yeu cau vao thu muc 'contracts/' cua dich vu dich.
    3. Ghi lai ma commit SHA goc va ma bam SHA-256 cua tung tep vao 'contracts.manifest.json'.
    4. Tu choi ghi de neu phat hien tep hop dong tai dich vu dich da bi chinh sua cuc bo (tru khi co co -Force).
    5. Tu choi nhap khau neu ma nguon hop dong dang co thay doi chua commit (tru khi co co -AllowDirty).

.PARAMETER TargetDirectory
    Duong dan thu muc goc cua dich vu can nhap khau hop dong (vi du: C:\PRN232\f-alumni-chat-service).

.PARAMETER ContractId
    Dinh danh hop dong can nhap khau:
    - 'grpc/user-profile/v1'    : Hop dong gRPC tra cuu ho so giua Chat va User Service.
    - 'events/user-registered/v1': Hop dong su kien Kafka user.registered.v1.
    - 'all'                     : Nhap khau tat ca cac hop dong chuan.

.PARAMETER SourceGuidelinePath
    Duong dan toi thu muc goc cua guideline repository. Mac dinh la thu muc goc cua repo chua script nay.

.PARAMETER Force
    Ghi de bat buoc ngay ca khi cac tep hop dong hien tai tai dich vu da bi thay doi cuc bo.

.PARAMETER AllowDirty
    Cho phep nhap khau ngay ca khi thu muc contracts/ tai repo nguon co thay doi chua commit.

.EXAMPLE
    .\scripts\import-contracts.ps1 -TargetDirectory ..\f-alumni-chat-service -ContractId grpc/user-profile/v1

.EXAMPLE
    .\scripts\import-contracts.ps1 -TargetDirectory ..\f-alumni-user-service -ContractId events/user-registered/v1

.EXAMPLE
    .\scripts\import-contracts.ps1 -TargetDirectory ..\f-alumni-user-service -ContractId all
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $true, Position = 0, HelpMessage = "Duong dan thu muc dich vu can nhap khau hop dong")]
    [string]$TargetDirectory,

    [Parameter(Mandatory = $true, Position = 1, HelpMessage = "Dinh danh hop dong (vi du: grpc/user-profile/v1, events/user-registered/v1, all)")]
    [string]$ContractId,

    [Parameter(Mandatory = $false, HelpMessage = "Duong dan repository huong dan nguon")]
    [string]$SourceGuidelinePath,

    [Parameter(Mandatory = $false)]
    [switch]$Force,

    [Parameter(Mandatory = $false)]
    [switch]$AllowDirty
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Danh muc cac hop dong chuan duoc ho tro trong he thong
$ContractCatalog = @{
    "grpc/user-profile/v1" = @{
        Type = "gRPC"
        Version = "1.0"
        Description = "Hop dong gRPC Protobuf tra cuu ho so nguoi dung (Chat Service -> User Service)"
        Files = @(
            "grpc/user-profile/v1/user_profile.proto",
            "grpc/user-profile/v1/README.md"
        )
    }
    "events/user-registered/v1" = @{
        Type = "KafkaEvent"
        Version = "1.0"
        Description = "Hop dong su kien mien Kafka user.registered.v1 (User Service -> Worker / Notification)"
        Files = @(
            "events/EventEnvelope.cs",
            "events/UserRegisteredEventV1.cs",
            "events/user.registered.v1.json",
            "events/README.md"
        )
    }
}

# Ho tro cac alias pho bien
$ContractAliases = @{
    "grpc/user-profile.v1"       = "grpc/user-profile/v1"
    "events/user.registered.v1"  = "events/user-registered/v1"
    "events/user-registered"     = "events/user-registered/v1"
    "grpc/user-profile"          = "grpc/user-profile/v1"
}

# 1. Xac dinh duong dan SourceGuidelinePath
if ([string]::IsNullOrWhiteSpace($SourceGuidelinePath)) {
    $ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $SourceGuidelinePath = Split-Path -Parent $ScriptDir
}

$ResolvedSource = Resolve-Path $SourceGuidelinePath -ErrorAction SilentlyContinue
if ($null -eq $ResolvedSource -or (-not (Test-Path (Join-Path $ResolvedSource.Path "contracts")))) {
    Write-Error "[FAIL] Khong tim thay thu muc 'contracts/' tai duong dan repo huong dan: $SourceGuidelinePath"
    exit 1
}
$SourceContractsDir = Join-Path $ResolvedSource.Path "contracts"

# 2. Xac thuc commit SHA va trang thai git cua source repo
$SourceCommitSha = "UNKNOWN"
$GitCmd = Get-Command git -ErrorAction SilentlyContinue

if ($null -ne $GitCmd) {
    try {
        $SourceCommitSha = (& git -C "$($ResolvedSource.Path)" rev-parse HEAD 2>&1).Trim()
        
        # Kiem tra dirty state trong contracts/
        $DirtyCheck = & git -C "$($ResolvedSource.Path)" status --porcelain contracts 2>&1
        if (-not [string]::IsNullOrWhiteSpace($DirtyCheck) -and (-not $AllowDirty)) {
            $dirtyMsg = "[FAIL] Thu muc 'contracts/' tai repo huong dan dang chua thay doi chua duoc commit!`n" +
                        "Hay commit cac thay doi truoc khi xuat ban/nhap khau de dam bao tinh minh bach phien ban (provenance),`n" +
                        "hoac su dung tham so -AllowDirty neu dang thu nghiem cuc bo."
            Write-Error $dirtyMsg
            exit 1
        }
    }
    catch {
        Write-Warning "[WARN] Khong the doc Git commit SHA tu repository nguon: $_"
    }
}

# 3. Chuan hoa ContractId
$ContractIdKey = $ContractId.Trim()
if ($ContractAliases.ContainsKey($ContractIdKey)) {
    $ContractIdKey = $ContractAliases[$ContractIdKey]
}

$ContractsToImport = @()
if ($ContractIdKey -eq "all") {
    $ContractsToImport = @($ContractCatalog.Keys)
}
elseif ($ContractCatalog.ContainsKey($ContractIdKey)) {
    $ContractsToImport = @($ContractIdKey)
}
else {
    Write-Host "[FAIL] Dinh danh hop dong '$ContractId' khong hop le!" -ForegroundColor Red
    Write-Host "Cac hop dong hop le co the nhap khau:" -ForegroundColor Yellow
    foreach ($k in $ContractCatalog.Keys) {
        Write-Host "  - $k ($($ContractCatalog[$k].Description))" -ForegroundColor DarkCyan
    }
    Write-Host "  - all (Nhap khau toan bo cac hop dong tren)" -ForegroundColor DarkCyan
    exit 1
}

# 4. Chuan bi thu muc dich
if (-not (Test-Path $TargetDirectory)) {
    New-Item -ItemType Directory -Path $TargetDirectory -Force | Out-Null
}
$ResolvedTarget = (Resolve-Path $TargetDirectory).Path
$TargetContractsDir = Join-Path $ResolvedTarget "contracts"
if (-not (Test-Path $TargetContractsDir)) {
    New-Item -ItemType Directory -Path $TargetContractsDir -Force | Out-Null
}

$ManifestPath = Join-Path $TargetContractsDir "contracts.manifest.json"

# 5. Doc manifest hien tai (neu co)
$ExistingManifest = $null
if (Test-Path $ManifestPath) {
    try {
        $ExistingContent = Get-Content -Path $ManifestPath -Raw -Encoding UTF8
        $ExistingManifest = $ExistingContent | ConvertFrom-Json
    }
    catch {
        Write-Warning "[WARN] Khong the doc manifest cu, se khoi tao manifest moi: $_"
    }
}

# 6. Kiem tra xem co file nao bi modified cuc bo hay khong (Guardrail)
foreach ($cId in $ContractsToImport) {
    $meta = $ContractCatalog[$cId]
    foreach ($relFile in $meta.Files) {
        $destFile = Join-Path $TargetContractsDir $relFile
        if (Test-Path $destFile) {
            # Tinh hash hien tai
            $currentHash = (Get-FileHash -Path $destFile -Algorithm SHA256).Hash
            
            # Kiem tra trong manifest cu neu co
            if ($null -ne $ExistingManifest -and $null -ne $ExistingManifest.contracts) {
                $cObj = $ExistingManifest.contracts.$cId
                if ($null -ne $cObj -and $null -ne $cObj.files) {
                    $recordedFile = $cObj.files | Where-Object { $_.relativePath -eq $relFile }
                    if ($null -ne $recordedFile -and $recordedFile.sha256 -ne $currentHash -and (-not $Force)) {
                        $guardMsg = "[SECURITY GUARDRAIL] Tep hop dong '$relFile' tai dich vu da bi chinh sua cuc bo (hash mismatch)!`n" +
                                    "  Hash ghi nhan: $($recordedFile.sha256)`n" +
                                    "  Hash hien tai:  $currentHash`n" +
                                    "Nhap khau bi huy de tranh mat mat thay doi cuc bo. Su dung -Force neu ban chu dich ghi de!"
                        Write-Error $guardMsg
                        exit 1
                    }
                }
            }
        }
    }
}

# 7. Thuc hien copy cac tep hop dong va tinh toan SHA-256
Write-Host "=================================================================" -ForegroundColor DarkCyan
Write-Host "        FPT Alumni Connect -- Contract Distribution Tool         " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor DarkCyan
Write-Host "Guideline Source Commit : $SourceCommitSha" -ForegroundColor Green
Write-Host "Target Service Directory: $ResolvedTarget" -ForegroundColor Green
Write-Host "Target Contracts Dir    : $TargetContractsDir`n" -ForegroundColor DarkGray

$ImportResults = [ordered]@{}

# Neu da co manifest cu, giu lai cac contracts khac khong bi import de
if ($null -ne $ExistingManifest -and $null -ne $ExistingManifest.contracts) {
    foreach ($prop in $ExistingManifest.contracts.PSObject.Properties) {
        if ($ContractsToImport -notcontains $prop.Name) {
            $ImportResults[$prop.Name] = $prop.Value
        }
    }
}

foreach ($cId in $ContractsToImport) {
    $meta = $ContractCatalog[$cId]
    Write-Host "[IMPORT] Hop dong: $cId ($($meta.Type))" -ForegroundColor Cyan
    
    $fileRecords = [System.Collections.Generic.List[PSObject]]::new()

    foreach ($relFile in $meta.Files) {
        $sourceFile = Join-Path $SourceContractsDir $relFile
        $targetFile = Join-Path $TargetContractsDir $relFile
        
        if (-not (Test-Path $sourceFile)) {
            Write-Error "[FAIL] Khong tim thay tep hop dong nguon: $sourceFile"
            exit 1
        }

        # Tao thu muc cha neu chua co
        $targetParent = Split-Path -Parent $targetFile
        if (-not (Test-Path $targetParent)) {
            New-Item -ItemType Directory -Path $targetParent -Force | Out-Null
        }

        Copy-Item -Path $sourceFile -Destination $targetFile -Force
        $fileHash = (Get-FileHash -Path $targetFile -Algorithm SHA256).Hash

        Write-Host "  -> $relFile" -ForegroundColor DarkGreen
        Write-Host "     SHA-256: $fileHash" -ForegroundColor DarkGray

        $fileRecords.Add([ordered]@{
            relativePath = $relFile.Replace("\", "/")
            sha256       = $fileHash
        })
    }

    $ImportResults[$cId] = [ordered]@{
        type        = $meta.Type
        version     = $meta.Version
        description = $meta.Description
        files       = $fileRecords
    }
}

# 8. Ghi contracts.manifest.json
$ManifestObject = [ordered]@{
    manifestVersion = "1.0"
    guidelineCommit = $SourceCommitSha
    importedAtUtc   = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    guidelineRepo   = "fpt-alumni-guideline"
    contracts       = $ImportResults
}

$JsonPayload = $ManifestObject | ConvertTo-Json -Depth 6
[System.IO.File]::WriteAllText($ManifestPath, $JsonPayload, [System.Text.Encoding]::UTF8)
Write-Host "`n[MANIFEST] Da tao va luu tru: $ManifestPath" -ForegroundColor Green

# 9. Sao chep kem script verify-contracts.ps1 vao thu muc scripts/ cua dich vu dich
$VerifyScriptSrc = Join-Path $ScriptDir "verify-contracts.ps1"
if (Test-Path $VerifyScriptSrc) {
    $TargetScriptsDir = Join-Path $ResolvedTarget "scripts"
    if (-not (Test-Path $TargetScriptsDir)) {
        New-Item -ItemType Directory -Path $TargetScriptsDir -Force | Out-Null
    }
    $TargetVerifyScript = Join-Path $TargetScriptsDir "verify-contracts.ps1"
    Copy-Item -Path $VerifyScriptSrc -Destination $TargetVerifyScript -Force
    Write-Host "[TOOLING] Da sao chep scripts/verify-contracts.ps1 sang dich vu de ho tro xac minh doc lap." -ForegroundColor Green
}

Write-Host "`n=================================================================" -ForegroundColor DarkCyan
Write-Host "     >>> NHAP KHAU HOP DONG HOAN TOAN THANH CONG (PASS) <<<      " -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor DarkCyan
Write-Host "Huong dan tiep theo cho lap trinh vien:" -ForegroundColor Yellow
Write-Host "1. Kiem tra hop dong vua nhap bang lenh: .\scripts\verify-contracts.ps1" -ForegroundColor Gray
Write-Host "2. Tham chieu hop dong cuc bo tu .csproj ma khong can den repository guideline." -ForegroundColor Gray
Write-Host "3. Commit ca tep hop dong va contracts/contracts.manifest.json vao Git cua dich vu." -ForegroundColor Gray
exit 0
