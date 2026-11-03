# Kịch Bản & Bảng Kiểm Tra Buổi Báo Cáo Đồ Án PRN232 (Final ASM Demonstration Checklist)

> **Mã tài liệu**: `CHECKLIST-03-ASM-DEMO`  
> **Dự án**: FPT Alumni Connect (PRN232 Final Assignment)  
> **Trạng thái**: Hoàn thành & Phê duyệt  
> **Mục tiêu**: Chuẩn bị đầy đủ các bước thực nghiệm, lệnh chạy mẫu, kết quả mong đợi và bản đồ khớp nối tiêu chí đánh giá môn học PRN232.

---

## 🎯 1. Bản Đồ Khớp Nối Tiêu Chí Chấm Điểm PRN232 (Evaluation Mapping)

Bảng dưới đây quy chiếu trực tiếp các yêu cầu học phần PRN232 với các thành phần công nghệ đã triển khai:

| Tiêu chí Đánh giá Môn học | Thành phần Triển khai Trong Dự án | Vị trí Mã nguồn & Tài liệu | Cách Trình diễn Trong Buổi Báo cáo |
| :--- | :--- | :--- | :--- |
| **1. REST API & Architecture** | Kiến trúc 3 tầng chuẩn .NET 10 (API $\rightarrow$ Services $\rightarrow$ Repositories), RFC 7807 ProblemDetails, OpenAPI/Swagger. | [`templates/dotnet-service/`](file:///c:/Use/University/PRN232/ASM/guideline/templates/dotnet-service/)<br>[`docs/04-dotnet-standards.md`](file:///c:/Use/University/PRN232/ASM/guideline/docs/04-dotnet-standards.md)<br>[`docs/06-rest-api.md`](file:///c:/Use/University/PRN232/ASM/guideline/docs/06-rest-api.md) | Mở Swagger UI, thực hiện CRUD, lọc và phân trang dữ liệu, cố tình gửi dữ liệu sai để chứng minh lỗi `400 Bad Request`. |
| **2. Database & EF Core** | Cơ sở dữ liệu riêng biệt cho từng service trên SQL Server, EF Core Code-First Migrations, quan hệ theo ID. | [`docs/05-database.md`](file:///c:/Use/University/PRN232/ASM/guideline/docs/05-database.md) | Mở SQL Server Management Studio (SSMS) cho thấy 5 database riêng biệt; chứng minh không dùng Foreign Key giữa các DB. |
| **3. Authentication & JWT** | User Service cấp JWT với claims `sub`, `email`, `role` (`USER`/`ADMIN`). Gateway và các service xác thực token. | [`docs/07-authentication.md`](file:///c:/Use/University/PRN232/ASM/guideline/docs/07-authentication.md) | Gọi API không truyền token $\rightarrow$ nhận `401 Unauthorized`. Đăng nhập với role `USER` gọi API quản trị $\rightarrow$ nhận `403 Forbidden`. |
| **4. gRPC Inter-Service** | Giao tiếp đồng bộ HTTP/2 nhị phân giữa Chat Service và User Service để tra cứu hồ sơ người dùng (Tên & Avatar). | [`examples/grpc-demo/`](file:///c:/Use/University/PRN232/ASM/guideline/examples/grpc-demo/)<br>[`docs/09-grpc.md`](file:///c:/Use/University/PRN232/ASM/guideline/docs/09-grpc.md) | Chat Service gọi endpoint lấy danh sách tin nhắn, tự động gọi qua gRPC lấy thông tin hiển thị của sender; chứng minh fallback khi User Service offline. |
| **5. Asynchronous Events (Kafka)** | Sự kiện `user.registered.v1` được phát lên Kafka Topic khi đăng ký tài khoản mới; partition key = `userId`. | [`examples/kafka-demo/`](file:///c:/Use/University/PRN232/ASM/guideline/examples/kafka-demo/)<br>[`docs/08-kafka.md`](file:///c:/Use/University/PRN232/ASM/guideline/docs/08-kafka.md) | Chạy Producer gửi event, hiển thị payload JSON Envelope với đầy đủ `correlationId` và `eventId`. |
| **6. Background Worker / Job** | .NET BackgroundService tiêu thụ sự kiện từ Kafka, xử lý gửi email chào mừng giả lập, khử trùng lặp (Idempotent). | [`examples/demo-worker/`](file:///c:/Use/University/PRN232/ASM/guideline/examples/demo-worker/)<br>[`docs/10-background-worker.md`](file:///c:/Use/University/PRN232/ASM/guideline/docs/10-background-worker.md) | Xem nhật ký console của Worker khi nhận message, chứng minh mã xác minh được che giấu (`[REDACTED]`) và sự kiện gửi lặp bị bỏ qua. |
| **7. API Gateway (YARP)** | Microsoft YARP Reverse Proxy làm cổng duy nhất cho Web và Mobile tại cổng `5000`, biến đổi đường dẫn URL. | [`examples/gateway-demo/`](file:///c:/Use/University/PRN232/ASM/guideline/examples/gateway-demo/)<br>[`docs/11-api-gateway.md`](file:///c:/Use/University/PRN232/ASM/guideline/docs/11-api-gateway.md) | Gọi `http://localhost:5000/api/chat/conversations/11111111-1111-1111-1111-111111111111/participants/11111111-1111-1111-1111-111111111111`, chứng minh Gateway tự rewrite sang `/api/v1/chats/...` và che giấu port nội bộ `5002`. |
| **8. Containerization (Docker)** | Toàn bộ hệ thống chạy qua Docker Compose trên bridge network `f-alumni-demo-net`. | [`docker/compose.demo.yml`](file:///c:/Use/University/PRN232/ASM/guideline/docker/compose.demo.yml)<br>[`docs/12-docker.md`](file:///c:/Use/University/PRN232/ASM/guideline/docs/12-docker.md) | Chạy `docker compose ps` cho thấy toàn bộ 6 container ở trạng thái `healthy` hoặc `Up`. |

---

## 🎬 2. Kịch Bản Trình Diễn Từng Bước (Step-by-Step Demo Script)

Thời lượng trình diễn khuyến nghị: **10 - 15 phút**.

### 🌟 Bước 1: Khởi Động & Kiểm Tra Sức Khỏe Toàn Hệ Thống (2 phút)

1. Mở PowerShell tại thư mục đồ án:
   ```powershell
   # Chạy kịch bản xác minh toàn diện:
   .\scripts\verify-all.ps1
   ```
2. **Điểm nhấn thuyết trình**: 
   > *"Thưa Thầy/Cô, toàn bộ 5 microservices và dự án mẫu của nhóm đều được kiểm thử tự động với 54 ca kiểm thử đạt tỷ lệ thành công 100%. Các quy chuẩn cấu hình tĩnh, hợp đồng Kafka và Protobuf đều được tự động xác minh trước khi chạy."*
3. Khởi chạy hệ sinh thái container:
   ```powershell
   docker compose --env-file docker/.env -f docker/compose.demo.yml up -d
   docker compose -f docker/compose.demo.yml ps
   ```

---

### 🌟 Bước 2: Trình Diễn Cổng API Gateway & Luồng Đồng Bộ gRPC (4 phút)

1. Kiểm tra sức khỏe của API Gateway:
   ```powershell
   curl http://localhost:5000/health
   # Kết quả mong đợi: "Healthy" (HTTP 200)
   ```
2. Thực hiện gọi API Chat qua Gateway:
   ```powershell
   curl http://localhost:5000/api/chat/conversations/11111111-1111-1111-1111-111111111111/participants/11111111-1111-1111-1111-111111111111
   ```
3. **Phân tích kết quả hiển thị cho Giảng viên**:
   - URL gửi tới là `/api/chat/conversations/.../participants/...` (cổng `5000`).
   - Gateway YARP tự động biến đổi thành `/api/v1/chats/conversations/.../participants/...` và định tuyến ngầm tới `chat-service:5002`.
   - Trong dữ liệu trả về, thông tin người dùng:
     ```json
     {
       "conversationId": "11111111-1111-1111-1111-111111111111",
       "userId": "11111111-1111-1111-1111-111111111111",
       "displayName": "Nguyen Van A",
       "avatarUrl": "https://cdn.falumni.local/avatars/nguyenvana.jpg",
       "status": "Alumni",
       "resolvedVia": "gRPC (HTTP/2 Protobuf)"
     }
     ```
     được Chat Service tra cứu trực tiếp qua **gRPC HTTP/2 nhị phân** từ User Service (`user-grpc-server:50051`) trong thời gian dưới 10ms.
4. **Trình diễn Khả năng Chịu lỗi & Hợp đồng Lỗi**:
   - Gọi ID người dùng không tồn tại (`99999999-9999-9999-9999-999999999999`):
     ```powershell
     curl -i http://localhost:5000/api/chat/conversations/11111111-1111-1111-1111-111111111111/participants/99999999-9999-9999-9999-999999999999
     # Kết quả: HTTP/1.1 404 Not Found theo chuẩn RFC 7807 ProblemDetails
     ```
   - Tạm dừng container User gRPC Server:
     ```powershell
     docker compose -f docker/compose.demo.yml pause user-grpc-server
     ```
   - Gọi lại endpoint: Chat Service không bị sập mà trả về HTTP 504 Gateway Timeout / 503 Service Unavailable an toàn.
   - Bật lại container: `docker compose -f docker/compose.demo.yml unpause user-grpc-server`.

---

### 🌟 Bước 3: Trình Diễn Luồng Bất Đồng Bộ Kafka & Background Worker (4 phút)

1. Mở một cửa sổ terminal theo dõi nhật ký của Notification Worker:
   ```powershell
   docker compose -f docker/compose.demo.yml logs -f notification-worker
   ```
2. Mở cửa sổ terminal thứ hai và kích hoạt Kafka Producer để phát sự kiện đăng ký người dùng mới:
   ```powershell
   # Chạy producer gửi sự kiện mẫu tới Kafka broker (host port 29092):
   dotnet run --project examples/kafka-demo/src/KafkaProducer/KafkaProducer.csproj -- localhost:29092
   ```
3. **Quan sát tại cửa sổ Worker**:
   - Worker ngay lập tức nhận được sự kiện `user.registered.v1`.
   - Dòng log xuất hiện:
     ```text
     info: NotificationWorker.Workers.NotificationBackgroundService[0]
           Received Kafka message from Partition 0 at Offset 0
     info: NotificationWorker.Services.NotificationEventProcessor[0]
           Consuming event: EventId=..., CorrelationId=..., EventType=user.registered.v1, Producer=f-alumni-user-service
     info: NotificationWorker.Services.MockEmailSender[0]
           [MOCK EMAIL DELIVERED] Recipient: st***@fpt.edu.vn | Subject: Chào mừng bạn đến với FPT Alumni Connect — Mã xác thực tài khoản | Provider: DevMock
     info: NotificationWorker.Services.NotificationEventProcessor[0]
           Email dispatched successfully: EventId=..., CorrelationId=...
     ```
4. **Chứng minh Khử Trùng Lặp (Idempotent Handling)**:
   - Gửi lại cùng sự kiện (cùng `eventId`):
   - Worker nhận diện `eventId` đã tồn tại trong bộ nhớ đệm, ghi log:
     ```text
     warn: NotificationWorker.Services.NotificationEventProcessor[0]
           Duplicate event skipped: EventId=..., CorrelationId=... was already processed (Idempotent).
     ```
   - Không có email thứ hai nào được gửi đi!

---

### 🌟 Bước 4: Trình Diễn Bảo Mật JWT & Ranh Giới Database (3 phút)

1. **Bảo mật Endpoint & Quyền Hạn**:
   - Gọi một API quản trị yêu cầu quyền `ADMIN`:
     ```powershell
     curl -i -X POST http://localhost:5000/api/v1/sample-items -H "Content-Type: application/json" -d "{\"name\":\"Test Item\",\"description\":\"Demo\"}"
     # Kết quả: HTTP/1.1 401 Unauthorized
     ```
   - Thêm Header Authorization với token role `USER` gọi API quản trị:
     ```powershell
     # Kết quả: HTTP/1.1 403 Forbidden
     ```
   - Thêm Header với token hợp lệ role `ADMIN`:
     ```powershell
     # Kết quả: HTTP/1.1 201 Created
     ```
2. **Ranh giới Cơ sở Dữ liệu Độc lập**:
   - Trình chiếu cấu hình chuỗi kết nối:
     - User Service kết nối `FptAlumni_User_Db`.
     - Chat Service kết nối `FptAlumni_Chat_Db`.
     - Forum Service kết nối `FptAlumni_Forum_Db`.
   - Giải thích cho Giảng viên: *"Mỗi dịch vụ có thể backup, restore hoặc nâng cấp schema database hoàn toàn độc lập mà không ảnh hưởng tới các dịch vụ còn lại."*

---

### 🌟 Bước 5: Chạy Smoke Test Tự Động & Kết Thúc (1 phút)

1. Chạy kịch bản kiểm tra khói tự động tổng kết:
   ```powershell
   .\scripts\smoke-test.ps1
   ```
2. Khi kết thúc buổi trình bày, tắt container sạch sẽ:
   ```powershell
   docker compose -f docker/compose.demo.yml down
   ```

---

## 📋 3. Bảng Tự Kiểm Tra Trước Giờ G (Pre-Demo Sanity Checklist)

Thực hiện trước buổi thuyết trình 30 phút:

- [ ] Máy tính đã bật **Docker Desktop** (biểu tượng cá voi màu xanh lá cây).
- [ ] Không có ứng dụng nào khác đang chiếm cổng `5000` (Gateway), `14333` (SQL Server), hoặc `29092` (Kafka).
- [ ] Tệp `docker/.env` đã được tạo từ `docker/.env.example`.
- [ ] Đã chạy thử `.\scripts\verify-all.ps1` và tất cả các mục đều hiển thị `[PASS]`.
- [ ] Đã chạy thử `.\scripts\smoke-test.ps1` và nhận thông báo thành công.
- [ ] Đã chuẩn bị sẵn 2 terminal PowerShell (một để chạy lệnh, một để theo dõi logs).
- [ ] Các thành viên trong nhóm đã phân công người nói từng phần (BE1, BE2, BE3).
