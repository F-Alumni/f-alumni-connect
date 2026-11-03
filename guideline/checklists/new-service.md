# Bảng Kiểm Tra Triển Khai Microservice Mới (New Service Checklist)

> **Mã tài liệu**: `CHECKLIST-01-NEW-SERVICE`  
> **Dự án**: FPT Alumni Connect (PRN232 Final Assignment)  
> **Mục đích**: Hướng dẫn từng bước và kiểm tra chất lượng khi một thành viên Backend (BE1, BE2, BE3) bắt đầu xây dựng một microservice mới từ mẫu chuẩn.

---

## 📋 Giai Đoạn 1: Chuẩn Bị & Phân Nhánh Git

- [ ] **Xác định quyền sở hữu dịch vụ**:
  - `BE1`: Phụ trách `f-alumni-user-service` và `f-alumni-chat-service`.
  - `BE2`: Phụ trách `f-alumni-forum-service` và `f-alumni-ai-service`.
  - `BE3`: Phụ trách `f-alumni-admin-service` và `f-alumni-infrastructure` (YARP Gateway & Notification Background Worker).
  - `FE`: Phụ trách `f-alumni-web` (Web Portal).
  - `Mobile`: Phụ trách `f-alumni-mobile` (Flutter App).
- [ ] **Kiểm tra User Stories & Ranh giới**:
  - Đã đọc kỹ danh sách User Stories được giao trong [`docs/03-service-boundaries.md`](file:///c:/Use/University/PRN232/ASM/guideline/docs/03-service-boundaries.md).
- [ ] **Tạo nhánh tính năng chuẩn**:
  - Cập nhật nhánh `develop` mới nhất: `git checkout develop && git pull origin develop`.
  - Tạo nhánh mới: `git checkout -b feat/<ma-ticket>-<ten-tinh-nang>`.

---

## 🚀 Giai Đoạn 2: Khởi Tạo Dịch Vụ Bằng Script Bootstrap

- [ ] **Thực thi script khởi tạo tự động**:
  ```powershell
  .\scripts\bootstrap-service.ps1 -ServiceName "<ServiceName>" -TargetDirectory "..\fpt-alumni-<ten-dich-vu>"
  ```
- [ ] **Kiểm tra kết quả khởi tạo**:
  - [ ] Solution có đầy đủ 4 dự án: `.Api`, `.Services`, `.Repositories`, `.Tests`.
  - [ ] Lệnh `dotnet build` và `dotnet test` chạy thành công 100% không có lỗi.
  - [ ] Tên namespace đã được đổi đồng bộ từ `Template` sang `<ServiceName>`.
- [ ] **Nhập khẩu hợp đồng giao tiếp cần thiết (Contract Distribution)**:
  - Nếu service cần gọi gRPC sang User Service (ví dụ Chat Service):
    ```powershell
    ..\f-alumni-guideline\scripts\import-contracts.ps1 -SourceGuidelinePath "..\f-alumni-guideline" -TargetDirectory "." -ContractId "grpc/user-profile/v1"
    ```
  - Nếu service cần phát sinh hoặc tiêu thụ Kafka Events (ví dụ Notification Worker, User, Forum):
    ```powershell
    ..\f-alumni-guideline\scripts\import-contracts.ps1 -SourceGuidelinePath "..\f-alumni-guideline" -TargetDirectory "." -ContractId "events/user-registered/v1"
    ```
  - [ ] File `contracts/contracts.manifest.json` và thư mục `contracts/` đã được tạo trong repository của service.
  - [ ] **Đường dẫn trong file `.csproj`**: Tham chiếu hợp đồng bằng đường dẫn tương đối từ project (`..\..\contracts\...`), TUYỆT ĐỐI KHÔNG dùng đường dẫn tuyệt đối hoặc tham chiếu sang repository khác.
  - [ ] Chạy lệnh xác minh hợp đồng đạt chuẩn: `.\scripts\verify-contracts.ps1`.
  - [ ] Chi tiết xem tại [`docs/15-contract-sharing.md`](file:///c:/Use/University/PRN232/ASM/guideline/docs/15-contract-sharing.md).

---

## 🗄️ Giai Đoạn 3: Thiết Kế Cơ Sở Dữ Liệu & EF Core

- [ ] **Đặt tên cơ sở dữ liệu riêng biệt**:
  - Tên database tuân thủ quy ước: `FptAlumni_<ServiceName>_Db` (Ví dụ: `FptAlumni_Forum_Db`).
  - Cập nhật chuỗi kết nối trong `appsettings.Development.json` và `appsettings.json`.
- [ ] **Tạo Domain Entities**:
  - Lưu tại thư mục `src/<ServiceName>.Repositories/Entities/`.
  - Có đầy đủ các trường chuẩn: `Id` (Guid hoặc int/long), `CreatedAtUtc`, `UpdatedAtUtc`.
- [ ] **Cấu hình Entity Mapping (Fluent API)**:
  - Đặt trong `src/<ServiceName>.Repositories/Configurations/`.
  - Quy định rõ ràng kiểu dữ liệu, độ dài tối đa (`HasMaxLength`), ràng buộc bắt buộc (`IsRequired`), và Index cần thiết.
- [ ] **Tuyệt đối tuân thủ ranh giới Database**:
  - [ ] **KHÔNG** tham chiếu khóa ngoại (Foreign Key) sang bảng của microservice khác.
  - [ ] Các mối quan hệ liên microservice chỉ lưu mã định danh (`UserId`, `AlumniId`, `RoomId`).
- [ ] **Tạo và thực thi Migration ban đầu**:
  ```powershell
  dotnet ef migrations add InitialCreate --project src/<ServiceName>.Repositories --startup-project src/<ServiceName>.Api
  dotnet ef database update --project src/<ServiceName>.Repositories --startup-project src/<ServiceName>.Api
  ```

---

## 🏛️ Giai Đoạn 4: Hiện Thực Kiến Trúc Phân Tầng

- [ ] **Tầng Repository (`<ServiceName>.Repositories`)**:
  - [ ] Định nghĩa interface trong thư mục `Interfaces/` (ví dụ: `IPostRepository`).
  - [ ] Hiện thực class trong thư mục `Implementations/` kế thừa interface.
  - [ ] Đăng ký DI trong `RepositoryServiceCollectionExtensions.cs`.
- [ ] **Tầng Dịch Vụ Nghiệp Vụ (`<ServiceName>.Services`)**:
  - [ ] Tạo DTO Request và Response trong thư mục `DTOs/`. Tuyệt đối không trả Entity trực tiếp ra ngoài API.
  - [ ] Định nghĩa interface trong thư mục `Interfaces/` (ví dụ: `IPostService`).
  - [ ] Hiện thực logic nghiệp vụ trong `Implementations/`.
  - [ ] Đăng ký DI trong `ServiceCollectionExtensions.cs`.
- [ ] **Tầng Giao Diện Web API (`<ServiceName>.Api`)**:
  - [ ] Tạo Controller kế thừa `ControllerBase` với route chuẩn `api/v1/<resource>`.
  - [ ] **Ràng buộc phụ thuộc**: Controller CHỈ inject Service Interface, TUYỆT ĐỐI KHÔNG inject Repository Interface hoặc DbContext.
  - [ ] Trả về mã HTTP status code chuẩn: `200 OK`, `201 Created` kèm Header `Location`, `204 No Content`, `400 Bad Request ProblemDetails`, `404 NotFound ProblemDetails`.
  - [ ] Kiểm tra hợp lệ dữ liệu đầu vào (Data Annotations DTO validation).
  - [ ] Thêm OpenAPI metadata (`[ProducesResponseType]`, XML documentation).

---

## 🔐 Giai Đoạn 5: Bảo Mật JWT & Phân Quyền

- [ ] **Áp dụng chính sách bảo vệ Endpoint**:
  - [ ] Các endpoint công khai (Public) được gắn thẻ `[AllowAnonymous]`.
  - [ ] Các endpoint yêu cầu đăng nhập được gắn `[Authorize(Policy = "RequireUserRole")]`.
  - [ ] Các endpoint quản trị nội bộ gắn `[Authorize(Policy = "RequireAdminRole")]`.
- [ ] **Trích xuất thông tin người dùng từ Token**:
  - Lấy `userId` từ claim `ClaimTypes.NameIdentifier` hoặc `JwtRegisteredClaimNames.Sub`.
  - Không tin tưởng mù quáng `userId` do client gửi trong request body nếu thao tác đó thuộc về chính chủ tài khoản.

---

## 🧪 Giai Đoạn 6: Kiểm Thử Đơn Vị & Tích Hợp

- [ ] **Kiểm thử logic nghiệp vụ (Service Tests)**:
  - Viết ca kiểm thử xUnit cho các hàm xử lý trong `*Service.cs`.
- [ ] **Kiểm thử tích hợp API (Controller Tests)**:
  - Sử dụng `CustomWebApplicationFactory` với cấu hình in-memory.
  - Kiểm tra trường hợp thành công (200/201).
  - Kiểm tra trường hợp lỗi đầu vào (400 ProblemDetails).
  - Kiểm tra từ chối truy cập khi thiếu token (401 Unauthorized) hoặc sai role (403 Forbidden).
- [ ] **Chạy toàn bộ kiểm thử trước khi commit**:
  ```powershell
  dotnet test
  ```

---

## 📦 Giai Đoạn 7: Mở Pull Request & Bàn Giao

- [ ] Chạy kiểm tra tính toàn vẹn hợp đồng cục bộ: `.\scripts\verify-contracts.ps1` (đạt 100% `[PASS]`).
- [ ] Chạy kịch bản xác minh hệ thống: `.\scripts\verify-all.ps1`.
- [ ] Soạn thảo Pull Request tuân thủ mẫu tại [`docs/14-team-workflow.md`](file:///c:/Use/University/PRN232/ASM/guideline/docs/14-team-workflow.md).
- [ ] Gửi link PR cho người review chéo được phân công để phê duyệt.
