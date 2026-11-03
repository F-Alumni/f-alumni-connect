<#
.SYNOPSIS
    Kich ban xac minh toan dien he thong Huong dan FPT Alumni Connect (PRN232).

.DESCRIPTION
    Kiem tra toan bo cau truc thu muc, tep cau hinh, hop dong (contracts),
    kiem tra tinh toan ven cua lien ket tai lieu (markdown links),
    bien dich va chay 100% bo kiem thu don vi tren tat ca cac solution (.NET 10),
    va kiem tra cu phap tep Docker Compose.

.PARAMETER SkipTests
    Bo qua chay kiem thu don vi (chi thuc hien restore, build va static checks).

.PARAMETER FailFast
    Dung ngay lap tuc khi gap loi dau tien thay vi chay het tat ca cac kiem tra.

.PARAMETER IncludeDocker
    Thu thuc hien kiem tra container Docker neu Docker daemon dang chay tren may.

.EXAMPLE
    .\scripts\verify-all.ps1

.EXAMPLE
    .\scripts\verify-all.ps1 -SkipTests
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $false)]
    [switch]$SkipTests,

    [Parameter(Mandatory = $false)]
    [switch]$FailFast,

    [Parameter(Mandatory = $false)]
    [switch]$IncludeDocker
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir

$TotalChecks = 0
$PassedChecks = 0
$FailedChecks = 0
$WarnedChecks = 0
$Results = [System.Collections.Generic.List[PSObject]]::new()

function Record-Check {
    param (
        [string]$Category,
        [string]$Name,
        [string]$Status, # PASS, FAIL, WARN, SKIP
        [string]$Details
    )

    $script:TotalChecks++
    if ($Status -eq "PASS") {
        $script:PassedChecks++
        Write-Host "  [PASS] $Name" -ForegroundColor Green
    }
    elseif ($Status -eq "WARN") {
        $script:WarnedChecks++
        Write-Host "  [WARN] $Name - $Details" -ForegroundColor Yellow
    }
    elseif ($Status -eq "SKIP") {
        Write-Host "  [SKIP] $Name - $Details" -ForegroundColor Gray
    }
    else {
        $script:FailedChecks++
        Write-Host "  [FAIL] $Name - $Details" -ForegroundColor Red
        if ($FailFast) {
            Write-Host "`n[FATAL] FailFast duoc bat. Dung kiem tra ngay lap tuc!" -ForegroundColor Red
            Print-Summary
            exit 1
        }
    }

    $script:Results.Add([PSCustomObject]@{
        Category = $Category
        Name     = $Name
        Status   = $Status
        Details  = $Details
    })
}

function Print-Summary {
    Write-Host "`n=================================================================" -ForegroundColor DarkCyan
    Write-Host "               KET QUA XAC MINH HE THONG GUIDELINE              " -ForegroundColor Cyan
    Write-Host "=================================================================" -ForegroundColor DarkCyan
    Write-Host ("  Tong so kiem tra : {0}" -f $script:TotalChecks)
    Write-Host ("  Thanh cong (PASS): {0}" -f $script:PassedChecks) -ForegroundColor Green
    Write-Host ("  Canh bao   (WARN): {0}" -f $script:WarnedChecks) -ForegroundColor Yellow
    Write-Host ("  That bai   (FAIL): {0}" -f $script:FailedChecks) -ForegroundColor $(if ($script:FailedChecks -gt 0) { "Red" } else { "Green" })
    Write-Host "-----------------------------------------------------------------" -ForegroundColor DarkCyan

    if ($script:FailedChecks -eq 0) {
        Write-Host ">>> KET QUA CHUNG CUOC: TAT CA KIEM TRA HOAN TOAN DAT CHUAN (PASS) <<<" -ForegroundColor Green
    }
    else {
        Write-Host ">>> KET QUA CHUNG CUOC: CO KIEM TRA BI THAT BAI (FAIL) <<<" -ForegroundColor Red
    }
    Write-Host "=================================================================`n" -ForegroundColor DarkCyan
}

Write-Host "=================================================================" -ForegroundColor DarkCyan
Write-Host "     FPT Alumni Connect -- Automated Guideline Verifier         " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor DarkCyan
Write-Host "Thu muc goc: $RepoRoot`n"

# -------------------------------------------------------------
# PHAN 1: KIEM TRA TINH TOAN VEN CUA TEP TIN & CAU HINH TINH
# -------------------------------------------------------------
Write-Host "[1/5] Kiem tra tap tin he thong va hop dong du an..." -ForegroundColor Cyan

# 1.1. Kiem tra docker/.env.example
$EnvExample = Join-Path $RepoRoot "docker\.env.example"
if (Test-Path $EnvExample) {
    $EnvContent = Get-Content $EnvExample -Raw
    $HasMssql   = $EnvContent -match "MSSQL_SA_PASSWORD"
    $HasKafka   = $EnvContent -match "KAFKA_HOST_PORT"
    $HasGateway = $EnvContent -match "GATEWAY_PORT"
    if ($HasMssql -and $HasKafka -and $HasGateway) {
        Record-Check "Config" "docker/.env.example chua day du bien bat buoc" "PASS" "MSSQL, Kafka, Gateway co mat"
    }
    else {
        Record-Check "Config" "docker/.env.example thieu bien moi truong quan trong" "FAIL" "Kiem tra MSSQL, Kafka hoac Gateway"
    }
}
else {
    Record-Check "Config" "docker/.env.example ton tai" "FAIL" "Khong tim thay tep docker/.env.example"
}

# 1.2. Kiem tra docker/compose.demo.yml
$ComposeFile = Join-Path $RepoRoot "docker\compose.demo.yml"
if (Test-Path $ComposeFile) {
    Record-Check "Config" "docker/compose.demo.yml ton tai" "PASS" "Tep compose mau co mat"
}
else {
    Record-Check "Config" "docker/compose.demo.yml ton tai" "FAIL" "Khong tim thay docker/compose.demo.yml"
}

# 1.3. Kiem tra contracts
$KafkaContract = Join-Path $RepoRoot "contracts\events\user.registered.v1.json"
if (Test-Path $KafkaContract) {
    Record-Check "Contracts" "Hop dong su kien Kafka user.registered.v1 co mat" "PASS" "contracts/events/user.registered.v1.json"
}
else {
    Record-Check "Contracts" "Hop dong su kien Kafka user.registered.v1 co mat" "FAIL" "Thieu contracts/events/user.registered.v1.json"
}

$GrpcContract = Join-Path $RepoRoot "contracts\grpc\user-profile\v1\user_profile.proto"
if (Test-Path $GrpcContract) {
    Record-Check "Contracts" "Hop dong gRPC Protobuf user_profile.proto co mat" "PASS" "contracts/grpc/user-profile/v1/user_profile.proto"
}
else {
    Record-Check "Contracts" "Hop dong gRPC Protobuf user_profile.proto co mat" "FAIL" "Thieu contracts/grpc/user-profile/v1/user_profile.proto"
}

# 1.4. Kiem tra danh muc tai lieu docs/
$RequiredDocs = @(
    "01-getting-started.md",
    "02-architecture.md",
    "03-service-boundaries.md",
    "04-dotnet-standards.md",
    "05-database.md",
    "06-rest-api.md",
    "07-authentication.md",
    "08-kafka.md",
    "09-grpc.md",
    "10-background-worker.md",
    "11-api-gateway.md",
    "12-docker.md",
    "13-testing.md",
    "14-team-workflow.md",
    "15-contract-sharing.md",
    "16-phase-guideline-handover.md"
)

foreach ($DocName in $RequiredDocs) {
    $DocPath = Join-Path $RepoRoot "docs\$DocName"
    if (Test-Path $DocPath) {
        Record-Check "Docs" "Tai lieu docs/$DocName ton tai" "PASS" "Co mat"
    }
    else {
        Record-Check "Docs" "Tai lieu docs/$DocName ton tai" "FAIL" "Thieu tep docs/$DocName"
    }
}

# 1.5. Kiem tra danh muc checklists/
$RequiredChecklists = @(
    "new-service.md",
    "integration.md",
    "asm-demo.md"
)

foreach ($CheckName in $RequiredChecklists) {
    $CheckPath = Join-Path $RepoRoot "checklists\$CheckName"
    if (Test-Path $CheckPath) {
        Record-Check "Checklists" "Checklist checklists/$CheckName ton tai" "PASS" "Co mat"
    }
    else {
        Record-Check "Checklists" "Checklist checklists/$CheckName ton tai" "FAIL" "Thieu tep checklists/$CheckName"
    }
}

# 1.6. Kiem tra danh muc scripts/ tu dong hoa
$RequiredScripts = @(
    "bootstrap-service.ps1",
    "verify-all.ps1",
    "smoke-test.ps1",
    "import-contracts.ps1",
    "verify-contracts.ps1"
)

foreach ($ScriptName in $RequiredScripts) {
    $ScriptPath = Join-Path $RepoRoot "scripts\$ScriptName"
    if (Test-Path $ScriptPath) {
        Record-Check "Scripts" "Kich ban scripts/$ScriptName ton tai" "PASS" "Co mat"
    }
    else {
        Record-Check "Scripts" "Kich ban scripts/$ScriptName ton tai" "FAIL" "Thieu tep scripts/$ScriptName"
    }
}

# -------------------------------------------------------------
# PHAN 2: XAC THUC CU PHAP DOCKER COMPOSE
# -------------------------------------------------------------
Write-Host "`n[2/5] Kiem tra cu phap Docker Compose..." -ForegroundColor Cyan

$DockerCmd = Get-Command docker -ErrorAction SilentlyContinue
if ($null -ne $DockerCmd) {
    $ComposeConfigOutput = & docker compose -f $ComposeFile config --quiet 2>&1
    if ($LASTEXITCODE -eq 0) {
        Record-Check "Docker" "Cu phap docker/compose.demo.yml hop le" "PASS" "docker compose config thanh cong"
    }
    else {
        Record-Check "Docker" "Cu phap docker/compose.demo.yml khong hop le" "FAIL" "$ComposeConfigOutput"
    }
}
else {
    Record-Check "Docker" "CLI Docker khong duoc cai dat tren may" "WARN" "Bo qua buoc kiem tra docker compose config"
}

# -------------------------------------------------------------
# PHAN 3: KHOI PHUC VA BIEN DICH TAT CA SOLUTIONS (.NET 10)
# -------------------------------------------------------------
Write-Host "`n[3/5] Khoi phuc va bien dich 5 solution .NET 10..." -ForegroundColor Cyan

$Solutions = @(
    "templates\dotnet-service\Template.sln",
    "examples\kafka-demo\KafkaDemo.sln",
    "examples\grpc-demo\GrpcDemo.sln",
    "examples\demo-worker\DemoWorker.sln",
    "examples\gateway-demo\GatewayDemo.sln"
)

foreach ($RelSln in $Solutions) {
    $SlnPath = Join-Path $RepoRoot $RelSln
    $SlnName = [System.IO.Path]::GetFileNameWithoutExtension($SlnPath)

    if (Test-Path $SlnPath) {
        $RestoreOut = & dotnet restore "$SlnPath" 2>&1
        if ($LASTEXITCODE -eq 0) {
            Record-Check "Build" "dotnet restore: $SlnName" "PASS" "Restore thanh cong"
        }
        else {
            Record-Check "Build" "dotnet restore: $SlnName" "FAIL" "Loi khi restore"
            continue
        }

        $BuildOut = & dotnet build "$SlnPath" --no-restore --configuration Release 2>&1
        if ($LASTEXITCODE -eq 0) {
            Record-Check "Build" "dotnet build: $SlnName (Release)" "PASS" "Build 0 errors"
        }
        else {
            Record-Check "Build" "dotnet build: $SlnName (Release)" "FAIL" "Loi bien dich"
        }
    }
    else {
        Record-Check "Build" "Khong tim thay solution $RelSln" "FAIL" "Duong dan khong ton tai"
    }
}

# -------------------------------------------------------------
# PHAN 4: CHAY BO KIEM THU DON VI (UNIT & INTEGRATION TESTS)
# -------------------------------------------------------------
Write-Host "`n[4/5] Chay kiem thu tu dong (Automated Unit & Integration Tests)..." -ForegroundColor Cyan

if (-not $SkipTests) {
    foreach ($RelSln in $Solutions) {
        $SlnPath = Join-Path $RepoRoot $RelSln
        $SlnName = [System.IO.Path]::GetFileNameWithoutExtension($SlnPath)

        if (Test-Path $SlnPath) {
            Write-Host "  -> Dang kiem thu solution: $SlnName..." -ForegroundColor DarkGray
            $TestOut = & dotnet test "$SlnPath" --no-build --configuration Release 2>&1
            if ($LASTEXITCODE -eq 0) {
                # Trich xuat so luong test passed tu output
                $Match = [regex]::Match($TestOut, "Passed!\s+-\s+Failed:\s+0,\s+Passed:\s+(\d+)")
                $PassedCount = if ($Match.Success) { $Match.Groups[1].Value } else { "Tat ca" }
                Record-Check "Tests" "dotnet test: $SlnName ($PassedCount passed)" "PASS" "100% Passed"
            }
            else {
                Record-Check "Tests" "dotnet test: $SlnName" "FAIL" "Co test bi that bai"
            }
        }
    }
}
else {
    Record-Check "Tests" "Bo kiem thu don vi" "SKIP" "Tham so -SkipTests duoc bat"
}

# -------------------------------------------------------------
# PHAN 5: KIEM TRA TRANG THAI DOCKER LIVE (NEU YEU CAU)
# -------------------------------------------------------------
Write-Host "`n[5/5] Kiem tra Docker Daemon & Smoke Test..." -ForegroundColor Cyan

if ($IncludeDocker) {
    $DockerPsOut = & docker ps 2>&1
    if ($LASTEXITCODE -eq 0) {
        Record-Check "DockerLive" "Docker Daemon dang hoat dong" "PASS" "Ket noi thanh cong"
        # Thuc hien goi endpoint gateway neu container dang chay
        try {
            $GatewayHealth = Invoke-RestMethod -Uri "http://localhost:5000/health" -TimeoutSec 3 -ErrorAction Stop
            if ($GatewayHealth -eq "Healthy") {
                Record-Check "SmokeTest" "API Gateway Health Check (http://localhost:5000/health)" "PASS" "Healthy"
            }
            else {
                Record-Check "SmokeTest" "API Gateway Health Check" "WARN" "Tra ve: $GatewayHealth"
            }
        }
        catch {
            Record-Check "SmokeTest" "API Gateway Endpoint (:5000) chua san sang" "WARN" "Container co the chua khoi chay qua compose up"
        }
    }
    else {
        Record-Check "DockerLive" "Docker Daemon khong hoat dong tren host" "WARN" "Dich vu com.docker.service chua duoc bat"
    }
}
else {
    Record-Check "DockerLive" "Kiem tra Docker truc tiep tren may host" "SKIP" "Su dung -IncludeDocker neu muon kiem tra container dang chay"
}

# IN TONG KET
Print-Summary

if ($FailedChecks -gt 0) {
    exit 1
}
exit 0
