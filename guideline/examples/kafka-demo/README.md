# FPT Alumni Connect — Kafka Demo Example

Dự án mẫu minh họa luồng phát sinh và tiêu thụ sự kiện bất đồng bộ qua **Apache Kafka** sử dụng thư viện **`Confluent.Kafka`** (.NET 10).

---

## 📁 Cấu trúc Dự án

```text
examples/kafka-demo/
├── KafkaDemo.sln
├── src/
│   ├── KafkaDemo.Contracts/    # Hợp đồng C# (EventEnvelope<T>, UserRegisteredEventV1)
│   ├── KafkaProducer/          # Console App gửi event với Key = UserId và Acks = All
│   └── KafkaConsumer/          # Console App tiêu thụ event, xử lý an toàn và Idempotent
└── tests/
    └── KafkaDemo.Tests/        # 10 Unit Tests (Serialization, Idempotency, Mock Broker)
```

---

## 🚀 Hướng dẫn Chạy Kiểm thử Tự động (Không cần Broker ngoài)

Tất cả logic đóng gói envelope, lọc trùng lặp idempotent, từ chối JSON lỗi và kiểm tra xuất bản (sử dụng in-process mock broker) có thể chạy trực tiếp:

```powershell
dotnet test KafkaDemo.sln
```

Kết quả mong đợi:
```text
Passed!  - Failed: 0, Passed: 10, Skipped: 0, Total: 10
```

---

## 📡 Hướng dẫn Chạy Producer & Consumer với Kafka Broker

Khi có một broker Kafka đang lắng nghe (mặc định: `localhost:9092` hoặc cấu hình qua tham số dòng lệnh / biến môi trường `KAFKA_BOOTSTRAP_SERVERS`):

### 1. Chạy Consumer
```powershell
dotnet run --project src/KafkaConsumer [bootstrap_servers]
# Ví dụ:
dotnet run --project src/KafkaConsumer localhost:9092
```

### 2. Chạy Producer
```powershell
dotnet run --project src/KafkaProducer [bootstrap_servers]
# Ví dụ:
dotnet run --project src/KafkaProducer localhost:9092
```

---

## 📖 Tài liệu Chi tiết

Xem chi tiết quy chuẩn kiến trúc sự kiện, cơ chế Manual Offset Commit, chống Poison Pill, và giải pháp Transactional Outbox tại:
- [docs/08-kafka.md](file:///c:/Use/University/PRN232/ASM/guideline/docs/08-kafka.md)
- [contracts/events/](file:///c:/Use/University/PRN232/ASM/guideline/contracts/events/)
