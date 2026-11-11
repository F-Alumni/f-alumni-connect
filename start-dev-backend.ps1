# ==============================================================================
# FPT Alumni Connect — Quickstart Script for Developers
# Starts User Service, Chat Service, Forum Service, AI Service, and Admin Service in InMemory Database mode
# ==============================================================================

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "   FPT Alumni Connect -- Microservices Backend Launcher          " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

$jwtKey = "falumni-production-shared-jwt-secret-key-2026-min-32-bytes"
$workDir = $PSScriptRoot
if (-not $workDir) { $workDir = "D:\KI 8\PRN232\Final\f-alumni-connect" }

Write-Host "1. Khoi dong User Service (REST: 5001 | gRPC: 50051)..." -ForegroundColor Yellow
Start-Process powershell -WorkingDirectory $workDir -ArgumentList "-NoExit", "-Command", "`$env:DatabaseProvider='InMemory'; `$env:Jwt__Key='$jwtKey'; dotnet run --project f-alumni-user-service\src\UserService.Api\UserService.Api.csproj"

Start-Sleep -Seconds 3

Write-Host "2. Khoi dong Chat Service (REST & SignalR: 5002)..." -ForegroundColor Yellow
Start-Process powershell -WorkingDirectory $workDir -ArgumentList "-NoExit", "-Command", "`$env:DatabaseProvider='InMemory'; `$env:Jwt__Key='$jwtKey'; dotnet run --project f-alumni-chat-service\src\ChatService.Api\ChatService.Api.csproj"

Start-Sleep -Seconds 3

Write-Host "3. Khoi dong Forum Service (REST & SignalR: 5003)..." -ForegroundColor Yellow
Start-Process powershell -WorkingDirectory $workDir -ArgumentList "-NoExit", "-Command", "`$env:DatabaseProvider='InMemory'; `$env:Jwt__Key='$jwtKey'; dotnet run --project F-Alumni-Forum-Service\src\ForumService.Api\ForumService.Api.csproj"

Start-Sleep -Seconds 3

Write-Host "4. Khoi dong AI Service (REST: 5004)..." -ForegroundColor Yellow
Start-Process powershell -WorkingDirectory $workDir -ArgumentList "-NoExit", "-Command", "`$env:DatabaseProvider='InMemory'; `$env:Jwt__Key='$jwtKey'; `$env:Kafka__Enabled='false'; dotnet run --project F-Alumni-AI-Service\src\AiService.Api\AiService.Api.csproj"

Start-Sleep -Seconds 3

Write-Host "5. Khoi dong Admin Service (REST: 5005)..." -ForegroundColor Yellow
Start-Process powershell -WorkingDirectory $workDir -ArgumentList "-NoExit", "-Command", "`$env:DatabaseProvider='InMemory'; `$env:Jwt__Key='$jwtKey'; dotnet run --project f-alumni-admin-service\src\AdminService.Api\AdminService.Api.csproj"

Start-Sleep -Seconds 3

Write-Host "6. Khoi dong YARP API Gateway (Port 5000 - Cổng thống nhất cho FE)..." -ForegroundColor Yellow
Start-Process powershell -WorkingDirectory $workDir -ArgumentList "-NoExit", "-Command", "`$env:ASPNETCORE_URLS='http://+:5000'; `$env:ReverseProxy__Clusters__user-cluster__Destinations__user-service__Address='http://localhost:5001'; `$env:ReverseProxy__Clusters__chat-cluster__Destinations__chat-service__Address='http://localhost:5002'; `$env:ReverseProxy__Clusters__forum-cluster__Destinations__forum-service__Address='http://localhost:5003'; `$env:ReverseProxy__Clusters__ai-cluster__Destinations__ai-service__Address='http://localhost:5004'; `$env:ReverseProxy__Clusters__admin-cluster__Destinations__admin-service__Address='http://localhost:5005'; dotnet run --project guideline\examples\gateway-demo\src\ApiGateway\ApiGateway.csproj"

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Green
Write-Host "   HE THONG BACKEND DA DUOC KHOI CHAY THANH CONG!                " -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Green
Write-Host "-> API Gateway        : http://localhost:5000 (Unified entrypoint for FE)" -ForegroundColor Cyan
Write-Host "-> User Service API   : http://localhost:5001" -ForegroundColor White
Write-Host "-> User Swagger UI    : http://localhost:5001/swagger" -ForegroundColor White
Write-Host "-> Chat Service API   : http://localhost:5002" -ForegroundColor White
Write-Host "-> Chat Swagger UI    : http://localhost:5002/swagger" -ForegroundColor White
Write-Host "-> SignalR Chat Hub   : http://localhost:5002/hubs/chat" -ForegroundColor White
Write-Host "-> Forum Service API  : http://localhost:5003" -ForegroundColor White
Write-Host "-> SignalR Forum Hub  : http://localhost:5003/hubs/forum" -ForegroundColor White
Write-Host "-> AI Service API     : http://localhost:5004" -ForegroundColor White
Write-Host "-> Admin Service API  : http://localhost:5005" -ForegroundColor White
Write-Host "-> Admin Swagger UI   : http://localhost:5005/swagger" -ForegroundColor White
Write-Host "-----------------------------------------------------------------" -ForegroundColor Gray
Write-Host "TAI KHOAN DANG NHAP KIEM THU (Mat khau chung: Password@123):" -ForegroundColor Yellow
Write-Host "1. nguyenvana@fpt.edu.vn  (Alumni User)" -ForegroundColor Cyan
Write-Host "2. tranthib@fpt.edu.vn    (Student User)" -ForegroundColor Cyan
Write-Host "3. levanc@fpt.edu.vn      (Suspended User - Test 403 Forbidden)" -ForegroundColor Red
Write-Host "4. admin@fpt.edu.vn       (Admin User - Test Admin Portal)" -ForegroundColor Magenta
Write-Host "=================================================================" -ForegroundColor Green
