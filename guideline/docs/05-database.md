# 05. Quy chuẩn Cơ sở Dữ liệu & EF Core (Database & EF Core Standards)

> **Môn học**: PRN232 — Final Assignment  
> **Dự án**: FPT Alumni Connect  
> **Áp dụng cho**: 3 Backend Developers (BE1, BE2, BE3).  
> **Mục tiêu**: Chuẩn hóa việc thiết kế cơ sở dữ liệu, phân định quyền sở hữu DbContext riêng biệt, quản lý EF Core Migrations và đảm bảo các dịch vụ tích hợp độc lập mà không chia sẻ bảng dữ liệu.

---

## 📑 Mục lục
1. [Nguyên tắc Tối thượng: Database-per-Service](#1-nguyên-tắc-tối-thượng-database-per-service)
2. [Quy chuẩn Đặt tên Cơ sở Dữ liệu (Database Naming Convention)](#2-quy-chuẩn-đặt-tên-cơ-sở-dữ-liệu-database-naming-convention)
3. [Quyền Sở hữu DbContext & Entity (No Shared DbContext)](#3-quyền-sở-hữu-dbcontext--entity-no-shared-dbcontext)
4. [Tham chiếu Dữ liệu Liên Dịch vụ (Cross-Service Data References)](#4-tham-chiếu-dữ-liệu-liên-dịch-vụ-cross-service-data-references)
5. [Cấu hình Chuỗi Kết nối (Connection Strings Configuration)](#5-cấu-hình-chuỗi-kết-nối-connection-strings-configuration)
6. [Quy trình Quản lý EF Core Migrations (Local Dev vs Deployment)](#6-quy-trình-quản-lý-ef-core-migrations-local-dev-vs-deployment)
7. [Fluent API Configuration & Thực thi Mẫu trong Template](#7-fluent-api-configuration--thực-thi-mẫu-trong-template)
8. [Cấu hình Kết nối SQL Server Express & Xác thực](#8-cấu-hình-kết-nối-sql-server-express--xác-thực)
9. [Kiểm thử Tích hợp Cơ sở Dữ liệu Thật & Cách ly An toàn](#9-kiểm-thử-tích-hợp-cơ-sở-dữ-liệu-thật--cách-ly-an-toàn)
10. [So sánh EF Core InMemory vs SQL Server Thật](#10-so-sánh-ef-core-inmemory-vs-sql-server-thật)
11. [Hướng dẫn Khắc phục Sự cố & An toàn Dữ liệu](#11-hướng-dẫn-khắc-phục-sự-cố--an-toàn-dữ-liệu)

---

## 1. Nguyên tắc Tối thượng: Database-per-Service

Trong hệ thống Microservices của đồ án **FPT Alumni Connect**, cả nhóm sử dụng **chung một instance Microsoft SQL Server** (chạy cục bộ qua SQL Server Express hoặc Docker container), nhưng **mỗi service sở hữu một Database hoàn toàn độc lập**.

```mermaid
flowchart TD
    subgraph Services["Các Dịch vụ Độc lập"]
        UserSvc["User Service\n(f-alumni-user-service)"]
        ChatSvc["Chat Service\n(f-alumni-chat-service)"]
        ForumSvc["Forum Service\n(f-alumni-forum-service)"]
        AiSvc["AI Service\n(f-alumni-ai-service)"]
        AdminSvc["Admin Service\n(f-alumni-admin-service)"]
    end

    subgraph SqlServer["Một SQL Server Instance chung (localhost\\SQLEXPRESS hoặc Docker:1433)"]
        DB_User[("FAlumni_User_Db\n[Users, Profiles, Tokens]")]
        DB_Chat[("FAlumni_Chat_Db\n[Conversations, Messages]")]
        DB_Forum[("FAlumni_Forum_Db\n[Categories, Posts, Comments]")]
        DB_AI[("FAlumni_AI_Db\n[FaqVectors, ModerationRules]")]
        DB_Admin[("FAlumni_Admin_Db\n[AuditLogs, Announcements]")]
    end

    UserSvc ===>|Chỉ truy cập| DB_User
    ChatSvc ===>|Chỉ truy cập| DB_Chat
    ForumSvc ===>|Chỉ truy cập| DB_Forum
    AiSvc ===>|Chỉ truy cập| DB_AI
    AdminSvc ===>|Chỉ truy cập| DB_Admin

    UserSvc -.-x|CẤM TRUY CẬP| DB_Chat
    ChatSvc -.-x|CẤM TRUY CẬP| DB_User
    ForumSvc -.-x|CẤM TRUY CẬP| DB_User
    AdminSvc -.-x|CẤM TRUY CẬP| DB_User
```

> [!CAUTION]
> **3 NGUYÊN TẮC CẤM TRUY CẬP CHÉO**:
> 1. **Cấm viết câu lệnh SQL liên database**: Tuyệt đối không dùng cú pháp `FAlumni_User_Db.dbo.Users` từ trong mã nguồn của Chat Service hay Forum Service.
> 2. **Cấm tạo Foreign Key liên database**: Không tạo khóa ngoại từ bảng `Messages` (trong `FAlumni_Chat_Db`) sang bảng `Users` (trong `FAlumni_User_Db`).
> 3. **Cấm chia sẻ DbContext hoặc Entity Library**: Mỗi repository tự định nghĩa `DbContext` và Entity của riêng mình. Không tạo project thư viện dùng chung `Common.Entities` chứa entity của tất cả các service.

---

## 2. Quy chuẩn Đặt tên Cơ sở Dữ liệu (Database Naming Convention)

Tên cơ sở dữ liệu trên SQL Server bắt buộc sử dụng tiền tố `FAlumni_` kết hợp với tên nghiệp vụ viết theo chuẩn `PascalCase`:

| Tên Cơ sở Dữ liệu | Service Sở hữu | Repository chứa Code & Migrations | Các bảng cốt lõi |
| :--- | :--- | :--- | :--- |
| **`FAlumni_User_Db`** | **User Service** | `f-alumni-user-service` | `Users`, `UserProfiles`, `Roles`, `UserRoles`, `VerificationTokens`, `RefreshTokens` |
| **`FAlumni_Chat_Db`** | **Chat Service** | `f-alumni-chat-service` | `Conversations`, `ConversationParticipants`, `Messages`, `MessageAttachments`, `MessageReceipts` |
| **`FAlumni_Forum_Db`** | **Forum Service** | `f-alumni-forum-service` | `Categories`, `Posts`, `Comments`, `Reactions`, `Bookmarks`, `PostTags`, `Reports` |
| **`FAlumni_AI_Db`** | **AI Service** | `f-alumni-ai-service` | `FaqKnowledgeBase`, `UserSkillVectors`, `ModerationRules`, `RecommendationCaches` |
| **`FAlumni_Admin_Db`** | **Admin Service** | `f-alumni-admin-service` | `AuditLogs`, `SystemAnnouncements`, `ModerationQueues`, `SystemSettings` |
| **`FAlumni_Template_Db`** | **Service Template** | `templates/dotnet-service` | `SampleItems`, `__EFMigrationsHistory` |

---

## 3. Quyền Sở hữu DbContext & Entity (No Shared DbContext)

1. **Một DbContext duy nhất cho mỗi Service**:
   - `UserDbContext` chỉ quản lý các thực thể liên quan đến tài khoản và hồ sơ.
   - `ChatDbContext` chỉ quản lý tin nhắn và phòng chat.
   - `ForumDbContext` chỉ quản lý bài đăng, chuyên mục và bình luận.
2. **Quyền sở hữu Migration hoàn toàn cục bộ**:
   - Thư mục `Migrations/` nằm ngay trong project `*.Repositories` của từng service độc lập.
   - Lập trình viên phụ trách service nào thì tự tạo migration và cập nhật database của service đó.

---

## 4. Tham chiếu Dữ liệu Liên Dịch vụ (Cross-Service Data References)

Khi một service cần thông tin từ một service khác (ví dụ: Chat Service cần biết tên và avatar của người gửi tin nhắn, hoặc Forum Service cần biết tác giả bài viết):

### ❌ Cách làm SAI (Vi phạm nguyên tắc Microservices):
```csharp
// SAI: Dùng navigation property trỏ sang entity của service khác
public class Message
{
    public Guid Id { get; set; }
    public string Content { get; set; }
    public User Sender { get; set; } // LỖI: Không được phép navigation sang User entity của User Service!
}
```

### ✅ Cách làm ĐÚNG (Chuẩn Microservices):
```csharp
// ĐÚNG: Chỉ lưu trữ khóa ngoại định danh (Scalar ID)
public class Message
{
    public Guid Id { get; set; }
    public string Content { get; set; }
    public Guid SenderId { get; set; } // Scalar ID thông thường
    public DateTime CreatedAtUtc { get; set; }
}
```

### Phương thức lấy thông tin bổ trợ:
1. **gRPC Đồng bộ (Synchronous)**: Chat Service gọi `UserProfileGrpcService.GetUserProfile(senderId)` sang User Service khi cần hiển thị tức thời thông tin người gửi.
2. **Kafka Bất đồng bộ (Data Replication)**: Khi User Service cập nhật Avatar hoặc Tên hiển thị, nó bắn sự kiện `UserProfileUpdatedEvent` ra Kafka topic. Các service khác lắng nghe và cập nhật bản sao nếu có lưu cache.

---

## 5. Cấu hình Chuỗi Kết nối (Connection Strings Configuration)

Mỗi service định nghĩa chuỗi kết nối trong file `appsettings.json` tại tầng API (`src/*.Api/appsettings.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=FAlumni_Template_Db;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### Đối với môi trường Docker:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver,1433;Database=FAlumni_Template_Db;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=True;"
  }
}
```

---

## 6. Quy trình Quản lý EF Core Migrations (Local Dev vs Deployment)

### 6.1 Trong quá trình phát triển cục bộ (Local Development)

Khi lập trình viên thay đổi Entity hoặc thêm bảng mới:

1. **Tạo Migration mới**:
   ```bash
   dotnet ef migrations add <Tên_Migration> --project src/Template.Repositories --startup-project src/Template.Api
   ```
   Ví dụ:
   ```bash
   dotnet ef migrations add AddUserBioAndSkills --project src/FAlumni.User.Repositories --startup-project src/FAlumni.User.Api
   ```

2. **Cập nhật Database cục bộ**:
   ```bash
   dotnet ef database update --project src/Template.Repositories --startup-project src/Template.Api
   ```

3. **Kiểm tra danh sách Migration đã áp dụng**:
   ```bash
   dotnet ef migrations list --project src/Template.Repositories --startup-project src/Template.Api
   ```

### 6.2 Trong quá trình triển khai & Nộp bài (Deployment / Production)

> [!IMPORTANT]
> Khi nộp bài hoặc triển khai lên môi trường đánh giá:
> - Không chạy lệnh migrate tự động lúc ứng dụng khởi động nếu chưa có sự kiểm soát vì dễ gây xung đột khi chạy nhiều instance.
> - Sử dụng lệnh sinh file SQL Script có tính năng lũy đẳng (**Idempotent SQL Script**):
>   ```bash
>   dotnet ef migrations script --idempotent --output migrations.sql --project src/Template.Repositories --startup-project src/Template.Api
>   ```
> - Script này có thể chạy an toàn trên bất kỳ database nào: Bảng nào đã migrate thì bỏ qua, bảng nào chưa có thì tạo mới.

---

## 7. Fluent API Configuration & Thực thi Mẫu trong Template

Để tránh làm bẩn Entity class bằng các DataAnnotations thuộc về database, toàn bộ cấu hình bảng được viết trong thư mục `Configurations/` kế thừa `IEntityTypeConfiguration<T>`:

```csharp
public class SampleItemConfiguration : IEntityTypeConfiguration<SampleItem>
{
    public void Configure(EntityTypeBuilder<SampleItem> builder)
    {
        builder.ToTable("SampleItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500)
            .HasDefaultValue(string.Empty);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();
    }
}
```

Và được đăng ký tự động trong `DbContext`:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfiguration(new SampleItemConfiguration());
}
```

---

## 8. Cấu hình Kết nối SQL Server Express & Xác thực

### 8.1 Chuỗi kết nối cục bộ (Windows Authentication / Trusted Connection)
Đối với máy phát triển cá nhân sử dụng SQL Server Express, chuỗi kết nối sử dụng xác thực Windows tích hợp (Integrated Security), không cần nhập User ID và Password:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=FAlumni_<ServiceName>_Db;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

> [!NOTE]
> - `Server=localhost\SQLEXPRESS` hoặc `Server=.\SQLEXPRESS`: Trỏ tới Named Instance mặc định của SQL Server Express trên Windows.
> - `Trusted_Connection=True` (hoặc `Integrated Security=True`): Đăng nhập bằng tài khoản Windows hiện tại.
> - `TrustServerCertificate=True`: Tránh lỗi bắt buộc kiểm tra TLS Certificate tự ký trên môi trường phát triển cục bộ.

### 8.2 Chuỗi kết nối Docker / SQL Server Authentication
Khi triển khai qua Docker hoặc môi trường kiểm thử tập trung:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver,1433;Database=FAlumni_<ServiceName>_Db;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=True;"
  }
}
```

### 8.3 Ghi đè cấu hình qua Biến Môi trường (Environment Variables)
ASP.NET Core hỗ trợ ghi đè cấu hình phân cấp thông qua biến môi trường sử dụng tiền tố hai dấu gạch dưới `__`:
```powershell
# PowerShell:
$env:ConnectionStrings__DefaultConnection = "Server=.\SQLEXPRESS;Database=FAlumni_<ServiceName>_Db;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet run --project src/Template.Api
```

> [!CAUTION]
> **Tuyệt đối không commit mật khẩu SQL Server lên Git**: Mật khẩu máy chủ hoặc tài khoản `sa` phải luôn được nạp từ biến môi trường, tệp `.env` (được đưa vào `.gitignore`), hoặc User Secrets.

---

## 9. Kiểm thử Tích hợp Cơ sở Dữ liệu Thật & Cách ly An toàn

### 9.1 Cơ sở dữ liệu kiểm thử dùng một lần (`FptAlumni_Guideline_Test_Db`)
Để bảo vệ an toàn cho các cơ sở dữ liệu nghiệp vụ (`FAlumni_User_Db`, `FAlumni_Chat_Db`, v.v.), toàn bộ các ca kiểm thử tích hợp thực tế với SQL Server được **cách ly tuyệt đối** vào một database dùng một lần:

`FptAlumni_Guideline_Test_Db`

### 9.2 Cơ chế Chốt chặn An toàn (Isolation Guardrail)
Mã kiểm thử tích hợp trong [`SqlServerTestEnvironment`](file:///c:/Use/University/PRN232/ASM/guideline/templates/dotnet-service/tests/Template.Tests/Infrastructure/SqlServerTestEnvironment.cs) thực thi kiểm tra nghiêm ngặt trước khi tạo bất kỳ kết nối nào:
- Nếu chuỗi kết nối trỏ tới bất kỳ cơ sở dữ liệu nào khác ngoài `FptAlumni_Guideline_Test_Db`, hệ thống sẽ ném ngoại lệ `InvalidOperationException` và lập tức dừng kiểm thử.
- Tuyệt đối không tự động chạy lệnh `DROP DATABASE` hoặc `EnsureDeleted()`.
- Các ca kiểm thử tự tạo ID duy nhất cho mỗi bản ghi (`Guid.NewGuid()`), theo dõi danh sách ID này và chỉ dọn dẹp đúng những bản ghi do chính nó tạo ra trong phương thức `DisposeAsync`.

### 9.3 Quy trình Xác minh EF Core Migrations
1. **Kiểm tra tính hợp lệ của Migration**: Xác nhận migration `20261024091753_InitialCreate` khớp với cấu hình Fluent API trong `SampleItemConfiguration`.
2. **Thực thi Migration lên SQL Server thật**:
   ```bash
   dotnet ef database update --project src/Template.Repositories --startup-project src/Template.Api
   ```
3. **Xác minh bảng và lược đồ**:
   - Bảng `__EFMigrationsHistory` được khởi tạo và ghi nhận bản ghi `20261024091753_InitialCreate`.
   - Bảng `SampleItems` được tạo với đúng kiểu dữ liệu:
     - `Id`: `uniqueidentifier` (Primary Key).
     - `Name`: `nvarchar(100)` NOT NULL.
     - `Description`: `nvarchar(500)` NOT NULL, Default `(N'')`.
     - `IsActive`: `bit` NOT NULL, Default `1`.
     - `CreatedAtUtc`: `datetime2` NOT NULL.
4. **Tính lũy đẳng khi chạy lại**: Khi chạy lại lệnh `dotnet ef database update`, EF Core nhận diện database đã cập nhật và không sinh ra bất kỳ thay đổi dư thừa nào.

---

## 10. So sánh EF Core InMemory vs SQL Server Thật

| Tiêu chí | EF Core InMemory Provider | Microsoft SQL Server (Relational) |
| :--- | :--- | :--- |
| **Mục đích sử dụng** | Unit test nhanh trong bộ nhớ, không cần cài đặt SQL Server. | Kiểm thử tích hợp thực tế (Integration Test) & Môi trường Runtime. |
| **Kiểm tra ràng buộc chuỗi (`HasMaxLength`)** | Bỏ qua hoàn toàn (cho phép chuỗi vượt quá 100 ký tự mà không báo lỗi). | Ép buộc nghiêm ngặt: Ném ngoại lệ `DbUpdateException` khi vượt quá độ dài khai báo. |
| **Ràng buộc khóa chính & duy nhất** | Hỗ trợ cơ bản trong bộ nhớ. | Ép buộc ở mức engine database với chỉ mục (Clustered/Non-Clustered Index). |
| **Thực thi Migrations** | **Không hỗ trợ**: Không tạo bảng `__EFMigrationsHistory` hay schema DDL. | **Hỗ trợ đầy đủ**: Chạy DDL migration và ghi vết lịch sử migration. |
| **Biên dịch câu lệnh LINQ** | Đánh giá trực tiếp trên đối tượng C# / LINQ to Objects. | Biên dịch thành T-SQL (`SELECT ... OFFSET ... FETCH NEXT`, `LIKE '%...'`). |
| **Giao dịch (Transactions & ACID)** | Không hỗ trợ rollback transaction thực sự. | Hỗ trợ đầy đủ ACID transaction, commit và rollback. |

---

## 11. Hướng dẫn Khắc phục Sự cố & An toàn Dữ liệu

### 11.1 Dịch vụ SQL Server chưa khởi động (Connection Refused)
Nếu gặp lỗi `A network-related or instance-specific error occurred while establishing a connection to SQL Server`:
1. Kiểm tra trạng thái dịch vụ SQL Server trong PowerShell:
   ```powershell
   Get-Service -Name *sql*
   ```
2. Nếu `MSSQL$SQLEXPRESS` đang ở trạng thái `Stopped`, khởi động lại:
   ```powershell
   Start-Service MSSQL$SQLEXPRESS
   ```

### 11.2 Lỗi Xác thực Chứng chỉ SSL/TLS (`The certificate chain was issued by an authority that is not trusted`)
- **Nguyên nhân**: Môi trường phát triển cục bộ dùng chứng chỉ tự ký chưa được tin cậy trong chứng thư gốc của Windows.
- **Cách khắc phục**: Luôn thêm tham số `TrustServerCertificate=True;` vào chuỗi kết nối phát triển cục bộ.

### 11.3 Lỗi Phân quyền Đăng nhập (`Login failed for user`)
- Đối với `Trusted_Connection=True`: Đảm bảo tài khoản Windows hiện tại đã được cấp quyền `sysadmin` hoặc `dbcreator` trên SQL Server.
- Đối với tài khoản `sa`: Kiểm tra xem SQL Server đã bật chế độ **SQL Server and Windows Authentication mode** (Mixed Mode) trong SQL Server Management Studio (SSMS) hay chưa.

### 11.4 Nguyên tắc Tạo Database cho từng Dịch vụ Mới
Mỗi khi lập trình viên bắt đầu một service mới (`User`, `Chat`, `Forum`, `AI`, `Admin`):
1. Đặt tên database theo đúng chuẩn `FAlumni_<ServiceName>_Db`.
2. Tạo Migration đầu tiên: `dotnet ef migrations add InitialCreate --project src/<ServiceName>.Repositories --startup-project src/<ServiceName>.Api`.
3. Cập nhật database: `dotnet ef database update --project src/<ServiceName>.Repositories --startup-project src/<ServiceName>.Api`.
4. Tuyệt đối không dùng chung database giữa các service.

