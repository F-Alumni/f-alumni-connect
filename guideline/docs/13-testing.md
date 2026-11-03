# Chiến Lược & Quy Chuẩn Kiểm Thử Hệ Thống (Testing Standards & Verification Guide)

> **Mã tài liệu**: `DOC-13-TESTING`  
> **Dự án**: FPT Alumni Connect (PRN232 Final Assignment)  
> **Trạng thái**: Hoàn thành & Phê duyệt  
> **Công cụ tự động hóa**: [`scripts/verify-all.ps1`](file:///c:/Use/University/PRN232/ASM/guideline/scripts/verify-all.ps1) | [`scripts/smoke-test.ps1`](file:///c:/Use/University/PRN232/ASM/guideline/scripts/smoke-test.ps1)  
> **Bộ kiểm thử mẫu**: [`templates/dotnet-service/tests/`](file:///c:/Use/University/PRN232/ASM/guideline/templates/dotnet-service/tests/) | [`examples/gateway-demo/tests/`](file:///c:/Use/University/PRN232/ASM/guideline/examples/gateway-demo/tests/) | [`examples/demo-worker/tests/`](file:///c:/Use/University/PRN232/ASM/guideline/examples/demo-worker/tests/)

---

## 🎯 1. Triết Lý Kiểm Thử Trong Đồ Án PRN232

Trong một hệ thống phân tán gồm nhiều microservices độc lập, việc kiểm thử đóng vai trò sống còn để đảm bảo:
1. **Tính độc lập của từng dịch vụ**: Mỗi dịch vụ có thể được kiểm thử đơn vị độc lập mà không cần phải bật toàn bộ hệ sinh thái.
2. **Tính toàn vẹn của hợp đồng tích hợp (Contract Integrity)**: Các dịch vụ giao tiếp đúng định dạng JSON REST, Protobuf gRPC nhị phân và Kafka Event Envelope.
3. **Độ tin cậy trong buổi thuyết trình đồ án (ASM Demo Readiness)**: Có kịch bản kiểm thử tự động rõ ràng để chứng minh cho giảng viên thấy hệ thống hoạt động ổn định và xử lý lỗi chuyên nghiệp.

---

## 🏗️ 2. Tháp Kiểm Thử (Testing Pyramid) Của Dự Án

```mermaid
flowchart TD
    E2E["3. End-to-End Smoke Tests\n(Docker Compose / scripts/smoke-test.ps1)\nGateway -> Chat REST -> User gRPC\nKafka -> Notification Worker"]
    Integration["2. Service Integration Tests\n(WebApplicationFactory / InMemory EF Core)\nREST Controllers, JWT Auth, YARP Transforms"]
    Unit["1. Unit Tests\n(xUnit / InMemory / Moq)\nDomain Services, Business Rules, Event Deserialization"]

    Unit --> Integration
    Integration --> E2E
```

---

## 🧪 3. Quy Chuẩn Kiểm Thử Từng Tầng

### 3.1. Kiểm Thử Đơn Vị Tầng Nghiệp Vụ (Service Unit Tests)
- **Mục tiêu**: Kiểm tra logic nghiệp vụ trong lớp `*Service.cs` độc lập với cơ sở dữ liệu và Web API.
- **Kỹ thuật**: Sử dụng interface repository (`ISampleItemRepository`) để giả lập dữ liệu mẫu (mock/stub) mà không cần gọi SQL Server thật.
- **Vị trí mẫu**: [`templates/dotnet-service/tests/Template.Tests/Services/SampleItemServiceTests.cs`](file:///c:/Use/University/PRN232/ASM/guideline/templates/dotnet-service/tests/Template.Tests/Services/SampleItemServiceTests.cs).

### 3.2. Kiểm Thử Tích Hợp API Với `WebApplicationFactory`
- **Mục tiêu**: Kiểm tra toàn bộ luồng HTTP Request từ Routing, Model Validation, Middleware, Authorization đến HTTP Response.
- **Quy tắc quan trọng**: Để bộ test chạy độc lập, không phụ thuộc vào trạng thái SQL Server trên máy chấm điểm:
  - Sử dụng [`CustomWebApplicationFactory`](file:///c:/Use/University/PRN232/ASM/guideline/templates/dotnet-service/tests/Template.Tests/Endpoints/CustomWebApplicationFactory.cs) cấu hình `DatabaseProvider = InMemory`.
  - Kiểm tra các trường hợp phân quyền:
    - Token hợp lệ `USER` $\rightarrow$ `201 Created`.
    - Token thiếu quyền `USER` cố gọi API quản trị $\rightarrow$ `403 Forbidden`.
    - Không truyền Header `Authorization` $\rightarrow$ `401 Unauthorized`.
    - Dữ liệu Request DTO không hợp lệ $\rightarrow$ `400 Bad Request ProblemDetails`.

### 3.3. Kiểm Thử Giao Tiếp Đồng Bộ gRPC (Chat $\rightarrow$ User)
- **Mục tiêu**: Đảm bảo Chat REST API gọi đúng Protobuf Client và xử lý trơn tru cả khi User gRPC Server trả về kết quả lẫn khi server gặp sự cố.
- **Vị trí mẫu**: [`examples/grpc-demo/tests/GrpcDemo.Tests/ChatServiceGrpcIntegrationTests.cs`](file:///c:/Use/University/PRN232/ASM/guideline/examples/grpc-demo/tests/GrpcDemo.Tests/ChatServiceGrpcIntegrationTests.cs).
- **Các ca kiểm thử bắt buộc**:
  1. Người dùng tồn tại $\rightarrow$ Trả về tên hiển thị và avatar đầy đủ.
  2. Người dùng không tồn tại (`Status.NotFound`) $\rightarrow$ Chat Service hiển thị tên ẩn danh dự phòng ("Anonymous User") thay vì gây sập ứng dụng.
  3. User Service gRPC ngắt kết nối (`Status.Unavailable`) $\rightarrow$ Chat Service kích hoạt cơ chế Fallback an toàn.

### 3.4. Kiểm Thử Sự Kiện Bất Đồng Bộ Kafka & Worker
- **Mục tiêu**: Đảm bảo Worker tiêu thụ đúng Envelope `user.registered.v1`, gửi email giả lập và không gửi trùng lặp.
- **Vị trí mẫu**: [`examples/demo-worker/tests/DemoWorker.Tests/NotificationWorkerTests.cs`](file:///c:/Use/University/PRN232/ASM/guideline/examples/demo-worker/tests/DemoWorker.Tests/NotificationWorkerTests.cs).
- **Các ca kiểm thử bắt buộc**:
  1. Sự kiện hợp lệ $\rightarrow$ Tiêu thụ thành công, ghi log gửi email che giấu token an toàn.
  2. Sự kiện bị gửi lặp lại (Duplicate `eventId`) $\rightarrow$ Bỏ qua không gửi email lần 2 (Idempotent Handling).
  3. Lỗi dịch vụ gửi mail tạm thời $\rightarrow$ Bắt lỗi và không commit offset lỗi.

### 3.5. Kiểm Thử Định Tuyến API Gateway (YARP)
- **Mục tiêu**: Đảm bảo YARP chuyển tiếp đúng địa chỉ upstream và biến đổi đường dẫn URL.
- **Vị trí mẫu**: [`examples/gateway-demo/tests/GatewayDemo.Tests/GatewayRoutingTests.cs`](file:///c:/Use/University/PRN232/ASM/guideline/examples/gateway-demo/tests/GatewayDemo.Tests/GatewayRoutingTests.cs).
- **Các ca kiểm thử bắt buộc**:
  1. Route rút gọn `/api/chat/rooms/{id}/messages` biến đổi thành `/api/v1/chats/rooms/{id}/messages`.
  2. Upstream microservice bị tắt $\rightarrow$ Gateway trả về `502 Bad Gateway` theo chuẩn RFC 7807 ProblemDetails.

### 3.6. Kiểm Thử Tích Hợp Cơ Sở Dữ Liệu SQL Server Thật
- **Mục tiêu**: Đảm bảo toàn bộ luồng CRUD, EF Core Migrations, ràng buộc dữ liệu quan hệ (Data Constraints), phân trang (`OFFSET ... FETCH NEXT`) và sắp xếp hoạt động chính xác trên Microsoft SQL Server thật.
- **Vị trí mẫu**: [`templates/dotnet-service/tests/Template.Tests/Database/SampleItemSqlServerIntegrationTests.cs`](file:///c:/Use/University/PRN232/ASM/guideline/templates/dotnet-service/tests/Template.Tests/Database/SampleItemSqlServerIntegrationTests.cs).
- **Nguyên tắc an toàn & cách ly dữ liệu**:
  1. **Chỉ kết nối database test riêng biệt**: Toàn bộ kiểm thử được giới hạn nghiêm ngặt trong database `FptAlumni_Guideline_Test_Db`. Lớp [`SqlServerTestEnvironment`](file:///c:/Use/University/PRN232/ASM/guideline/templates/dotnet-service/tests/Template.Tests/Infrastructure/SqlServerTestEnvironment.cs) sẽ chặn ngay lập tức nếu phát hiện kết nối trỏ tới database nghiệp vụ.
  2. **Tách biệt kiểm thử linh hoạt qua `[SqlServerFact]`**: Khi chạy `dotnet test` bình thường trên máy không có SQL Server, 7 bài kiểm thử SQL Server sẽ tự động được đánh dấu **SKIP** an toàn mà không làm gãy quy trình CI/CD.
  3. **Kích hoạt kiểm thử SQL Server thật**:
     ```powershell
     # Cách 1: Sử dụng cờ kích hoạt cục bộ (mặc định trỏ tới localhost\SQLEXPRESS)
     $env:RUN_SQLSERVER_TESTS = "true"
     dotnet test templates/dotnet-service/Template.sln

     # Cách 2: Cung cấp chuỗi kết nối tùy chỉnh qua biến môi trường
     $env:TEST_SQLSERVER_CONNECTION_STRING = "Server=localhost\SQLEXPRESS;Database=FptAlumni_Guideline_Test_Db;Trusted_Connection=True;TrustServerCertificate=True;"
     dotnet test templates/dotnet-service/Template.sln --filter "Category=SqlServerIntegration"
     ```
  4. **Tự dọn dẹp dữ liệu (Self-cleaning)**: Mỗi test theo dõi danh sách GUID do chính nó tạo ra và xóa sạch trong `DisposeAsync`, không làm ảnh hưởng đến dữ liệu khác và không bao giờ tự ý drop database.

---

## ⚡ 4. Bộ Công Cụ Tự Động Hóa Kiểm Thử Của Dự Án

Nhóm đồ án đã xây dựng sẵn 2 kịch bản PowerShell tự động để phục vụ kiểm thử:

### 4.1. Kịch Bản Xác Minh Toàn Diện (`scripts/verify-all.ps1`)
Chạy kiểm tra toàn bộ 5 solution, kiểm tra cấu hình tĩnh và chạy toàn bộ bộ kiểm thử tự động:

```powershell
# Thực hiện toàn bộ kiểm tra:
.\scripts\verify-all.ps1

# Chỉ kiểm tra biên dịch và cấu hình, bỏ qua chạy test:
.\scripts\verify-all.ps1 -SkipTests

# Dừng ngay khi gặp lỗi đầu tiên:
.\scripts\verify-all.ps1 -FailFast
```

**Bảng Tổng Hợp Kiểm Thử Trong `verify-all.ps1`**:
| Solution | Dự án Kiểm Thử | Số lượng Tests (Mặc định) | Số lượng Tests (Kèm SQL Server) | Trạng thái |
| :--- | :--- | :---: | :---: | :---: |
| `Template.sln` | `Template.Tests` | 36 passed (7 skipped) | 43 passed (0 skipped) | **PASS** |
| `KafkaDemo.sln` | `KafkaDemo.Tests` | 10 | 10 | **PASS** |
| `GrpcDemo.sln` | `GrpcDemo.Tests` | 8 | 8 | **PASS** |
| `DemoWorker.sln` | `DemoWorker.Tests` | 10 | 10 | **PASS** |
| `GatewayDemo.sln` | `GatewayDemo.Tests` | 6 | 6 | **PASS** |
| **Tổng cộng** | **5 Test Projects** | **70 Passed** | **77 Passed** | **100% PASS** |

### 4.2. Kịch Bản Smoke Test Tích Hợp (`scripts/smoke-test.ps1`)
Chạy kiểm tra luồng tích hợp xuyên suốt giữa các microservices:

```powershell
# 1. Kiem tra truc tiep tren he thong Docker Compose dang chay:
.\scripts\smoke-test.ps1 -Live

# 2. Kiem tra gia lap in-process bang bo test suite WebApplicationFactory:
.\scripts\smoke-test.ps1 -InProcess

# 3. Tu dong nhan dien moi truong (Auto-detection):
.\scripts\smoke-test.ps1
```

- **Khi chọn cờ `-Live`**: Bắt buộc kiểm tra trực tiếp trên container Docker. Nếu Docker Desktop chưa bật hoặc container chưa sẵn sàng, kịch bản báo `[NOT VERIFIED]` và trả về mã lỗi kết thúc khác 0. **Tuyệt đối không tự động fallback sang In-Process để tránh ngụy tạo kết quả kiểm thử thực tế.**
- **Khi chọn cờ `-InProcess`**: Chạy 16 ca kiểm thử tích hợp nội tại (`GatewayDemo.Tests` và `DemoWorker.Tests`) độc lập hoàn toàn với Docker.

---

## 🎓 5. Hướng Dẫn Trình Diễn Kiểm Thử Trong Buổi Báo Cáo Đồ Án (ASM Defense)

Khi trình bày với Hội đồng Giảng viên, các nhóm sinh viên hãy thực hiện theo trình tự 4 bước sau để đạt điểm tối đa:

1. **Bước 1 — Chứng minh Kiểm thử Đơn vị & Tự động hóa**:
   - Mở terminal và chạy lệnh `.\scripts\verify-all.ps1`.
   - Giới thiệu với giảng viên: Hệ thống có 70 bài kiểm thử tự động (và 7 bài kiểm thử SQL Server thực tế) bao phủ toàn bộ các khía cạnh kiến trúc (REST, EF Core, JWT, gRPC, Kafka, Gateway).
2. **Bước 2 — Trình diễn Luồng Đồng bộ (REST + gRPC)**:
   - Gửi request tới Gateway: `GET http://localhost:5000/api/chat/conversations/11111111-1111-1111-1111-111111111111/participants/11111111-1111-1111-1111-111111111111`.
   - Chỉ ra rằng: Gateway đã rewrite đường dẫn từ `/api/chat/...` sang `/api/v1/chats/...`, Chat Service đã nhận request và gọi sang User gRPC Server qua HTTP/2 nhị phân để lấy Avatar và Tên của cựu sinh viên.
3. **Bước 3 — Trình diễn Luồng Bất đồng bộ (Kafka + Background Worker)**:
   - Chạy Producer gửi sự kiện `user.registered.v1`:
     ```powershell
     dotnet run --project examples/kafka-demo/src/KafkaProducer/KafkaProducer.csproj -- localhost:29092
     ```
   - Mở nhật ký của Notification Worker (`docker compose -f docker/compose.demo.yml logs -f notification-worker`) để thấy thông báo giả lập gửi email chào mừng với mã xác minh đã được bảo mật.
4. **Bước 4 — Trình diễn Tính Chịu Lỗi (Fault Tolerance)**:
   - Tạm dừng container `user-grpc-server` (`docker compose -f docker/compose.demo.yml pause user-grpc-server`).
   - Gửi lại request chat: Hệ thống không bị crash mà Chat Service xử lý ngoại lệ an toàn trả về ProblemDetails (HTTP 504/503).
   - Bật lại: `docker compose -f docker/compose.demo.yml unpause user-grpc-server`.
