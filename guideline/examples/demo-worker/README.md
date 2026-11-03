# FPT Alumni Connect — Notification Background Worker Demo

Dự án mẫu minh họa dịch vụ chạy ngầm (**Background Worker Service**) tiêu thụ bất đồng bộ sự kiện `user.registered.v1` từ Apache Kafka để gửi email thông báo xác thực tài khoản.

---

## 📁 Cấu trúc Dự án

```text
examples/demo-worker/
├── DemoWorker.sln
├── src/
│   └── NotificationWorker/        # .NET 10 Worker Service kế thừa BackgroundService
│       ├── Abstractions/
│       │   └── IEmailSender.cs    # Interface trừu tượng hóa dịch vụ gửi email
│       ├── Services/
│       │   ├── MockEmailSender.cs # Implementation gửi email giả lập (che giấu thông tin nhạy cảm)
│       │   ├── IProcessedEventTracker.cs # Quản lý khử trùng lặp (Idempotent tracking)
│       │   └── NotificationEventProcessor.cs # Xử lý nghiệp vụ chính & bóc tách phong bì sự kiện
│       ├── Workers/
│       │   └── NotificationBackgroundService.cs # Vòng lặp lắng nghe Kafka & commit offset
│       ├── Program.cs             # Khởi tạo Generic Host & cấu hình Dependency Injection
│       └── appsettings.json       # Cấu hình BootstrapServers, Topic và Consumer Group
└── tests/
    └── NotificationWorker.Tests/  # 10 Unit Tests (Xử lý sự kiện, lọc trùng, che giấu OTP, commit offset)
        └── WorkerTests.cs
```

---

## 🚀 Hướng dẫn Chạy Kiểm thử Tự động

Chạy toàn bộ 10 bài test tự động kiểm tra xử lý sự kiện, khử trùng lặp (Idempotency), an toàn bảo mật log và cam kết offset:

```powershell
dotnet test examples/demo-worker/DemoWorker.sln
```

Kết quả mong đợi:
```text
Passed!  - Failed: 0, Passed: 10, Skipped: 0, Total: 10, Duration: 374 ms - NotificationWorker.Tests.dll
```

---

## 📡 Hướng dẫn Khởi chạy Worker Thực tế

Khi bạn có một broker Kafka đang lắng nghe (mặc định: `localhost:9092`):

```powershell
dotnet run --project examples/demo-worker/src/NotificationWorker
```

### Kết quả đầu ra mẫu khi nhận sự kiện:
```text
info: NotificationWorker.Workers.NotificationBackgroundService[0]
      Notification Background Worker started. Listening for events on Kafka topic 'user.registered.v1'...
info: NotificationWorker.Workers.NotificationBackgroundService[0]
      Received Kafka message from Partition 0 at Offset 12
info: NotificationWorker.Services.NotificationEventProcessor[0]
      Consuming event: EventId=8f7e6d5c-4b3a-2109-8765-43210fedcba9, CorrelationId=req-9b1deb4d, EventType=user.registered.v1, Producer=f-alumni-user-service
info: NotificationWorker.Services.MockEmailSender[0]
      [MOCK EMAIL DELIVERED] Recipient: st***@fpt.edu.vn | Subject: Chào mừng bạn đến với FPT Alumni Connect — Mã xác thực tài khoản | Provider: DevMock | Timestamp: 2026-10-24T10:25:00Z
info: NotificationWorker.Services.NotificationEventProcessor[0]
      Email dispatched successfully: EventId=8f7e6d5c-4b3a-2109-8765-43210fedcba9, CorrelationId=req-9b1deb4d
```

### Kết quả khi nhận lại thông điệp trùng lặp:
```text
warn: NotificationWorker.Services.NotificationEventProcessor[0]
      Duplicate event skipped: EventId=8f7e6d5c-4b3a-2109-8765-43210fedcba9, CorrelationId=req-9b1deb4d was already processed (Idempotent).
```

---

## 📖 Tài liệu Chi tiết

Xem chi tiết kiến trúc worker, cơ chế bảo mật Opaque Verification Reference, và hướng dẫn thay thế nhà cung cấp email thực tế tại:
- [docs/10-background-worker.md](file:///c:/Use/University/PRN232/ASM/guideline/docs/10-background-worker.md)
- [contracts/events/](file:///c:/Use/University/PRN232/ASM/guideline/contracts/events/)
