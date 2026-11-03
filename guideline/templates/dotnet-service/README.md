# 📦 Layered ASP.NET Core .NET 10 Service Template

> Mẫu khung dự án backend chuẩn dành cho các Microservices thuộc dự án **FPT Alumni Connect** (PRN232).

---

## 🏛️ Cấu trúc Kiến trúc (Layered Architecture)

Template áp dụng mô hình 3 tầng phân lớp rõ ràng:

```text
Template.Api (REST API, Controllers, Middleware, Health, OpenAPI)
    │
    ▼ (Reference duy nhất)
Template.Services (Business Logic, Validation, DTOs, Exceptions)
    │
    ▼ (Reference duy nhất)
Template.Repositories (Data Access, EF Core SqlServer, Migrations, Entities)
```

> ⚠️ **Quy tắc bất di bất dịch**: `Template.Api` không được reference hay inject bất kỳ Repository nào trực tiếp. Mọi truy vấn và nghiệp vụ phải đi qua tầng `Template.Services`.

---

## 🗄️ Cơ sở Dữ liệu & EF Core Migrations

- **Provider**: Microsoft SQL Server (`Microsoft.EntityFrameworkCore.SqlServer`).
- **Tên Database mặc định**: `FAlumni_Template_Db` (trên instance `localhost\SQLEXPRESS` hoặc Docker SQL Server).
- **Cấu hình chuỗi kết nối**: Trong `src/Template.Api/appsettings.json`:
  ```json
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=FAlumni_Template_Db;Trusted_Connection=True;TrustServerCertificate=True;"
  }
  ```

### Các lệnh EF Core thông dụng:

```bash
# 1. Liệt kê các migration hiện có
dotnet ef migrations list --project src/Template.Repositories --startup-project src/Template.Api

# 2. Tạo migration mới khi có thay đổi Entity/Fluent API
dotnet ef migrations add <MigrationName> --project src/Template.Repositories --startup-project src/Template.Api

# 3. Áp dụng migration vào SQL Server
dotnet ef database update --project src/Template.Repositories --startup-project src/Template.Api

# 4. Sinh file script SQL lũy đẳng để nộp bài hoặc triển khai
dotnet ef migrations script --idempotent --output migrations.sql --project src/Template.Repositories --startup-project src/Template.Api
```

---

## 🚀 Hướng dẫn Sử dụng Nhanh (Quickstart)

### 1. Khôi phục packages & Biên dịch dự án
```bash
# Khôi phục dependencies
dotnet restore

# Biên dịch toàn bộ Solution
dotnet build
```

### 2. Chạy Kiểm thử (xUnit Unit & Integration Tests)
```bash
# 2.1 Chạy toàn bộ kiểm thử đơn vị & API integration với EF Core InMemory (36 tests passed)
dotnet test

# 2.2 Chạy thêm kiểm thử tích hợp thực tế với SQL Server (FptAlumni_Guideline_Test_Db) (43 tests passed)
$env:RUN_SQLSERVER_TESTS = "true"
dotnet test

# 2.3 Chỉ chạy riêng các bài kiểm thử SQL Server Integration
dotnet test --filter "Category=SqlServerIntegration"
```

### 3. Khởi chạy Web API cục bộ
Trước khi khởi chạy, cần cung cấp khóa bí mật ký JWT (`Jwt:Key`). Nhằm tuân thủ bảo mật, khóa này **không được lưu cứng** trong `appsettings.json` và tuyệt đối **không tái sử dụng bất kỳ chuỗi khóa mẫu nào trong lịch sử Git** (các chuỗi đã commit phải coi là đã bị lộ).

Hãy tự sinh một khóa bí mật ngẫu nhiên chuẩn mã hóa (256 bits / 32 bytes) thông qua một trong hai cách sau:
- **Cách 1: Sinh ngẫu nhiên và gán biến môi trường (PowerShell)**:
  ```powershell
  $bytes = New-Object byte[] 32; [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes); $randomKey = [Convert]::ToBase64String($bytes)
  $env:Jwt__Key = $randomKey
  dotnet run --project src/Template.Api/Template.Api.csproj
  ```
- **Cách 2: Sử dụng .NET User Secrets (khuyên dùng khi phát triển máy cá nhân)**:
  ```powershell
  dotnet user-secrets init --project src/Template.Api
  $bytes = New-Object byte[] 32; [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes); $randomKey = [Convert]::ToBase64String($bytes)
  dotnet user-secrets set "Jwt:Key" $randomKey --project src/Template.Api
  dotnet run --project src/Template.Api/Template.Api.csproj
  ```

> 💡 **Lưu ý**: Khóa JWT phải có độ dài tối thiểu **32 bytes / 256 bits** (chuẩn HMAC-SHA256; chuỗi Base64 32 bytes có độ dài 44 ký tự đáp ứng hoàn hảo tiêu chuẩn này). Nếu thiếu hoặc quá ngắn, ứng dụng sẽ dừng ngay ở bước khởi động với thông báo lỗi rõ ràng. Đối với kiểm thử tự động (`dotnet test`), test fixture `CustomWebApplicationFactory` tự động nạp khóa kiểm thử giả lập độc lập mà không cần cấu hình thủ công.

Mặc định ứng dụng sẽ lắng nghe trên cổng HTTP: `http://localhost:5000`.

---

## 📡 Danh sách Endpoints Mẫu (Sample Endpoints)

| Phương thức | Đường dẫn URL | Mô tả | Mã trạng thái HTTP |
| :--- | :--- | :--- | :--- |
| `GET` | `/health` | Kiểm tra tình trạng hoạt động (Health Check) | `200 OK` (`"Healthy"`) |
| `GET` | `/api/v1/sample-items` | Lấy danh sách toàn bộ các items (Public) | `200 OK` (JSON List) |
| `GET` | `/api/v1/sample-items/paged` | Lấy danh sách có phân trang (`pageNumber`, `pageSize`) và tìm kiếm | `200 OK` (`PagedResponse<T>`) |
| `GET` | `/api/v1/sample-items/{id}` | Lấy chi tiết 1 item theo GUID (Public) | `200 OK` hoặc `404 Not Found` |
| `POST` | `/api/v1/sample-items` | Tạo mới 1 sample item (USER/ADMIN role) | `201 Created` (Kèm Location header) |
| `PUT` | `/api/v1/sample-items/{id}` | Cập nhật toàn bộ thông tin 1 item theo GUID (USER/ADMIN role) | `200 OK` hoặc `404 Not Found` |
| `DELETE` | `/api/v1/sample-items/{id}` | Xóa 1 sample item theo GUID (ADMIN role) | `204 NoContent` hoặc `404 Not Found` |

---

## 🛠️ Hướng dẫn Sao chép cho Service Mới

Khi bắt đầu một service mới trong danh sách (`f-alumni-user-service`, `f-alumni-chat-service`, v.v.):

1. Copy toàn bộ nội dung thư mục này vào thư mục repository của service.
2. Đổi tên solution `Template.sln` thành `FAlumni.<ServiceName>.sln`.
3. Đổi tên các thư mục `src/Template.*` thành `src/FAlumni.<ServiceName>.*`.
4. Tìm và thay thế chuỗi `Template` thành `FAlumni.<ServiceName>` trong tất cả các file `.csproj` và `.cs`.
5. Cập nhật tên Database trong `appsettings.json` thành `FAlumni_<ServiceName>_Db` (xem [docs/05-database.md](file:///c:/Use/University/PRN232/ASM/guideline/docs/05-database.md)).
6. Chạy `dotnet test` để đảm bảo hệ thống biên dịch và chạy kiểm thử thành công trước khi bắt đầu viết code nghiệp vụ.
