# Bảng Kiểm Tra Tích Hợp Giữa Các Microservices (Inter-Service Integration Checklist)

> **Mã tài liệu**: `CHECKLIST-02-INTEGRATION`  
> **Dự án**: FPT Alumni Connect (PRN232 Final Assignment)  
> **Mục đích**: Đảm bảo việc kết nối giữa 5 microservices, API Gateway, Apache Kafka và Background Worker diễn ra trơn tru, không xung đột hợp đồng trước khi hợp nhất vào nhánh `develop`.  
> **Phân công trách nhiệm tích hợp**:  
> - **BE1**: User Service (gRPC Server, Auth JWT, Kafka Producer) & Chat Service (gRPC Client, SignalR Hub).  
> - **BE2**: Forum Service (SignalR Hub, Kafka Producer) & AI Service (Kafka Consumer, FAQ API).  
> - **BE3**: Admin Service (Dashboard API, Audit Log) & Hạ tầng (YARP Gateway, Docker Compose, Notification Background Worker).  
> - **FE & Mobile**: Tích hợp REST qua API Gateway (`http://localhost:5000`) và WebSockets SignalR.

---

## 🚪 1. Cổng API Gateway Tập Trung (YARP Reverse Proxy)

- [ ] **Khai báo Route và Cluster trong `appsettings.json`**:
  - [ ] Đã thêm cụm Cluster trỏ tới tên DNS nội bộ container của dịch vụ (ví dụ: `http://forum-service:5003`).
  - [ ] Đường dẫn Route công khai tuân thủ quy ước: `/api/v1/<service-name>/{**remainder}`.
  - [ ] Các route rút gọn (nếu có) phải cấu hình phép biến đổi URL (`Transforms`) hợp lệ.
- [ ] **Ranh giới Bảo Mật & Cross-Cutting**:
  - [ ] Web Portal (React 18) và Mobile App (Flutter) chỉ trỏ tới duy nhất `http://localhost:5000`.
  - [ ] Đã cấu hình CORS tập trung tại Gateway cho phép nguồn gốc client hợp lệ.
  - [ ] Endpoint `/health` của Gateway phản hồi `200 Healthy`.
  - [ ] Khi microservice đích bị tắt hoặc lỗi, Gateway trả về mã `502 Bad Gateway` với ProblemDetails chuẩn.

---

## ⚡ 2. Giao Tiếp Đồng Bộ gRPC (Chat Service $\rightarrow$ User Service)

- [ ] **Hợp đồng Protobuf Chuẩn Hóa & Toàn Vẹn**:
  - [ ] Sử dụng đúng hợp đồng mẫu tại [`contracts/grpc/user-profile/v1/user_profile.proto`](file:///c:/Use/University/PRN232/ASM/guideline/contracts/grpc/user-profile/v1/user_profile.proto).
  - [ ] Hợp đồng được nhập khẩu qua `scripts/import-contracts.ps1` và kiểm tra toàn vẹn bằng `scripts/verify-contracts.ps1` (100% `[PASS]`).
  - [ ] Không có đường dẫn tương đối xuyên repository (`..\..\..\..\contracts`) trỏ ra ngoài; file `.csproj` chỉ trỏ hợp đồng cục bộ (Xem [`docs/15-contract-sharing.md`](file:///c:/Use/University/PRN232/ASM/guideline/docs/15-contract-sharing.md)).
  - [ ] Không tự ý thêm trường bắt buộc phá vỡ tính tương thích ngược (Backward Compatibility).
- [ ] **Bảo Toàn Giao Thức HTTP/2 Binary**:
  - [ ] User Service Kestrel cấu hình rõ ràng giao thức `Http2` cho cổng gRPC (`50051`).
  - [ ] Chat Service khởi tạo kênh `GrpcChannel.ForAddress("http://user-grpc-server:50051")`.
- [ ] **Xử Lý Lỗi & Khả Năng Chịu Lỗi (Fault Tolerance)**:
  - [ ] Xử lý trường hợp không tìm thấy hồ sơ người dùng (`RpcException` với `StatusCode.NotFound`).
  - [ ] Xử lý trường hợp mất kết nối mạng (`StatusCode.Unavailable`) bằng cơ chế Fallback (trả về thông tin mặc định thay vì sập API).
  - [ ] Đã thiết lập thời gian chờ tối đa (Deadline / Timeout) cho mỗi cuộc gọi gRPC.

---

## 📬 3. Sự Kiện Bất Đồng Bộ Qua Apache Kafka

- [ ] **Cấu Trúc JSON Event Envelope Chuẩn & Toàn Vẹn**:
  - [ ] Sự kiện tuân thủ schema tại [`contracts/events/user-registered-v1.json`](file:///c:/Use/University/PRN232/ASM/guideline/contracts/events/user-registered-v1.json).
  - [ ] Hợp đồng sự kiện và POCO C# được nhập khẩu bằng `scripts/import-contracts.ps1` và xác minh toàn vẹn bằng `scripts/verify-contracts.ps1` (100% `[PASS]`).
  - [ ] Có đầy đủ 7 trường envelope: `eventId`, `eventType` (`user.registered.v1`), `version`, `occurredAt`, `correlationId`, `producer`, `data`.
- [ ] **Quy Chuẩn Topic & Khóa Phân Vùng (Partition Key)**:
  - [ ] Tên topic theo quy ước kebab-case: `falumni.users.v1.registered`.
  - [ ] Khóa thông điệp (`Message.Key`) BẮT BUỘC là **`userId`** để bảo đảm thứ tự xử lý của cùng một tài khoản.
- [ ] **Xử Lý Tại Consumer (Notification Background Worker)**:
  - [ ] Consumer Group đặt tên có ý nghĩa: `fpt-alumni-notification-worker`.
  - [ ] Đã triển khai cơ chế khử trùng lặp (Idempotent Consumer) dựa trên `eventId` để tránh gửi 2 email cho cùng một sự kiện.
  - [ ] Chỉ commit offset khi đã xử lý email thành công (hoặc đưa vào dead-letter log nếu lỗi không thể phục hồi).
  - [ ] Tuyệt đối che giấu mã xác minh nhạy cảm trong log (`[REDACTED]`).

---

## 🗄️ 4. Ranh Giới Cơ Sở Dữ Liệu (Database Isolation)

- [ ] **Không Dùng Chung Bảng hay Database**:
  - [ ] Mỗi microservice sở hữu 1 cơ sở dữ liệu riêng: `FptAlumni_User_Db`, `FptAlumni_Chat_Db`, `FptAlumni_Forum_Db`, `FptAlumni_AI_Db`, `FptAlumni_Admin_Db`.
  - [ ] Không có bất kỳ câu lệnh SQL cross-database nào (`SELECT ... FROM Db1.dbo.Users JOIN Db2.dbo.Orders`).
- [ ] **Tham Chiếu Bằng Khóa Định Danh (ID Reference Only)**:
  - [ ] Khi Chat Service hoặc Forum Service cần dữ liệu tác giả, chỉ lưu `authorId` (Guid/string), không tạo Foreign Key sang bảng Users.

---

## 🔐 5. Hợp Đồng Xác Thực JWT & Phân Quyền

- [ ] **Tính Nhất Quán Của Cấu Hình Token**:
  - [ ] Issuer (`Jwt:Issuer`) đồng bộ: `https://identity.falumni.local`.
  - [ ] Audience (`Jwt:Audience`) đồng bộ: `falumni-services`.
  - [ ] Chuỗi ký đối xứng (`Jwt:Key`) phải giống hệt nhau trên tất cả các dịch vụ có xác thực (cung cấp qua User Secrets hoặc biến môi trường runtime; TUYỆT ĐỐI KHÔNG commit vào Git hoặc tái sử dụng các khóa demo cũ).
- [ ] **Cấu Trúc Claim Chuẩn**:
  - [ ] `sub` / `NameIdentifier`: Mã định danh người dùng (`userId`).
  - [ ] `role`: Vai trò tài khoản (`USER` hoặc `ADMIN`).
- [ ] **Xử Lý 401 & 403 Nhất Quán**:
  - [ ] Client chưa truyền Bearer Token $\rightarrow$ Trả về `401 Unauthorized`.
  - [ ] Token hợp lệ nhưng không đủ quyền $\rightarrow$ Trả về `403 Forbidden`.

---

## 🐳 6. Đóng Gói Container & Mạng Nội Bộ Docker

- [ ] **Phân Định Địa Chỉ Mạng (Container vs Host)**:
  - [ ] Các kết nối giữa container với nhau sử dụng tên DNS nội bộ (`chat-service:5002`, `kafka:9092`, `user-grpc-server:50051`).
  - [ ] Các kết nối từ máy phát triển sử dụng cổng host đã ánh xạ (`localhost:5000`, `localhost:29092`, `localhost:14333`).
- [ ] **Khởi Động Đúng Thứ Tự (`depends_on` & Healthchecks)**:
  - [ ] `api-gateway` phụ thuộc vào `chat-service` và `user-service`.
  - [ ] `notification-worker` phụ thuộc vào điều kiện `kafka` đã `service_healthy`.
  - [ ] Toàn bộ container hoạt động trơn tru trên bridge network chung: `f-alumni-demo-net`.
- [ ] **Chạy Kịch Bản Kiểm Tra Khói Tích Hợp**:
  ```powershell
  .\scripts\smoke-test.ps1
  ```
  Kết quả trả về: Tất cả các luồng tích hợp đều đạt trạng thái `[DAT CHUAN]`.
