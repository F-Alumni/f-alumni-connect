# Sequence Diagram — User Service (`f-alumni-user-service`)

> **Thành viên thực hiện**: Trương Chí Trung (`chstrg2208` — Backend 1)  
> **Service**: `f-alumni-user-service` (.NET 10)  
> **File Draw.io (Trắng Đen Chuẩn UML)**: [`docs/architecture/f-alumni-user-sequence-diagram.drawio`](architecture/f-alumni-user-sequence-diagram.drawio)

---

## 1. Luồng 1 & 2: Đăng ký Tài khoản, Gửi OTP qua Kafka, Xác thực Email & Đăng nhập JWT / Google OAuth (`US-01..US-03`)

```mermaid
sequenceDiagram
    autonumber
    actor Client as Client (Web / Mobile)
    participant GW as YARP API Gateway (:8080)
    participant Ctrl as AuthController (:5001)
    participant Svc as AuthService
    participant Repo as UserRepository
    participant DB as FAlumni_User_Db
    participant Kafka as Apache Kafka Broker

    Note over Client,Kafka: Flow 1: US-02 & US-03 — Đăng ký Tài khoản & Xác thực OTP qua Kafka
    Client->>GW: POST /api/users/auth/register { Email, Password, FullName, Campus }
    GW->>Ctrl: POST /api/v1/auth/register
    Ctrl->>Svc: RegisterAsync(RegisterRequest)
    Svc->>Repo: GetByEmailAsync(email) & CreateAsync(User + UserProfile + VerificationToken)
    Repo->>DB: INSERT Users, UserProfiles, VerificationTokens (6-digit OTP)
    DB-->>Repo: Saved Entities
    Svc-)Kafka: PublishAsync("user.registered.v1", { userId, email, otpCode })
    Svc-->>Ctrl: RegisterResponse { userId, status="PendingVerification" }
    Ctrl-->>Client: 201 Created

    Client->>GW: POST /api/users/auth/verify-email { Email, OtpCode }
    GW->>Ctrl: POST /api/v1/auth/verify-email
    Ctrl->>Svc: VerifyEmailAsync(VerifyEmailRequest)
    Svc->>Repo: GetValidVerificationTokenAsync() & MarkVerificationTokenUsedAsync()
    Repo->>DB: UPDATE Users SET Status="Active", EmailVerified=1 & INSERT RefreshTokens
    Svc-->>Client: 200 OK (AuthTokenResponse { accessToken, refreshToken, userProfile })

    Note over Client,DB: Flow 2: US-01 — Đăng nhập Email hoặc Google OAuth ID Token
    Client->>GW: POST /api/users/auth/login (hoặc /api/users/auth/google)
    GW->>Ctrl: POST /api/v1/auth/login (hoặc /api/v1/auth/google)
    Ctrl->>Svc: LoginAsync / GoogleLoginAsync
    Svc->>Repo: GetByEmailAsync(email) & CreateRefreshTokenAsync(RefreshToken)
    Repo->>DB: SELECT User + UserProfile & INSERT RefreshTokens
    Svc-->>Client: 200 OK (AuthTokenResponse { accessToken, refreshToken, userProfile })
```

---

## 2. Luồng 3 & 4: Tra cứu Danh bạ, Cập nhật Hồ sơ/Privacy, Chặn Người dùng (`US-04..US-11`) & Admin Khóa Tài khoản (`AD-03`)

```mermaid
sequenceDiagram
    autonumber
    actor Client as Client / Admin Service
    participant GW as YARP API Gateway (:8080)
    participant Ctrl as Users / InternalUsersController (:5001)
    participant Svc as UserProfileAppService
    participant Repo as UserRepository
    participant DB as FAlumni_User_Db
    participant Kafka as Apache Kafka Broker

    Note over Client,DB: Flow 3: US-04..US-11 — Tra cứu Danh bạ, Cập nhật Hồ sơ, Privacy & Chặn Người dùng
    Client->>GW: GET /api/users/directory?search=...&campus=...&graduationYear=...
    GW->>Ctrl: GET /api/v1/users/directory
    Ctrl->>Svc: GetDirectoryPagedAsync(...)
    Svc->>Repo: GetDirectoryPagedAsync(...)
    Repo->>DB: SELECT Users JOIN UserProfiles WHERE Status='Active'
    DB-->>Svc: Paged Items (Áp dụng bộ lọc Privacy US-09)
    Svc-->>Client: 200 OK (PagedResponse<UserProfileResponse>)

    Client->>GW: PUT /api/users/me (hoặc /me/privacy, /me/calendar, POST /{id}/block)
    GW->>Ctrl: PUT /api/v1/users/me
    Ctrl->>Svc: UpdateProfileAsync / UpdatePrivacySettingsAsync / BlockUserAsync
    Svc->>Repo: UpdateAsync(User) / BlockUserAsync(blockerId, targetId)
    Repo->>DB: UPDATE UserProfiles / INSERT BlockedUsers
    Svc-->>Client: 200 OK (UserProfileResponse / BlockedUserResponse)

    Note over Client,Kafka: Flow 4: AD-03 — Admin Khóa Tài khoản (SuspendUser) & Thu hồi toàn bộ Phiên
    Client->>Ctrl: POST /api/v1/internal/users/{id}/suspend { Reason }
    Ctrl->>Svc: SuspendUserAsync(userId, SuspendUserRequest)
    Svc->>Repo: UpdateAsync(Status="Suspended") & RevokeAllUserRefreshTokensAsync(userId)
    Repo->>DB: UPDATE Users SET Status='Suspended' & UPDATE RefreshTokens SET IsRevoked=1
    Svc-)Kafka: PublishAsync("user.suspended.v1", { userId, reason, suspendedAtUtc })
    Svc-->>Client: 200 OK (UserProfileResponse { status="Suspended" })
```
