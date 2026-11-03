# FPT Alumni Connect — YARP API Gateway Demo

Dự án mẫu minh họa cổng **API Gateway** tập trung sử dụng thư viện **YARP (Yet Another Reverse Proxy)** của Microsoft trên nền tảng .NET 10.

---

## 📁 Cấu trúc Thư mục

```text
examples/gateway-demo/
├── GatewayDemo.sln
├── src/
│   └── ApiGateway/            # Dự án Reverse Proxy ASP.NET Core (.NET 10)
│       ├── Program.cs         # Khởi tạo YARP & Health Check
│       ├── appsettings.json   # Cấu hình Route Mapping & Cluster Destinations
│       └── Dockerfile         # Dockerfile đóng gói container gateway
└── tests/
    └── GatewayDemo.Tests/     # 6 bài kiểm thử tự động (Route rewrite, E2E gRPC forwarding, 404, 502/503)
        └── GatewayRoutingTests.cs
```

---

## 🚀 Hướng dẫn Chạy Kiểm thử Tự động

```powershell
dotnet test examples/gateway-demo/GatewayDemo.sln
```

Kết quả mong đợi:
```text
Passed!  - Failed: 0, Passed: 6, Skipped: 0, Total: 6, Duration: 3 s - GatewayDemo.Tests.dll
```

---

## 📡 Cấu hình Định tuyến YARP (`appsettings.json`)

Gateway cung cấp 2 tuyến đường chính:
1. `/api/v1/chats/{**remainder}`: Chuyển tiếp trực tiếp sang Chat Service (`http://chat-service:5002`).
2. `/api/chat/{**remainder}`: Biến đổi đường dẫn (Path Pattern Transform) thành `/api/v1/chats/{remainder}` và chuyển tiếp sang Chat Service.

Chi tiết xem tại: [docs/11-api-gateway.md](file:///c:/Use/University/PRN232/ASM/guideline/docs/11-api-gateway.md).
