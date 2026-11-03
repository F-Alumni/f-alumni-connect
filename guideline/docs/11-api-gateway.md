# Quy chuẩn Cổng API Tập trung với YARP (Central API Gateway Standards)

> **Mã tài liệu**: `DOC-11-API-GATEWAY`  
> **Dự án**: FPT Alumni Connect (PRN232 Final Assignment)  
> **Trạng thái**: Hoàn thành & Phê duyệt  
> **Công nghệ triển khai**: ASP.NET Core .NET 10 & Microsoft YARP (`Yarp.ReverseProxy` 2.3.0)  
> **Cổng dịch vụ (Port)**: `5000` (HTTP/1.1 & HTTP/2)  
> **Dự án mẫu thực tế**: [`examples/gateway-demo/`](file:///c:/Use/University/PRN232/ASM/guideline/examples/gateway-demo/) | **Docker Compose**: [`docker/compose.demo.yml`](file:///c:/Use/University/PRN232/ASM/guideline/docker/compose.demo.yml)

---

## 🎯 1. Vai trò của API Gateway trong Kiến trúc Microservices

Trong hệ thống **FPT Alumni Connect**, các ứng dụng khách hàng (Web Portal React 18 và Mobile App Flutter) **KHÔNG BAO GIỜ** kết nối trực tiếp đến từng dịch vụ nội bộ (User Service, Chat Service, Forum Service,...). Toàn bộ lưu lượng truy cập công khai đều phải đi qua **Cổng API Gateway Tập trung (Single Entrypoint)**.

```mermaid
flowchart TD
    subgraph Clients["Tầng Khách Hàng (Public Clients)"]
        Web["Web Portal\n(React 18 / Vite)\nhttp://localhost:3000"]
        Mobile["Mobile App\n(Flutter)\nAndroid / iOS"]
    end

    subgraph Perimeter["Ranh Giới Bảo Mật Mạng (Security Perimeter)"]
        Gateway["YARP API Gateway\nhttp://localhost:5000\n[CORS, JWT Offload, Rate Limit]"]
    end

    subgraph InternalServices["Mạng Nội Bộ Docker Bridge (Private Cluster Network)"]
        UserSvc["User Service (:5001)\n[REST & gRPC Server]"]
        ChatSvc["Chat Service (:5002)\n[REST & SignalR Hub]"]
        ForumSvc["Forum Service (:5003)\n[REST & SignalR Hub]"]
        AiSvc["AI Service (:5004)\n[REST API]"]
        AdminSvc["Admin Service (:5005)\n[REST API]"]
    end

    Web -->|HTTP / JSON| Gateway
    Mobile -->|HTTP / JSON| Gateway

    Gateway -->|/api/v1/users/*| UserSvc
    Gateway -->|/api/v1/chats/*| ChatSvc
    Gateway -->|/api/chat/* (Transformed)| ChatSvc
    Gateway -->|/api/v1/forum/*| ForumSvc
    Gateway -->|/api/v1/ai/*| AiSvc
    Gateway -->|/api/v1/admin/*| AdminSvc
```

### 1.1. Lợi ích Cốt lõi của API Gateway đối với Nhóm Phát triển

1. **Điểm truy cập duy nhất (Single URL for Clients)**:
   - Web và Mobile chỉ cần cấu hình đúng 1 địa chỉ `BASE_URL = http://localhost:5000` (hoặc domain production), không cần phải nhớ hàng loạt port `5001`, `5002`, `5003`,...
2. **Che giấu cấu trúc mạng nội bộ (Topology Hiding)**:
   - Các microservices chạy an toàn bên trong mạng riêng ảo (Docker bridge network). Bên ngoài Internet không thể quét được địa chỉ IP hay port của các dịch vụ bên trong.
3. **Tập trung hóa các mối quan tâm dùng chung (Cross-Cutting Concerns)**:
   - **Xác thực JWT**: Kiểm tra chữ ký và tính hợp lệ của token ngay tại Gateway trước khi cho phép request đi tiếp.
   - **CORS (Cross-Origin Resource Sharing)**: Cấu hình tập trung tại Gateway, giải phóng các backend developer khỏi lỗi CORS phức tạp.
   - **Rate Limiting & Throttling**: Chống spam request và giảm tải cho backend microservices.

---

## ⚙️ 2. Công nghệ Microsoft YARP (Yet Another Reverse Proxy)

Nhóm đồ án sử dụng **YARP** — thư viện reverse proxy mã nguồn mở hiệu năng cực cao do chính Microsoft phát triển riêng cho hệ sinh thái .NET.

### 2.1. Cấu hình Định tuyến trong `appsettings.json`

YARP tách biệt rõ ràng giữa hai khái niệm:
- **Routes**: Quy tắc so khớp đường dẫn URL (Path Matching) và các phép biến đổi (Transforms).
- **Clusters**: Nhóm các địa chỉ máy chủ đích (Destinations) sẽ tiếp nhận request được chuyển tiếp.

```json
{
  "ReverseProxy": {
    "Routes": {
      "chat-route-v1": {
        "ClusterId": "chat-cluster",
        "Match": {
          "Path": "/api/v1/chats/{**remainder}"
        }
      },
      "chat-route-short": {
        "ClusterId": "chat-cluster",
        "Match": {
          "Path": "/api/chat/{**remainder}"
        },
        "Transforms": [
          {
            "PathPattern": "/api/v1/chats/{**remainder}"
          }
        ]
      }
    },
    "Clusters": {
      "chat-cluster": {
        "Destinations": {
          "chat-service": {
            "Address": "http://chat-service:5002"
          }
        }
      }
    }
  }
}
```

### 2.2. Phép Biến đổi Đường dẫn (Path Pattern Transform)
- Để hỗ trợ cả endpoint chuẩn `/api/v1/chats/*` lẫn endpoint ngắn gọn `/api/chat/*`, YARP sử dụng khối `Transforms`:
  - Request: `GET /api/chat/conversations/123/participants/456`
  - YARP tự động biến đổi thành: `GET /api/v1/chats/conversations/123/participants/456`
  - Chuyển tiếp đến `Chat Service` tại `http://chat-service:5002` mà không cần Chat Service phải khai báo thêm route trùng lặp trong code.

---

## 🗺️ 3. Ma trận Định tuyến Toàn hệ thống (Gateway Routing Matrix)

Bảng phân bổ định tuyến qua API Gateway cho 5 dịch vụ nghiệp vụ chính:

| Tiền tố Đường dẫn (Prefix) | Dịch vụ Đích (Backend Cluster) | Port Nội bộ | Mô tả Nghiệp vụ |
| :--- | :--- | :---: | :--- |
| `/api/v1/auth/*`<br>`/api/v1/users/*` | `user-service` | `5001` | Đăng ký, đăng nhập, cấp JWT, danh bạ hồ sơ cựu sinh viên |
| `/api/v1/chats/*`<br>`/api/chat/*` | `chat-service` | `5002` | Cuộc hội thoại 1-1, nhóm chat, tra cứu participant |
| `/api/v1/forum/*` | `forum-service` | `5003` | Bài đăng, bình luận, tương tác like, nhóm sở thích |
| `/api/v1/ai/*` | `ai-service` | `5004` | Trợ lý hỏi đáp FAQ, gợi ý kết nối mentor cựu sinh viên |
| `/api/v1/admin/*` | `admin-service` | `5005` | Quản trị hệ thống, kiểm duyệt nội dung, audit log |
| `/health` | *Xử lý tại Gateway* | `5000` | Kiểm tra tình trạng sống (Liveness/Readiness probe) của Gateway |

---

## 🛡️ 4. Xử lý Lỗi & Truyền đạt Phản hồi (Error Propagation)

Khi gọi qua API Gateway, nếu service nội bộ trả về lỗi (ví dụ 404 ProblemDetails khi không tìm thấy người dùng, hoặc 500 lỗi nội bộ):
- YARP **giữ nguyên toàn bộ HTTP Status Code và nội dung JSON (`ProblemDetails`)** từ backend service và chuyển tiếp nguyên vẹn về cho Web/Mobile.
- Nếu service nội bộ bị tắt (ví dụ Chat Service bị sập): YARP sẽ trả về **`502 Bad Gateway`** hoặc **`503 Service Unavailable`** cho client biết rằng hạ tầng backend đang gặp sự cố kết nối.

---

## 💻 5. Hướng dẫn Chạy Kiểm thử Gateway Demo (`examples/gateway-demo/`)

Dự án kiểm thử tự động độc lập được đặt tại [`examples/gateway-demo/`](file:///c:/Use/University/PRN232/ASM/guideline/examples/gateway-demo/).

```powershell
# Chạy 6 unit & integration tests kiểm tra chuyển tiếp YARP và gRPC
dotnet test "examples/gateway-demo/GatewayDemo.sln"
```

Kết quả mong đợi:
```text
Passed!  - Failed: 0, Passed: 6, Skipped: 0, Total: 6, Duration: 3 s - GatewayDemo.Tests.dll
```
