# 🎓 FPT Alumni Connect — Microservices Backend

> **Hệ sinh thái Microservices kết nối Cựu sinh viên & Sinh viên Đại học FPT**  
> **Môn học**: PRN232 — Advanced .NET Architecture & Cloud Development  
> **Phiên bản**: 1.0.0 (Production / Demo-Ready)  
> **Tiêu chuẩn chất lượng**: 100% Test Passed (205/205), SHA-256 Hợp đồng nguyên bản, Không lỗi CORS, Cơ sở dữ liệu phân tán cách ly hoàn toàn.

---

## 🏛️ 1. Sơ Đồ Kiến Trúc Hệ Thống (System Architecture)

Hệ thống được thiết kế theo kiến trúc **Microservices chuẩn doanh nghiệp**, phân tách rõ ràng giữa API Gateway, các dịch vụ nghiệp vụ độc lập, giao tiếp bất đồng bộ qua **Apache Kafka**, giao tiếp đồng bộ hiệu năng cao qua **gRPC Protobuf**, và truyền tải thời gian thực bằng **SignalR WebSockets**:

```mermaid
flowchart TD
    subgraph Clients ["📱 Tầng Ứng Dụng (Clients)"]
        WebClient["💻 Frontend Web (React 18 / Vite)"]
        MobileClient["📱 Frontend Mobile (Flutter Android/iOS)"]
        AdminPortal["🛡️ Admin Web Portal (React Dashboard)"]
    end

    subgraph Gateway ["🚪 Cổng Vào Duy Nhất (API Gateway)"]
        YarpGateway["🔀 YARP Reverse Proxy (Port 5000)\n• CORS All Allowed\n• Request Routing\n• SignalR Hub Proxy"]
    end

    subgraph Services ["⚙️ Tầng Dịch Vụ Microservices (.NET 10 / ASP.NET Core)"]
        UserService["👤 User Service (Port 5001)\n• Auth JWT & OAuth2\n• Directory & Profiles\n• gRPC Server (Port 50051)"]
        ChatService["💬 Chat Service (Port 5002)\n• 1-on-1 & Group Chats\n• SignalR (/hubs/chat)\n• gRPC Client sang User"]
        ForumService["📰 Forum Service (Port 5003)\n• Bài viết, Bình luận, Vote\n• Sự kiện & Hội thảo\n• SignalR (/hubs/forum)"]
        AiService["🤖 AI Service (Port 5004)\n• RAG Knowledge Engine\n• Gemini Chatbot\n• Mentor Recommendation"]
        AdminService["🛡️ Admin Service (Port 5005)\n• Quản trị thành viên\n• Xử lý vi phạm\n• Thống kê hệ thống"]
    end

    subgraph Messaging ["📨 Hàng Đợi & Sự Kiện (Event-Driven)"]
        KafkaBroker["⚡ Apache Kafka (Port 9092, 29092)\n• user.registered.v1\n• post.created.v1 (Nạp tự động vào AI RAG)"]
    end

    subgraph Databases ["🗄️ CSDL Cách Ly Tuyệt Đối (Database per Service)"]
        DbUser[("📦 FAlumni_User_Db")]
        DbChat[("📦 FAlumni_Chat_Db")]
        DbForum[("📦 FAlumni_Forum_Db")]
        DbAI[("📦 FAlumni_AI_Db")]
        DbAdmin[("📦 FAlumni_Admin_Db")]
    end

    %% Client to Gateway
    WebClient -->|HTTP / WSS| YarpGateway
    MobileClient -->|HTTP / WSS| YarpGateway
    AdminPortal -->|HTTP / WSS| YarpGateway

    %% Gateway to Services
    YarpGateway -->|/api/v1/auth, /api/v1/users| UserService
    YarpGateway -->|/api/v1/chat/*, /hubs/chat| ChatService
    YarpGateway -->|/api/v1/posts/*, /hubs/forum| ForumService
    YarpGateway -->|/api/v1/ai/*| AiService
    YarpGateway -->|/api/v1/admin/*| AdminService

    %% Inter-service
    ChatService -.->|gRPC Protobuf (50051)| UserService
    UserService -.->|Publish user.registered.v1| KafkaBroker
    ForumService -.->|Publish post.created.v1| KafkaBroker
    KafkaBroker -.->|Consume post.created.v1| AiService

    %% Database bindings
    UserService --- DbUser
    ChatService --- DbChat
    ForumService --- DbForum
    AiService --- DbAI
    AdminService --- DbAdmin
```

---

## 🌐 2. Ma Trận Cổng & Dịch Vụ (Port Matrix)

Frontend và Mobile **chỉ cần kết nối qua Cổng API Gateway `http://localhost:5000`**; không cần phải ghi nhớ cổng riêng lẻ của từng service.

| Dịch vụ | Port Ngoài | Giao thức | Nhiệm vụ chính & Tuyến đường API | Database riêng biệt |
| :--- | :---: | :--- | :--- | :--- |
| **API Gateway (YARP)** | **`5000`** | HTTP / WS | Điểm truy cập duy nhất, chuyển tiếp `/api/v1/*` và `/hubs/*` | N/A |
| **User Service** | `5001`<br>`50051` | REST<br>gRPC | Đăng ký, Đăng nhập, Profile, Danh bạ Alumni<br>gRPC Protobuf tra cứu hồ sơ người dùng | `FAlumni_User_Db` |
| **Chat Service** | `5002` | REST<br>SignalR | Quản lý hội thoại, lịch sử tin nhắn<br>Realtime WebSocket: `/hubs/chat` | `FAlumni_Chat_Db` |
| **Forum Service** | `5003` | REST<br>SignalR | Bài viết, Chuyên mục, Bình luận, Sự kiện<br>Realtime WebSocket: `/hubs/forum` | `FAlumni_Forum_Db` |
| **AI Service** | `5004` | REST | RAG Chatbot quy chế & văn hóa FPT, Gợi ý Mentor Alumni | `FAlumni_AI_Db` |
| **Admin Service** | `5005` | REST | Quản trị người dùng, Xử lý tố cáo vi phạm, Báo cáo & Thống kê | `FAlumni_Admin_Db` |
| **Kafka Broker** | `9092`<br>`29092` | TCP | Hệ thống hàng đợi sự kiện mien KRaft (User Registered, Post Created) | N/A |
| **SQL Server 2022** | `14333` | TCP | Hệ quản trị CSDL quan hệ lưu trữ 5 CSDL độc lập | MSSQL Engine |

---

## 🚀 3. Hướng Dẫn Khởi Chạy Nhanh (Quickstart)

### 🌟 Lựa chọn 1: Chạy toàn bộ hệ thống bằng Docker Compose (Khuyến nghị số 1 cho FE & Demo)

Yêu cầu: Máy đã cài đặt **Docker Desktop**.

```powershell
# 1. Clone repository
git clone https://github.com/F-Alumni/f-alumni-connect.git
cd f-alumni-connect

# 2. Khởi chạy toàn bộ 8 containers trong nền
docker compose up -d --build

# 3. Xem logs kiểm tra trạng thái
docker compose logs -f api-gateway
```

> 💡 **Tự động hóa 100%**: Khi khởi động, các service sẽ tự động chạy **EF Core Migrations** tạo bảng và nạp sẵn **Database Seed Data** mẫu (Tài khoản, Phòng chat, Bài viết diễn đàn, Hồ sơ Mentor). Bạn không cần chạy lệnh SQL hay script tạo database thủ công.

---

### 💻 Lựa chọn 2: Chạy trực tiếp trên máy phát triển (.NET 10 SDK)

Dành cho thành viên phụ trách backend muốn debug từng project.

```powershell
# Chạy script PowerShell tự động mở 6 cửa sổ terminal riêng biệt:
.\start-dev-backend.ps1
```

Script sẽ tự động:
1. Kiểm tra kết nối SQL Server và Kafka.
2. Khởi động lần lượt 5 microservices trên các cổng `5001` đến `5005`.
3. Khởi động API Gateway trên cổng `5000`.

---

## 🔑 4. Tài Khoản Kiểm Thử Sẵn Có (Seed Test Accounts)

Toàn bộ tài khoản kiểm thử đã được nạp sẵn với mật khẩu dùng chung: **`Password@123`**

| Email Đăng Nhập | Mật Khẩu | Tên Người Dùng | Vai Trò (Role) | Chuyên Ngành / Chức Danh | Mục Đích Kiểm Thử |
| :--- | :---: | :--- | :---: | :--- | :--- |
| **`admin@fpt.edu.vn`** | `Password@123` | FPT Alumni Admin | `ADMIN` | Quản trị viên hệ thống | Đăng nhập Admin Portal, duyệt bài, khóa tài khoản |
| **`nguyenvana@fpt.edu.vn`** | `Password@123` | Nguyễn Văn A | `USER` | Alumni K17 — Software Engineering (FPT Software) | Đăng nhập tài khoản cựu sinh viên, đăng bài, chat |
| **`tranthib@fpt.edu.vn`** | `Password@123` | Trần Thị B | `USER` | Student K19 — Artificial Intelligence (FPT University) | Đăng nhập song song (trình duyệt ẩn danh) để test chat thời gian thực |
| **`levanc@fpt.edu.vn`** | `Password@123` | Lê Văn C | `USER` | Alumni K16 — Information Assurance (Tài khoản bị khóa) | Kiểm tra xử lý lỗi `403 Forbidden` khi bị Admin phạt vi phạm |

---

## 🧪 5. Kiểm Thử Hệ Thống & Bảo Mật Hợp Đồng

### Chạy toàn bộ 205 Unit & Integration Tests:
```powershell
dotnet test f-alumni-user-service\tests\UserService.Tests\UserService.Tests.csproj
dotnet test f-alumni-chat-service\tests\ChatService.Tests\ChatService.Tests.csproj
dotnet test F-Alumni-Forum-Service\tests\ForumService.Tests\ForumService.Tests.csproj
dotnet test f-alumni-admin-service\tests\AdminService.Tests\AdminService.Tests.csproj
dotnet test F-Alumni-AI-Service\tests\AiService.Tests\AiService.Tests.csproj
dotnet test guideline\examples\gateway-demo\tests\GatewayDemo.Tests\GatewayDemo.Tests.csproj
```
**Kết quả**: `205/205 PASSED (100%)`.

### Xác minh tính toàn vẹn hợp đồng dữ liệu (SHA-256):
```powershell
powershell -ExecutionPolicy Bypass -Command "cd f-alumni-user-service; .\scripts\verify-contracts.ps1"
powershell -ExecutionPolicy Bypass -Command "cd f-alumni-chat-service; .\scripts\verify-contracts.ps1"
powershell -ExecutionPolicy Bypass -Command "cd F-Alumni-Forum-Service; .\scripts\verify-contracts.ps1"
powershell -ExecutionPolicy Bypass -Command "cd F-Alumni-AI-Service; .\scripts\verify-contracts.ps1"
powershell -ExecutionPolicy Bypass -Command "cd f-alumni-admin-service; .\scripts\verify-contracts.ps1"
```
**Kết quả**: Tất cả hợp đồng gRPC Protobuf và sự kiện Kafka đều khớp mã băm SHA-256 nguyên bản 100%.

---

## 📖 6. Tài Liệu Hướng Dẫn Tích Hợp Chi Tiết

- 🌐 [**Hướng dẫn Tích hợp Web Frontend (React / Vite)**](FRONTEND_GUIDE.md): Axios Interceptors, SignalR WebSockets, OpenAPI Specs, Schema Request/Response.
- 📱 [**Hướng dẫn Tích hợp Mobile App (Flutter Android/iOS)**](MOBILE_FLUTTER_GUIDE.md): Cấu hình IP máy ảo `10.0.2.2`, Network Security Config, Dio Interceptor, `signalr_netcore`.

---

## 👥 7. Đóng Góp & Bản Quyền

Dự án thuộc bài tập lớn đồ án chuyên ngành **PRN232 — Đại học FPT**.  
Mọi thắc mắc kỹ thuật vui lòng liên hệ đội ngũ phát triển backend F-Alumni.
