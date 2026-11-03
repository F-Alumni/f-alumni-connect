# 🤝 Hướng dẫn Đóng góp & Quy chuẩn Làm việc Nhóm (Contributing Guide)

> **Dự án**: FPT Alumni Connect (PRN232 Final Assignment)  
> **Áp dụng cho**: Toàn bộ 5 thành viên nhóm phát triển (3 Backend, 1 Web, 1 Mobile).

Chào mừng bạn đến với dự án **FPT Alumni Connect**! Để đảm bảo 5 thành viên có thể phối hợp nhịp nhàng trên các repository độc lập mà không làm hỏng code của nhau, mọi thành viên bắt buộc phải đọc và tuân theo các quy định trong tài liệu này.

---

## 📑 Mục lục
1. [Danh sách Repository & Phân công Sở hữu](#1-danh-sách-repository--phân-công-sở-hữu)
2. [Quy chuẩn Phân nhánh (Branching Convention)](#2-quy-chuẩn-phân-nhánh-branching-convention)
3. [Quy trình Phát triển & Mở Pull Request (PR Workflow)](#3-quy-trình-phát-triển--mở-pull-request-pr-workflow)
4. [Quy chuẩn Thông điệp Commit (Conventional Commits)](#4-quy-chuẩn-thông-điệp-commit-conventional-commits)
5. [Phối hợp Thay đổi Hợp đồng API (Contract Coordination)](#5-phối-hợp-thay-đổi-hợp-đồng-api-contract-coordination)
6. [Mẫu Pull Request (PR Template)](#6-mẫu-pull-request-pr-template)
7. [Ví dụ Mẫu Dành cho Người mới](#7-ví-dụ-mẫu-dành-cho-người-mới)

---

## 1. Danh sách Repository & Phân công Sở hữu

Nhóm làm việc theo mô hình Multi-Repo, bao gồm 9 repository chính thức:

| Tên Repository | Công nghệ | Chủ sở hữu chính (Primary Owner) | Người duyệt chéo (Reviewer) |
| :--- | :--- | :--- | :--- |
| **`f-alumni-guideline`** | Markdown / Docs | **BE1 (Lead)** | Toàn bộ thành viên |
| **`f-alumni-infrastructure`** | Docker / YARP Gateway | **BE3** | BE1, BE2 |
| **`f-alumni-user-service`** | .NET 10 Web API | **BE1 (Lead)** | BE2, BE3 |
| **`f-alumni-chat-service`** | .NET 10 Web API | **BE1 (Lead)** | BE3, Mobile |
| **`f-alumni-forum-service`** | .NET 10 Web API | **BE2** | BE1, Web |
| **`f-alumni-ai-service`** | .NET 10 Web API | **BE2** | BE1, Mobile |
| **`f-alumni-admin-service`** | .NET 10 Web API | **BE3** | BE1, Web |
| **`f-alumni-web`** | React 18, Vite, TS | **Web Dev** | BE1, BE2 |
| **`f-alumni-mobile`** | Flutter, Dart | **Mobile Dev** | BE1, BE3 |

---

## 2. Quy chuẩn Phân nhánh (Branching Convention)

Nhóm áp dụng mô hình phân nhánh tinh gọn:
- **`main`**: Nhánh ổn định (Production / Milestone demo). Tuyệt đối **không commit trực tiếp** lên `main`.
- **`develop`**: Nhánh tích hợp hằng ngày của toàn nhóm. Mọi nhánh tính năng đều rẽ ra từ đây và merge về đây.
- **`feat/<ticket>-<tên_ngắn_gọn>`**: Nhánh phát triển tính năng mới (Ví dụ: `feat/US-01-register`, `feat/CH-02-realtime-chat`).
- **`fix/<ticket>-<tên_ngắn_gọn>`**: Nhánh sửa lỗi phát sinh (Ví dụ: `fix/US-03-jwt-expiration`, `fix/FO-08-reaction-counter`).

---

## 3. Quy trình Phát triển & Mở Pull Request (PR Workflow)

1. **Luôn bắt đầu từ `develop` mới nhất**:
   ```bash
   git checkout develop
   git pull origin develop
   ```
2. **Tạo nhánh tính năng hoặc sửa lỗi**:
   ```bash
   git checkout -b feat/<ticket>-<tên_tính_năng>
   ```
3. **Phát triển và tự kiểm thử cục bộ**:
   - Chạy build và test không có lỗi.
   - Tuyệt đối không can thiệp trực tiếp vào database của service khác (Zero cross-database access).
4. **Commit và Push lên GitHub**:
   ```bash
   git add <các_file_thay_đổi>
   git commit -m "loại(phạm_vi): mô tả thay đổi"
   git push -u origin feat/<ticket>-<tên_tính_năng>
   ```
5. **Mở Pull Request vào nhánh `develop`**:
   - Base branch: **`develop`**.
   - Điền đầy đủ thông tin vào [Pull Request Template](#6-mẫu-pull-request-pr-template).
   - Yêu cầu ít nhất **1 lượt Approve** từ Reviewer trước khi merge.
   - Khuyến khích chọn tùy chọn **Squash and Merge** để giữ lịch sử git gọn gàng.

---

## 4. Quy chuẩn Thông điệp Commit (Conventional Commits)

Cú pháp chuẩn: `<loại>(<phạm_vi_tùy_chọn>): <mô tả>`

Các loại tiền tố:
- `feat`: Tính năng mới (ví dụ: `feat(chat): implement SignalR direct messaging`).
- `fix`: Sửa lỗi (ví dụ: `fix(auth): fix OTP expiration calculation`).
- `docs`: Cập nhật tài liệu (ví dụ: `docs: update team workflow guide`).
- `refactor`: Tái cấu trúc mã nguồn (ví dụ: `refactor(forum): extract PostRepository interface`).
- `test`: Bổ sung hoặc chỉnh sửa test (ví dụ: `test(user): add unit tests for AuthService`).
- `chore`: Cấu hình build, dependencies, tooling (ví dụ: `chore: update MassTransit package version`).

---

## 5. Phối hợp Thay đổi Hợp đồng API (Contract Coordination)

> [!WARNING]
> Khi một thay đổi có nguy cơ phá vỡ giao tiếp giữa 2 service hoặc giữa Backend với Frontend Web/Mobile (Breaking Change):

1. **Họp/Nhắn tin thống nhất trước**: Không được tự ý đổi endpoint REST, file gRPC `.proto`, hoặc cấu trúc Kafka event payload mà chưa có sự đồng ý của bên gọi API.
2. **Hỗ trợ tương thích ngược (Backward Compatibility)**:
   - Thêm trường mới hoặc endpoint mới song song với trường cũ.
   - Gắn nhãn đánh dấu trường cũ sắp bị hủy (`[Obsolete]` / deprecated).
3. **Quy trình 2 pha (Two-Phase Update)**:
   - Pha 1: Service cung cấp API triển khai phiên bản mới và thông báo cho người tiêu thụ (Consumer).
   - Pha 2: Service/Client tiêu thụ cập nhật code tương ứng. Sau khi mọi bên đã chạy ổn định mới dọn dẹp code cũ.

---

## 6. Mẫu Pull Request (PR Template)

Mọi PR trên GitHub cần sử dụng mẫu sau (đã được lưu tại [.github/pull_request_template.md](file:///.github/pull_request_template.md)):

```markdown
## 🎯 Mục đích PR
- Triển khai ticket / User Story: [Mã US/CH/FO/AI/AD]

## 📦 Danh sách thay đổi chính
- [ ] Mô tả thay đổi 1
- [ ] Mô tả thay đổi 2

## ⚠️ Thay đổi Hợp đồng API / Breaking Change
- [ ] Có thay đổi Contract (Đã thống nhất với: @username)
- [ ] Không có breaking change

## 🧪 Kết quả Kiểm thử Cục bộ (Self-Verification)
- [ ] Đã chạy build / test thành công cục bộ không lỗi.
- [ ] Đã kiểm thử qua Swagger / Postman hoạt động đúng.
- [ ] Tuân thủ quy tắc Zero Cross-Database Access.

## 📸 Hình ảnh minh chứng (Nếu có)
<!-- Đính kèm ảnh Swagger hoặc UI -->
```

---

## 7. Ví dụ Mẫu Dành cho Người mới

Giả sử bạn là **BE2** nhận nhiệm vụ làm tính năng `FO-02` (Đăng bài viết mới trên Forum Service):

```bash
# 1. Chuyển vào thư mục repo Forum Service
cd c:\Use\University\PRN232\ASM\f-alumni-forum-service

# 2. Cập nhật nhánh develop
git checkout develop
git pull origin develop

# 3. Tạo nhánh làm việc mới
git checkout -b feat/FO-02-create-post

# 4. Viết code tính năng, sau đó kiểm tra file
git status

# 5. Thêm file và commit
git add src/FAlumni.Forum.API/Controllers/PostsController.cs
git add src/FAlumni.Forum.Services/Implementations/PostService.cs
git commit -m "feat(forum): add endpoint to create new discussion post"

# 6. Đẩy nhánh lên remote
git push -u origin feat/FO-02-create-post

# 7. Lên GitHub mở Pull Request vào nhánh `develop`, gắn reviewer BE1 và điền PR Template!
```

---

Để xem tài liệu chi tiết và đầy đủ hơn, vui lòng tham khảo:
- [docs/14-team-workflow.md](file:///c:/Use/University/PRN232/ASM/guideline/docs/14-team-workflow.md): Hướng dẫn quy trình phát triển và tiêu chuẩn git chi tiết.
- [docs/02-architecture.md](file:///c:/Use/University/PRN232/ASM/guideline/docs/02-architecture.md): Tài liệu kiến trúc hệ thống FPT Alumni Connect.
- [docs/03-service-boundaries.md](file:///c:/Use/University/PRN232/ASM/guideline/docs/03-service-boundaries.md): Ranh giới dịch vụ và ma trận phân loại User Stories.
