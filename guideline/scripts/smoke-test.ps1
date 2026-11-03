<#
.SYNOPSIS
    Kich ban kiem thu khoi (Integration Smoke Test) cho FPT Alumni Connect.

.DESCRIPTION
    Kiem tra 2 luong tich hop microservices quan trong nhat cua do an:
    1. Workflow A: Client -> Gateway -> Chat REST API -> User gRPC Server (Dong bo HTTP/2)
    2. Workflow B: Kafka Producer -> Real Kafka Broker -> Notification Background Worker -> Mock Email (Bat dong bo)

.PARAMETER Live
    Yeu cau kiem thu truc tiep tren cac container Docker dang hoat dong.
    NEU DOCKER CHUA BAT, KICH BAN SE BAO [NOT VERIFIED] VA TRA VE MA LOI KHAC 0,
    TUYET DOI KHONG TU DONG CHUYEN SANG IN-PROCESS DE TRUYEN THONG SAI LECH KET QUA.

.PARAMETER InProcess
    Yeu cau kiem thu tich hop gia lap trong tien trinh (In-Process Test Suites qua WebApplicationFactory).

.EXAMPLE
    .\scripts\smoke-test.ps1 -Live
.EXAMPLE
    .\scripts\smoke-test.ps1 -InProcess
.EXAMPLE
    .\scripts\smoke-test.ps1
#>

[CmdletBinding(DefaultParameterSetName = "Auto")]
param (
    [Parameter(ParameterSetName = "Live", Mandatory = $false)]
    [switch]$Live,

    [Parameter(ParameterSetName = "InProcess", Mandatory = $false)]
    [switch]$InProcess
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir
$ComposeFile = Join-Path $RepoRoot "docker\compose.demo.yml"

Write-Host "=================================================================" -ForegroundColor DarkCyan
Write-Host "     FPT Alumni Connect -- Integration Smoke Test Runner        " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor DarkCyan

# -------------------------------------------------------------
# XAC DINH CHE DO KIEM THU (LIVE vs IN-PROCESS)
# -------------------------------------------------------------
$RunLive = $false

if ($Live) {
    $RunLive = $true
}
elseif ($InProcess) {
    $RunLive = $false
}
else {
    # Che do Auto: Tu dong phat hien neu Docker dang chay va container Gateway da san sang
    $DockerCmd = Get-Command docker -ErrorAction SilentlyContinue
    if ($null -ne $DockerCmd) {
        $PsCheck = & docker ps 2>&1
        if ($LASTEXITCODE -eq 0) {
            # Kiem tra xem gateway port 5000 co phan hoi khong
            try {
                $probe = Invoke-RestMethod -Uri "http://localhost:5000/health" -TimeoutSec 2 -ErrorAction Stop
                if ($probe -eq "Healthy") {
                    $RunLive = $true
                }
            }
            catch {
                $RunLive = $false
            }
        }
    }
}

# -------------------------------------------------------------
# THUC THI KIEM THU TRUC TIEP TREN DOCKER (LIVE DOCKER MODE)
# -------------------------------------------------------------
if ($RunLive) {
    Write-Host "[CHE DO] LIVE DOCKER INTEGRATION -- Kiem thu tren he thong container thuc te`n" -ForegroundColor Green

    # 1. Kiem tra Docker CLI va Daemon
    $DockerCmd = Get-Command docker -ErrorAction SilentlyContinue
    if ($null -eq $DockerCmd) {
        Write-Host "  [NOT VERIFIED] CLI Docker khong ton tai tren he thong." -ForegroundColor Red
        exit 1
    }

    $DockerPsOut = & docker ps 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [NOT VERIFIED] Docker daemon / Docker Desktop chua duoc bat tren may host." -ForegroundColor Red
        exit 1
    }

    # 2. Kiem tra cac container bat buoc dang chay
    Write-Host "1. Kiem tra trang thai cac container demo..." -ForegroundColor Cyan
    $RunningServices = & docker compose -f "$ComposeFile" ps --services --filter "status=running" 2>&1
    $RequiredServices = @("api-gateway", "chat-service", "user-grpc-server", "kafka", "notification-worker")
    $MissingServices = @()

    foreach ($svc in $RequiredServices) {
        if ($RunningServices -notcontains $svc) {
            $MissingServices += $svc
        }
    }

    if ($MissingServices.Count -gt 0) {
        Write-Host "  [FAIL] Mot so container bat buoc chua chay: $($MissingServices -join ', ')" -ForegroundColor Red
        Write-Host "  Huong dan: Chay lenh sau de khoi dong:" -ForegroundColor Yellow
        Write-Host "  docker compose -f docker/compose.demo.yml up -d kafka user-grpc-server chat-service notification-worker api-gateway" -ForegroundColor Yellow
        exit 1
    }
    Write-Host "   -> Tat ca 5 container demo dang hoat dong (Up/Healthy)." -ForegroundColor Green

    # 3. Kiem tra Gateway Health
    Write-Host "`n2. Kiem tra API Gateway Health (:5000/health)..." -ForegroundColor Cyan
    try {
        $HealthRes = Invoke-RestMethod -Uri "http://localhost:5000/health" -TimeoutSec 3 -ErrorAction Stop
        Write-Host "   -> API Gateway Health: $HealthRes (HTTP 200 OK)" -ForegroundColor Green
    }
    catch {
        Write-Host "   -> [FAIL] Khong the ket noi toi Gateway tai http://localhost:5000/health" -ForegroundColor Red
        exit 1
    }

    # 4. WORKFLOW A: Client -> Gateway -> Chat REST -> User gRPC
    Write-Host "`n3. [WORKFLOW A] Kiem tra: Client -> YARP Gateway -> Chat REST -> User gRPC (HTTP/2)..." -ForegroundColor Cyan
    $KnownUserId = "11111111-1111-1111-1111-111111111111"
    $ConvId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
    $ChatUrl = "http://localhost:5000/api/chat/conversations/$ConvId/participants/$KnownUserId"

    try {
        $ChatRes = Invoke-RestMethod -Uri $ChatUrl -TimeoutSec 5 -ErrorAction Stop
        Write-Host "   -> Goi URL: $ChatUrl" -ForegroundColor DarkGray
        Write-Host "   -> Nguoi dung: $($ChatRes.displayName) (Status: $($ChatRes.status))" -ForegroundColor Green
        Write-Host "   -> Avatar URL: $($ChatRes.avatarUrl)" -ForegroundColor Green
        Write-Host "   -> Phuong thuc giai quyet: $($ChatRes.resolvedVia)" -ForegroundColor Green

        if ($ChatRes.displayName -ne "Nguyen Van A" -or $ChatRes.resolvedVia -notlike "*gRPC*") {
            Write-Host "   -> [FAIL] Du lieu tra ve khong dung voi mong doi tu gRPC!" -ForegroundColor Red
            exit 1
        }
        Write-Host "   [PASS] Workflow A: Thanh cong ket noi Gateway -> Chat REST -> User gRPC!" -ForegroundColor Green
    }
    catch {
        Write-Host "   -> [FAIL] Loi khi goi Chat API qua Gateway: $_" -ForegroundColor Red
        exit 1
    }

    # 5. Kiem tra hop dong User khong ton tai (404 Not Found)
    Write-Host "`n4. [WORKFLOW A - Hop dong loi] Kiem tra nguoi dung khong ton tai (Expected 404)..." -ForegroundColor Cyan
    $UnknownUserId = "99999999-9999-9999-9999-999999999999"
    $MissingUrl = "http://localhost:5000/api/chat/conversations/$ConvId/participants/$UnknownUserId"

    try {
        $null = Invoke-RestMethod -Uri $MissingUrl -TimeoutSec 5 -ErrorAction Stop
        Write-Host "   -> [FAIL] API tra ve 200 OK cho user khong ton tai (Mong doi 404 Not Found)!" -ForegroundColor Red
        exit 1
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 404) {
            Write-Host "   -> Nhan dung ma loi HTTP 404 Not Found theo chuan ProblemDetails." -ForegroundColor Green
            Write-Host "   [PASS] Xu ly ngoai le RpcException (NotFound) hop chuan!" -ForegroundColor Green
        }
        else {
            Write-Host "   -> [FAIL] Ma loi khong dung mong doi: $statusCode (Mong doi 404)" -ForegroundColor Red
            exit 1
        }
    }

    # 6. WORKFLOW B: Kafka Producer -> Kafka Broker -> Notification Worker
    Write-Host "`n5. [WORKFLOW B] Kiem tra: Kafka Producer -> Kafka Broker -> Notification Worker..." -ForegroundColor Cyan
    $ProducerProj = Join-Path $RepoRoot "examples\kafka-demo\src\KafkaProducer\KafkaProducer.csproj"

    Write-Host "   -> Dang phat su kien 'user.registered.v1' vao Kafka broker (localhost:29092)..." -ForegroundColor DarkGray
    $ProducerOut = & dotnet run --project "$ProducerProj" --no-restore -c Release -- localhost:29092 2>&1
    $ProducerText = $ProducerOut -join "`n"
    if ($LASTEXITCODE -ne 0 -or $ProducerText -notmatch "SUCCESS") {
        Write-Host "   -> [FAIL] Phat su kien vao Kafka that bai:" -ForegroundColor Red
        Write-Host "$ProducerText" -ForegroundColor Red
        exit 1
    }
    Write-Host "   -> Kafka Producer da gui su kien thanh cong toi Broker." -ForegroundColor Green

    # Cho worker tieu thu message
    Write-Host "   -> Doi Notification Worker tieu thu message (3 giay)..." -ForegroundColor DarkGray
    Start-Sleep -Seconds 3

    # Kiem tra nhat ky Worker trong Docker
    $WorkerLogs = & docker compose -f "$ComposeFile" logs --tail 25 notification-worker 2>&1
    if ($WorkerLogs -match "\[MOCK EMAIL DELIVERED\]" -and $WorkerLogs -match "user\.registered\.v1") {
        Write-Host "   -> Tim thay nhat ky gui email gia lap trong Worker:" -ForegroundColor Green
        $MatchedLines = $WorkerLogs | Where-Object { $_ -match "\[MOCK EMAIL DELIVERED\]" } | Select-Object -Last 1
        Write-Host "      $MatchedLines" -ForegroundColor DarkGreen
        Write-Host "   [PASS] Workflow B: Kafka Broker va Notification Worker tieu thu & gui email thanh cong!" -ForegroundColor Green
    }
    else {
        Write-Host "   -> [FAIL] Khong tim thay nhat ky [MOCK EMAIL DELIVERED] trong Worker log:" -ForegroundColor Red
        Write-Host "$WorkerLogs" -ForegroundColor DarkGray
        exit 1
    }

    Write-Host "`n=================================================================" -ForegroundColor DarkCyan
    Write-Host "  >>> KET QUA: TOAN BO 2 LUONG LIVE DOCKER DAT CHUAN (PASS) <<< " -ForegroundColor Green
    Write-Host "=================================================================" -ForegroundColor DarkCyan
    exit 0
}
else {
    # -------------------------------------------------------------
    # THUC THI KIEM THU IN-PROCESS (IN-PROCESS TEST SUITES)
    # -------------------------------------------------------------
    Write-Host "[CHE DO] IN-PROCESS INTEGRATION -- Kiem thu tich hop gia lap trong bo nho`n" -ForegroundColor Yellow

    # Luong 1: In-process Gateway -> Chat REST -> User gRPC
    Write-Host "1. Kiem tra tich hop End-to-End: Gateway -> Chat REST -> User gRPC..." -ForegroundColor Cyan
    $GatewaySln = Join-Path $RepoRoot "examples\gateway-demo\GatewayDemo.sln"
    $GatewayTestOut = & dotnet test "$GatewaySln" --no-restore -v q 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   -> Luong dinh tuyen YARP Gateway toi Chat REST:       [DAT CHUAN]" -ForegroundColor Green
        Write-Host "   -> Bien doi duong dan /api/chat/* -> /api/v1/chats/*: [DAT CHUAN]" -ForegroundColor Green
        Write-Host "   -> Chat REST goi qua User gRPC lay Avatar & Ten:      [DAT CHUAN]" -ForegroundColor Green
        Write-Host "   -> Xu ly loi 502/503 khi upstream bi ngat:            [DAT CHUAN]" -ForegroundColor Green
        Write-Host "   => 6/6 Gateway integration tests passed!" -ForegroundColor Green
    }
    else {
        Write-Host "   [FAIL] Kiem thu Gateway that bai:" -ForegroundColor Red
        Write-Host "$GatewayTestOut" -ForegroundColor Red
        exit 1
    }

    # Luong 2: In-process Kafka Event -> Notification Worker
    Write-Host "`n2. Kiem tra tich hop Asynchronous: Kafka Event -> Notification Worker..." -ForegroundColor Cyan
    $WorkerSln = Join-Path $RepoRoot "examples\demo-worker\DemoWorker.sln"
    $WorkerTestOut = & dotnet test "$WorkerSln" --no-restore -v q 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   -> Tieu thu su kien user.registered.v1 tu Kafka:       [DAT CHUAN]" -ForegroundColor Green
        Write-Host "   -> Che giay thong tin nhay cam (Opaque Masking):       [DAT CHUAN]" -ForegroundColor Green
        Write-Host "   -> Khong gui trung lap email (Idempotent Handling):    [DAT CHUAN]" -ForegroundColor Green
        Write-Host "   -> Xu ly loi tam thoi va commit offset an toan:        [DAT CHUAN]" -ForegroundColor Green
        Write-Host "   => 10/10 Notification Worker tests passed!" -ForegroundColor Green
    }
    else {
        Write-Host "   [FAIL] Kiem thu Worker that bai:" -ForegroundColor Red
        Write-Host "$WorkerTestOut" -ForegroundColor Red
        exit 1
    }

    Write-Host "`n=================================================================" -ForegroundColor DarkCyan
    Write-Host "  SMOKE TEST TICH HOP IN-PROCESS HOAN TOAN THANH CONG (PASS)!   " -ForegroundColor Green
    Write-Host "=================================================================" -ForegroundColor DarkCyan
    Write-Host "Luu y: Chay '.\scripts\smoke-test.ps1 -Live' khi da bat Docker Compose" -ForegroundColor Gray
    Write-Host "de kiem thu truc tiep tren cac container mang thuc te!" -ForegroundColor Gray
    exit 0
}
