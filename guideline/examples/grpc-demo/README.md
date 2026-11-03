# FPT Alumni Connect — gRPC Profile Lookup Demo

Dự án mẫu minh họa giao tiếp đồng bộ hiệu năng cao qua **gRPC (HTTP/2 + Protocol Buffers)** giữa hai dịch vụ độc lập: **Chat Service** (Client) và **User Service** (Server).

---

## 📁 Cấu trúc Dự án

```text
examples/grpc-demo/
├── GrpcDemo.sln
├── src/
│   ├── GrpcDemo.Contracts/    # Sinh code C# từ contracts/grpc/user-profile/v1/user_profile.proto
│   ├── UserGrpcServer/        # gRPC Server (Kestrel HTTP/2) cung cấp UserProfileService
│   │   ├── Services/UserProfileGrpcService.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   └── ChatRestApi/           # REST API (Client) gọi UserGrpcServer đồng bộ và trả về DTO tổng hợp
│       ├── Controllers/ChatParticipantsController.cs
│       ├── Models/ConversationParticipantResponse.cs
│       ├── Program.cs
│       └── appsettings.json
└── tests/
    └── GrpcDemo.Tests/        # 8 Unit & Integration Tests (Success, NotFound 404, Unavailable 503, Timeout 504, Live TCP Socket)
        └── ProfileLookupIntegrationTests.cs
```

---

## 🚀 Hướng dẫn Chạy Kiểm thử Tự động

Chạy toàn bộ 8 bài test kiểm tra luồng từ đầu đến cuối (E2E), xử lý mã lỗi và kết nối socket trực tiếp:

```powershell
dotnet test examples/grpc-demo/GrpcDemo.sln
```

Kết quả mong đợi:
```text
Passed!  - Failed: 0, Passed: 8, Skipped: 0, Total: 8, Duration: 3 s - GrpcDemo.Tests.dll
```

---

## 📡 Hướng dẫn Chạy Trực tiếp (Live Server-to-Client Demo)

### Bước 1: Khởi động User gRPC Server
Mở Terminal 1:
```powershell
dotnet run --project examples/grpc-demo/src/UserGrpcServer
```
Server sẽ lắng nghe trên: `http://localhost:50051` (Giao thức HTTP/2).

### Bước 2: Khởi động Chat REST API
Mở Terminal 2:
```powershell
dotnet run --project examples/grpc-demo/src/ChatRestApi --urls "http://localhost:50052"
```
REST API sẽ lắng nghe trên: `http://localhost:50052`.

### Bước 3: Gửi yêu cầu kiểm tra từ Client (PowerShell / cURL / Browser)

#### 1. Tra cứu người dùng thành công (User 1 - Nguyen Van A):
```powershell
curl http://localhost:50052/api/v1/chats/conversations/99999999-9999-9999-9999-999999999999/participants/11111111-1111-1111-1111-111111111111
```
Phản hồi JSON (200 OK):
```json
{
  "conversationId": "99999999-9999-9999-9999-999999999999",
  "userId": "11111111-1111-1111-1111-111111111111",
  "displayName": "Nguyen Van A",
  "avatarUrl": "https://cdn.falumni.local/avatars/nguyenvana.jpg",
  "status": "Alumni",
  "resolvedVia": "gRPC (HTTP/2 Protobuf)",
  "retrievedAtUtc": "2026-10-24T09:50:00Z"
}
```

#### 2. Tra cứu người dùng không tồn tại (User Not Found):
```powershell
curl -i http://localhost:50052/api/v1/chats/conversations/99999999-9999-9999-9999-999999999999/participants/00000000-0000-0000-0000-000000000000
```
Phản hồi (404 Not Found):
```json
{
  "type": "https://httpstatuses.com/404",
  "title": "Participant Profile Not Found",
  "status": 404,
  "detail": "User profile with ID '00000000-0000-0000-0000-000000000000' was not found."
}
```

#### 3. Khi User gRPC Server bị tắt hoặc mất mạng (Service Unavailable):
Tắt Terminal 1 (Ctrl+C), sau đó gửi lại request. Chat REST API bắt ngoại lệ `StatusCode.Unavailable` và trả về:
```json
{
  "type": "https://httpstatuses.com/503",
  "title": "User Profile Service Unavailable",
  "status": 503,
  "detail": "The upstream User Service gRPC endpoint is unreachable or currently unavailable."
}
```

---

## 📖 Tài liệu Chi tiết

Xem chi tiết kiến trúc gRPC, so sánh REST vs gRPC, và quy tắc bảo mật ranh giới dịch vụ tại:
- [docs/09-grpc.md](file:///c:/Use/University/PRN232/ASM/guideline/docs/09-grpc.md)
- [contracts/grpc/user-profile/v1/](file:///c:/Use/University/PRN232/ASM/guideline/contracts/grpc/user-profile/v1/)
