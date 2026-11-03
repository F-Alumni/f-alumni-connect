# 🗄️ Sơ Đồ ERD Cơ Sở Dữ Liệu — User Service (`FAlumni_User_Db`)

> **Service**: `f-alumni-user-service`  
> **Người phụ trách**: Trương Chí Trung (`chstrg2208` — Backend 1)  
> **Cơ sở dữ liệu**: `FAlumni_User_Db` (SQL Server + EF Core 10)  
> **Nguyên tắc kiến trúc**: Database-per-Service — Độc lập 100%, không chia sẻ bảng hay khóa ngoại (Zero Cross-Database Foreign Keys) với các Microservices khác.

---

## 1. Hình Ảnh Sơ Đồ ERD Trực Quan (SVG & Draw.io)

- **File Vector SVG**: [`docs/architecture/f-alumni-user-db-erd.svg`](architecture/f-alumni-user-db-erd.svg)
- **File Draw.io (Mở trên diagrams.net)**: [`docs/architecture/f-alumni-user-db-erd.drawio`](architecture/f-alumni-user-db-erd.drawio)

![ERD FAlumni_User_Db](architecture/f-alumni-user-db-erd.svg)

---

## 2. Sơ Đồ ERD Chuẩn Mermaid (`FAlumni_User_Db`)

```mermaid
erDiagram
    Users ||--|| UserProfiles : "1:1 (FK UserId, Cascade)"
    Users ||--o{ RefreshTokens : "1:N (FK UserId, Cascade)"
    Users ||--o{ VerificationTokens : "1:N (FK UserId, Cascade)"
    Users ||--o{ BlockedUsers : "1:N (BlockerUserId / BlockedTargetUserId)"

    Users {
        uniqueidentifier Id PK "Guid.NewGuid()"
        nvarchar_150 Email UK "NOT NULL, Unique Index"
        nvarchar_500 PasswordHash "NOT NULL (BCrypt/PBKDF2)"
        nvarchar_150 FullName "NOT NULL"
        nvarchar_30 Role "NOT NULL, Default 'USER' (USER | ADMIN)"
        nvarchar_30 AccountType "NOT NULL, Default 'Alumni' (Student | Alumni | Admin)"
        nvarchar_40 Status "NOT NULL, Default 'PendingVerification' (Active | Suspended)"
        bit EmailVerified "NOT NULL"
        nvarchar_500 SuspensionReason "NULL (AD-03)"
        datetime2 CreatedAtUtc "NOT NULL"
        datetime2 UpdatedAtUtc "NOT NULL"
    }

    UserProfiles {
        uniqueidentifier Id PK "Guid.NewGuid()"
        uniqueidentifier UserId FK,UK "NOT NULL, Unique (1-1 with Users)"
        nvarchar_150 DisplayName "NOT NULL"
        nvarchar_500 AvatarUrl "NOT NULL, Default avatar URL"
        nvarchar_30 StudentCode "NOT NULL (e.g., SE170001)"
        nvarchar_50 Campus "NOT NULL, Default 'HCM' (HCM | HN | DN | CT | QN)"
        int GraduationYear "NULL (US-04, US-06)"
        nvarchar_100 Major "NOT NULL (US-04, US-06)"
        nvarchar_150 CurrentCompany "NOT NULL"
        nvarchar_150 JobTitle "NOT NULL"
        nvarchar_1000 Bio "NOT NULL"
        nvarchar_500 Skills "NOT NULL"
        nvarchar_300 LinkedInUrl "NOT NULL"
        nvarchar_300 GitHubUrl "NOT NULL"
        nvarchar_30 PhoneNumber "NOT NULL"
        nvarchar_500 GoogleCalendarUrl "NOT NULL (US-11 Booking Link)"
        bit ShowEmailPublicly "NOT NULL, Default 1 (US-09 Privacy)"
        bit ShowPhonePublicly "NOT NULL, Default 0 (US-09 Privacy)"
        bit ShowCompanyPublicly "NOT NULL, Default 1 (US-09 Privacy)"
        bit ShowCalendarPublicly "NOT NULL, Default 1 (US-09 Privacy)"
        datetime2 UpdatedAtUtc "NOT NULL"
    }

    RefreshTokens {
        uniqueidentifier Id PK "Guid.NewGuid()"
        uniqueidentifier UserId FK "NOT NULL -> Users.Id"
        nvarchar_250 Token UK "NOT NULL, Unique Index"
        datetime2 ExpiresAtUtc "NOT NULL"
        bit IsRevoked "NOT NULL (US-08 Logout / AD-03 Suspend)"
        datetime2 CreatedAtUtc "NOT NULL"
    }

    VerificationTokens {
        uniqueidentifier Id PK "Guid.NewGuid()"
        uniqueidentifier UserId FK "NOT NULL -> Users.Id"
        nvarchar_20 OtpCode "NOT NULL (6-digit OTP)"
        nvarchar_40 Purpose "NOT NULL, Default 'EmailVerification' (EmailVerification | PasswordReset)"
        datetime2 ExpiresAtUtc "NOT NULL"
        bit IsUsed "NOT NULL"
        datetime2 CreatedAtUtc "NOT NULL"
    }

    BlockedUsers {
        uniqueidentifier Id PK "Guid.NewGuid()"
        uniqueidentifier BlockerUserId UK "NOT NULL (Composite Unique with BlockedTargetUserId)"
        uniqueidentifier BlockedTargetUserId UK "NOT NULL (US-10)"
        nvarchar_300 Reason "NOT NULL"
        datetime2 CreatedAtUtc "NOT NULL"
    }
```

---

## 3. Đặc Tả Chi Tiết Các Bảng & Ràng Buộc (Data Dictionary)

### 3.1. Bảng `Users` (Định danh & Bảo mật cốt lõi)
- **Mục đích**: Lưu thông tin tài khoản, phân quyền (`USER`, `ADMIN`), loại tài khoản (`Student`, `Alumni`, `Admin`) và trạng thái kích hoạt/khóa (`PendingVerification`, `Active`, `Suspended`).
- **Khóa & Chỉ mục**:
  - `PK_Users`: `Id` (`uniqueidentifier`).
  - `IX_Users_Email` (**Unique Index**): Đảm bảo mỗi email FPT/cá nhân chỉ đăng ký 1 tài khoản duy nhất.

### 3.2. Bảng `UserProfiles` (Hồ sơ Cựu sinh viên & Quyền riêng tư)
- **Mục đích**: Phục vụ các User Stories từ **US-04 đến US-11** (Danh bạ cựu sinh viên, lọc theo `Campus`, `Major`, `GraduationYear`, cài đặt quyền riêng tư **US-09**, và đặt lịch hẹn Google Calendar **US-11**) cũng như trả dữ liệu cho **gRPC Server** (`UserProfileGrpcService`).
- **Khóa & Quan hệ**:
  - `PK_UserProfiles`: `Id` (`uniqueidentifier`).
  - `IX_UserProfiles_UserId` (**Unique Index** + **Foreign Key**): Quan hệ `1 : 1` chặt chẽ với bảng `Users` (`OnDelete: Cascade`).

### 3.3. Bảng `RefreshTokens` (Quản lý phiên đăng nhập JWT)
- **Mục đích**: Cấp mới Access Token và hỗ trợ thu hồi phiên tức thì khi người dùng đăng xuất (**US-08**) hoặc khi Admin khóa tài khoản (**AD-03**).
- **Khóa & Quan hệ**:
  - `PK_RefreshTokens`: `Id` (`uniqueidentifier`).
  - `IX_RefreshTokens_Token` (**Unique Index**): Tra cứu token nhanh với độ phức tạp $O(1)$.
  - `FK_RefreshTokens_Users_UserId`: Quan hệ `N : 1` với `Users` (`OnDelete: Cascade`).

### 3.4. Bảng `VerificationTokens` (Xác thực OTP Email & Quên mật khẩu)
- **Mục đích**: Lưu mã OTP 6 chữ số để kích hoạt tài khoản (**US-03**) và đặt lại mật khẩu.
- **Khóa & Quan hệ**:
  - `PK_VerificationTokens`: `Id` (`uniqueidentifier`).
  - `FK_VerificationTokens_Users_UserId`: Quan hệ `N : 1` với `Users` (`OnDelete: Cascade`).

### 3.5. Bảng `BlockedUsers` (Danh sách chặn tương tác — US-10)
- **Mục đích**: Cho phép người dùng chặn một tài khoản khác để ngăn tương tác không mong muốn (**US-10**).
- **Khóa & Chỉ mục**:
  - `PK_BlockedUsers`: `Id` (`uniqueidentifier`).
  - `IX_BlockedUsers_BlockerUserId_BlockedTargetUserId` (**Composite Unique Index**): Đảm bảo một người không bị chặn trùng lặp nhiều lần bởi cùng một người.
