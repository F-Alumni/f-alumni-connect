# 🎯 Nội Dung Slide Đồ Án: FPT Alumni Connect (Chuẩn Guideline V11)

> **Hệ sinh thái:** FPT Alumni Connect (`f-alumni-connect` • `-F-Alumni---Web` • `F-Alumni-Mobile`)  
> **Cấu trúc chuẩn V11:** 24 Slide chính (4 phần, ≈ 18–20 phút) + Slide 25 Phụ lục (`A1–A6`)  
> **Nguyên tắc chung:** Mỗi slide truyền tải **1 thông điệp kết luận** ở tiêu đề, trả lời đúng **Câu hỏi hội đồng**, trình bày tổng quan ở phần **Design** và chi tiết hóa 3 workflow liên hoàn ở phần **Demo**, mọi nhận định đều gắn **Minh chứng thực tế**.  
> *(Lưu ý: Các mục đánh dấu `[Nhóm điền thêm...]` ở Slide 02–03 là số liệu khảo sát ngoài mã nguồn để nhóm bổ sung theo thực tế bảo vệ).*

---

## 📋 MỤC LỤC & PHÂN BỔ THỜI GIAN (TỔNG ≈ 18.5 PHÚT)

| Phần | Slide | Nội dung | Thời lượng |
| :--- | :--- | :--- | :---: |
| **P1 · PROBLEM** | `01 – 06` | Overview, Context, Problem, Proposed Solution, Main Actors, Main Features | **≈ 3.5′** |
| **P2 · DESIGN** | `07 – 16` | Business Flow, State Machine, Architecture, Tech Stack, UI/UX, Sequence*, Class, Conceptual ERD*, Physical DB, Traceability | **≈ 6.0′** |
| **P3 · DEMO** | `17 – 20` | Demo Plan, Demo WF-01 (Alumni), Demo WF-02 (Student), Demo WF-03 (Admin) | **≈ 6.5′** |
| **P4 · PROOF** | `21 – 24` | Testing & Verification, Achievements, Limitations & Next Steps, Conclusion | **≈ 2.5′** |
| **PHỤ LỤC** | `25 (A1–A6)` | Slide dự phòng mở khi hội đồng hỏi sâu (không chiếu trong bài nói chính) | **0 s** |

*(Ghi chú `*`: Nếu hội đồng giới hạn 15 phút, có thể gộp Slide 02 với 03, gộp Slide 14 vào 15, đưa Slide 12 vào Phụ lục và demo 2 workflow cốt lõi).*

---

## PHẦN 1: PROBLEM (`≈ 3.5 phút`)

### SLIDE 01 / OVERVIEW *(Thời lượng: 30s)*
- **Câu hỏi hội đồng:** Đây là sản phẩm gì, cho ai, và đã chạy được chưa?
- **Thông điệp kết luận:** **FPT Alumni Connect — Nền tảng kết nối mạng lưới Cựu sinh viên, Sinh viên & Giảng viên Đại học FPT đã vận hành đồng bộ trên Web & Mobile**
- **Nội dung (4 ý):**
  1. **Tên đề tài & Dạng sản phẩm:** *FPT Alumni Connect* — Hệ sinh thái phần mềm đa nền tảng (Web Portal + Mobile App Flutter) vận hành trên kiến trúc Microservices.
  2. **Một câu mô tả giá trị:** Kết nối sinh viên, cựu sinh viên (Alumni) và nhà trường tại các cơ sở FPTU để trao đổi học thuật, hỏi đáp nghề nghiệp, tìm kiếm cố vấn (Mentor) và quản trị cộng đồng tập trung.
  3. **Đối tượng & 3 giá trị cốt lõi:**
     - *Đối tượng:* Sinh viên FPTU, Cựu sinh viên (Alumni), Giảng viên / Điều phối viên (Faculty / Moderator), Quản trị viên (Admin).
     - *3 Giá trị:* (1) Lưu trữ & tìm kiếm tri thức chuyên ngành có kiểm chứng; (2) Kết nối cố vấn AI Mentor & Chat thời gian thực; (3) Kiểm duyệt tập trung, minh bạch dấu vết (Audit Log).
  4. **Thông tin nhóm & Trạng thái:** Nhóm **F-Alumni** (5 thành viên: Trương Chí Trung, Đỗ Trọng Tín, Vinh, Nguyễn Quang Tâm, Khải) — GVHD: Thầy **Phuonglhk** — Hệ thống đã chạy end-to-end qua Docker Compose + YARP API Gateway (`:5000`).
- **Minh chứng bắt buộc:**
  - Ảnh chụp thật sản phẩm đang chạy: Home Feed Web (`-F-Alumni---Web/screenshots/02_user_home_feed.png`) + Mobile Home Feed (`F-Alumni-Mobile/docs/evidence/auth/Step_06_DangNhap_ThanhCong_HomeFeed.png`).
- **Gợi ý bố cục:**
  - *Trái:* 4 khối `PROJECT NAME` | `USERS` | `VALUE` | `PLATFORM (React Web • Flutter Android • .NET 10 Microservices)`.
  - *Phải:* Mockup/ảnh màn hình thật Web + Mobile cạnh nhau.

---

### SLIDE 02 / CONTEXT *(Thời lượng: 30s)*
- **Câu hỏi hội đồng:** Đề tài ra đời trong bối cảnh nào, công việc hiện được thực hiện ra sao?
- **Thông điệp kết luận:** **Cộng đồng FPTU đông đảo qua nhiều khóa và cơ sở nhưng hoạt động kết nối hiện nay vẫn phân tán trên các kênh rời rạc**
- **Nội dung (4 ý):**
  1. **Môi trường, lĩnh vực & quy mô:** Cộng đồng giáo dục – cựu sinh viên Đại học FPT phân bố trên **5 cơ sở** (`FU-HL`, `FU-HCM`, `FU-DN`, `FU-CT`, `FU-QN`) với nhiều chuyên ngành (`SE`, `AI`, `IA`, `GD`, `IB`, `MC`, `MKT`) qua nhiều khóa (`K13–K20+`).
  2. **Quy trình hiện tại (4 bước):**
     - *Bước 1:* Sinh viên đăng câu hỏi môn học, OJT, Capstone lên các nhóm chat/mạng xã hội tự phát.
     - *Bước 2:* Cựu sinh viên phản hồi rải rác trong bình luận trôi nổi.
     - *Bước 3:* Khi cần cố vấn 1-1 hoặc tìm cơ hội thực tập/việc làm, sinh viên tự liên hệ qua quan hệ cá nhân.
     - *Bước 4:* Nhà trường/CLB thu thập thông tin cựu sinh viên và thông báo qua biểu mẫu, bảng tính thủ công.
  3. **Người liên quan & Công cụ hiện tại:** Sinh viên, Alumni, Cán bộ/Giảng viên hiện dùng **Facebook Groups, Zalo, Messenger, Google Forms/Sheets**.
  4. **Nhu cầu dẫn đến đề tài:** Cần một nền tảng hợp nhất có xác thực danh tính FPT (`@fpt.edu.vn` / `@fe.edu.vn`), lưu giữ tri thức lâu dài và ghép cặp cố vấn đúng chuyên môn.
- **Minh chứng bắt buộc & Bố cục gợi ý:**
  - 4 thẻ ngang: `MÔI TRƯỜNG` | `NGƯỜI LIÊN QUAN` | `CÔNG CỤ HIỆN TẠI` | `QUY TRÌNH HIỆN TẠI (4 bước)`.
  - Thanh `HIỆN TRẠNG` ở đáy: `[Nhóm điền thêm số liệu quy mô khảo sát thực tế: số lượng sinh viên/alumni hoặc số kênh Facebook/Zalo hiện hữu]`.

---

### SLIDE 03 / PROBLEM *(Thời lượng: 40s)*
- **Câu hỏi hội đồng:** Vấn đề có thật không, và gây hậu quả gì?
- **Thông điệp kết luận:** **3 điểm nghẽn khiến tri thức thất thoát, sinh viên khó tìm đúng Mentor và quản trị cộng đồng thiếu kiểm soát**
- **Nội dung (4 ý — Bảng Pain Point → Nguyên nhân → Impact):**

| Pain Point | Nguyên nhân | Impact (Hậu quả) |
| :--- | :--- | :--- |
| **1. Thất thoát & trùng lặp tri thức hỏi đáp** | Bài chia sẻ kinh nghiệm OJT/phỏng vấn trôi nhanh trên mạng xã hội; thiếu phân loại theo Cơ sở/Chuyên ngành và đánh dấu câu trả lời chuẩn. | Sinh viên khóa sau lặp lại cùng câu hỏi học phí, môn năm nhất, đồ án; tốn thời gian tra cứu thủ công. |
| **2. Khó xác thực danh tính & tìm đúng Mentor** | Kênh mạng xã hội mở không ràng buộc email FPT; thiếu bộ lọc kỹ năng/công ty và gợi ý cố vấn theo ngành học. | Nguy cơ spam/lừa đảo thương mại; sinh viên khó tiếp cận đúng cựu sinh viên cùng chuyên ngành (`SE`, `AI`, `IA`...). |
| **3. Thiếu quy trình kiểm duyệt & truy vết** | Xử lý vi phạm thủ công rời rạc từng kênh; không có hàng đợi báo cáo (`Pending → Resolved`) hay nhật ký kiểm toán. | Chậm xử lý nội dung công kích/spam; không thể thống kê KPI vận hành hay xuất báo cáo định kỳ. |

- **Đối tượng ảnh hưởng nhất & Minh chứng:**
  - *Affected users:* Sinh viên năm 3–4 chuẩn bị OJT/Capstone và Quản trị viên cộng đồng.
  - *Minh chứng:* `[Nhóm bổ sung số liệu khảo sát/phỏng vấn thực tế, ghi rõ nguồn & cỡ mẫu N=...]` kết hợp kịch bản dữ liệu thực tế trong `ForumDatabaseSeeder` & `AdminDatabaseSeeder` (case spam link thương mại, bình luận công kích cần hàng đợi kiểm duyệt).

---

### SLIDE 04 / PROPOSED SOLUTION *(Thời lượng: 40s)*
- **Câu hỏi hội đồng:** Giải pháp có trúng vấn đề không, phạm vi làm tới đâu?
- **Thông điệp kết luận:** **Xây dựng nền tảng FPT Alumni Connect tập trung hóa diễn đàn, AI RAG/Mentor và quản trị kiểm toán**
- **Nội dung (4 ý):**
  1. **Giải pháp tổng thể:** Nền tảng **FPT Alumni Connect** chạy trên 5 Microservices độc lập qua YARP Gateway, phục vụ đồng bộ trên Web Portal (React) và Mobile App (Flutter).
  2. **Bảng ánh xạ Pain Point → Solution → Expected Value:**

| Pain Point | Solution trong sản phẩm | Expected Value |
| :--- | :--- | :--- |
| Thất thoát & trôi tri thức | Diễn đàn phân loại theo `Campus / Major / PostType`, chọn `Best Answer` & nạp bài viết tự động vào kho tri thức **AI RAG** qua Kafka. | Lưu trữ tri thức có cấu trúc, AI trả lời kèm **trích dẫn nguồn (`Citations`)**. |
| Khó tìm Mentor & rủi ro danh tính | Xác thực OTP / Google SSO `@fpt.edu.vn` + Thuật toán **AI Mentor Matching** tính `MatchScore` + Chat 1-1 thời gian thực. | Xác thực danh tính FPT và kết nối đúng Mentor chỉ qua một bước bấm **"Kết nối"**. |
| Thiếu kiểm duyệt & truy vết | Hàng đợi `ModerationReports`, khóa/mở user ghi `AuditLogs` bất biến, Dashboard KPI & Export CSV. | Minh bạch trạng thái xử lý vi phạm và truy vết đầy đủ hành động quản trị. |

  3. **Điểm khác biệt đo được:** Kết hợp **Hybrid RAG** (12 tài liệu đào tạo FPTU + bài viết Forum nạp real-time qua Kafka `post.created.v1`) giúp AI trả lời có nguồn kiểm chứng, tránh trả lời sai ngữ cảnh.
  4. **Phạm vi (In-scope MVP vs Out-of-scope):**
     - **IN-SCOPE (MVP):** `US-01..11` (Auth/Profile/Directory), `CH-01..10` (Chat Realtime), `FO-01..16` (Forum/Events), `AI-01..04` (RAG & Mentor Matching), `AD-01..09` (Admin Moderation & Export).
     - **OUT-OF-SCOPE (Giai đoạn sau):** Gọi Video/Audio 1-1 qua WebRTC và Cổng thanh toán Quỹ học bổng (do cần thêm hạ tầng TURN/STUN và tích hợp đối tác thanh toán).

---

### SLIDE 05 / MAIN ACTORS *(Thời lượng: 30s)*
- **Câu hỏi hội đồng:** Ai dùng hệ thống, họ là ai, và mỗi vai trò được làm gì?
- **Thông điệp kết luận:** **Hệ thống phục vụ 4 nhóm người dùng nội bộ FPTU và 3 hệ thống tích hợp ngoài với phân quyền JWT chặt chẽ**
- **Nội dung & Bố cục bảng (khớp phân quyền thật trong code):**

| Actor | Loại | Mô tả ngữ cảnh | Quyền hạn & Nghiệp vụ chính |
| :--- | :--- | :--- | :--- |
| **Student** | Người dùng (`USER`) | Sinh viên FPTU đang học tập, chuẩn bị OJT / Capstone | Đăng nhập OTP/SSO, hỏi đáp Forum, lưu bài, hỏi AI RAG, nhận gợi ý Mentor, chat 1-1/nhóm, báo cáo vi phạm (`US/FO/CH/AI`). |
| **Alumni** | Người dùng (`USER`) | Cựu sinh viên FPTU đang làm việc tại doanh nghiệp | Toàn quyền như Student + chia sẻ kinh nghiệm, đăng tin tuyển dụng (`Recruitment`), gắn link Google Calendar nhận Mentorship. |
| **Admin / Moderator** | Quản trị (`ADMIN` / `MODERATOR`) | Cán bộ quản trị mạng lưới & điều phối viên | Truy cập Admin Portal (`/admin/*`), xem Dashboard KPI, duyệt/bác bỏ `ModerationReports`, khóa/mở tài khoản, đăng thông báo hệ thống, xem Audit Logs & Export CSV (`AD-01..09`). |
| **Google Auth & Gemini API** | Hệ thống ngoài | SSO Workspace & mô hình ngôn ngữ tạo sinh (LLM) | Xác thực OAuth2 Google Sign-In; sinh câu trả lời tự nhiên từ ngữ cảnh RAG (`AiChatService`). |
| **Firebase BaaS** | Hệ thống ngoài | Bộ 6 dịch vụ đám mây trên Mobile App | Lưu trữ ảnh/PDF (`Storage`), gửi Push Notification (`FCM`), `Analytics`, `Crashlytics`, `Remote Config`. |

---

### SLIDE 06 / MAIN FEATURES *(Thời lượng: 45s)*
- **Câu hỏi hội đồng:** Hệ thống làm được gì, và đâu là các workflow quan trọng nhất?
- **Thông điệp kết luận:** **5 nhóm tính năng nghiệp vụ hội tụ thành 3 workflow lõi xuyên suốt sẽ demo trực tiếp**
- **Nội dung (4 ý):**
  1. **5 nhóm tính năng:** (1) Tài khoản & Danh bạ (`US-01..11`); (2) Diễn đàn & Sự kiện (`FO-01..16`); (3) Trò chuyện Real-time (`CH-01..10`); (4) AI RAG & Mentor Matching (`AI-01..04`); (5) Quản trị & Kiểm toán (`AD-01..09`).
  2. **3 Workflow lõi chọn để Demo (Slide 18–20):**

| Mã WF | Tên Workflow | Actor | Trigger | Kết quả đầu ra | Tính năng bao gồm |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **WF-01** | Chia sẻ Forum & Nạp tri thức AI | **Alumni** | Alumni cần chia sẻ kinh nghiệm OJT/phỏng vấn | Bài mới hiển thị Feed (SignalR) & nạp thành `RagChunk` qua Kafka | Soạn bài • Kiểm tra `BR-02` • Tag/Campus/Major • Broadcast |
| **WF-02** | AI Gợi ý Mentor & Chat Real-time | **Student** | Sinh viên cần cố vấn chuyên ngành phù hợp | Danh sách Mentor kèm `MatchScore` → Mở phòng chat SignalR | AI Recommend • Kết nối 1-1 • Kiểm tra `BR-04` • Nhắn tin |
| **WF-03** | Kiểm duyệt, Khóa User & Báo cáo | **Admin** | Có báo cáo vi phạm ở trạng thái `Pending` | Báo cáo `Resolved`, user vi phạm bị `Suspended`, ghi `AuditLog`, xuất CSV | Duyệt báo cáo • Khóa tài khoản • Lưu vết bất biến • Export |

  3. **MVP vs Mở rộng:** 50 User Stories (`41` Non-Admin + `9` Admin) đều thuộc MVP đã hoàn thiện; WebRTC Call và Scholarship Payment để ở giai đoạn sau.
  4. **Liên kết:** WF-01 giải quyết thất thoát tri thức; WF-02 giải quyết tìm Mentor; WF-03 giải quyết kiểm duyệt & truy vết.
- **Footer gợi ý:** *Ngoài workflow lõi: Danh bạ Alumni • Quyền riêng tư (`US-09`) • Chặn người dùng (`US-10`) • Google Calendar (`US-11`) • Thông báo hệ thống (`AD-08`) • AI Chatbot 24/7.*

---

## PHẦN 2: DESIGN — TOÀN HỆ THỐNG (`≈ 6 phút`)
> *Nguyên tắc: Trình bày toàn hệ thống bằng các sơ đồ tổng quan; không lặp lại chi tiết từng workflow.*

### SLIDE 07 / BUSINESS FLOW *(Thời lượng: 60s)*
- **Câu hỏi hội đồng:** Nghiệp vụ của hệ thống vận hành thế nào, ai làm gì ở đâu?
- **Thông điệp kết luận:** **Luồng nghiệp vụ khép kín kết nối Sinh viên/Alumni, Hệ thống Microservices và Quản trị viên qua 6 quy tắc nghiệp vụ (BR)**
- **Nội dung (4 ý):**
  1. **Một sơ đồ Activity/BPMN tổng thể với 4 Swimlane:** `User (Student/Alumni)` | `API Gateway & Backend Services` | `Kafka / AI Engine` | `Admin / Moderator`.
  2. **Chuỗi bước chính:** Đăng ký & xác thực OTP (`BR-01`) → Lọc/đăng bài Forum (`BR-02`, `BR-03`) → Hệ thống phát sự kiện `post.created.v1` qua Kafka nạp tri thức AI RAG → Sinh viên hỏi AI / nhận gợi ý Mentor và tạo phòng chat (`BR-04`) → Khi phát sinh vi phạm (`BR-05`), Admin tiếp nhận báo cáo, xử lý kỷ luật & lưu dấu vết (`BR-06`).
  3. **Nhánh ngoại lệ quan trọng:** Chặn đăng nhập khi `PendingVerification` hoặc `Suspended` (`FAIL BR-01`); chặn đăng bài khi thiếu tiêu đề/nội dung (`FAIL BR-02`); chặn tạo phòng chat `< 2` thành viên (`FAIL BR-04`); chặn truy cập Admin API khi thiếu role `ADMIN` (`FAIL BR-05`).
  4. **Bảng mã Business Rule gắn tại điểm rẽ:**
     - `BR-01`: Tài khoản phải ở trạng thái `Active` (đã xác thực OTP) và không bị `Suspended` mới được cấp JWT đăng nhập.
     - `BR-02`: Bài viết bắt buộc có Tiêu đề `5–300` ký tự và Nội dung `≥ 10` ký tự.
     - `BR-03`: Chỉ tác giả bài viết mới có quyền sửa/xóa bài hoặc đánh dấu `Best Answer` (trừ Admin).
     - `BR-04`: Hội thoại phải có tối thiểu `2` thành viên phân biệt và không bị chặn lẫn nhau (`BlockedUsers`).
     - `BR-05`: Chỉ tài khoản có role `ADMIN` / `MODERATOR` trong JWT mới được gọi API `/api/v1/admin/*`.
     - `BR-06`: Mọi thao tác kỷ luật (`Suspend`, `Resolve/Dismiss Report`, `Announcement`) bắt buộc ghi `AuditLog` bất biến.
- **Minh chứng bắt buộc:** 1 sơ đồ Activity tổng thể 4 swimlane có điểm rẽ kim cương gắn mã `BR-01..BR-06` và nhánh `FAIL` tô đỏ.

---

### SLIDE 08 / STATE MACHINE *(Thời lượng: 40s)*
- **Câu hỏi hội đồng:** Đối tượng chính đi qua những trạng thái nào, chuyển nào bị cấm?
- **Thông điệp kết luận:** **Kiểm soát chặt chẽ vòng đời Tài khoản (`User.Status`) và Báo cáo vi phạm (`ModerationReport.Status`) bằng Event & Guard**
- **Nội dung (4 ý):**
  1. **Chọn Business Entity có vòng đời cốt lõi:** **`User`** (`FAlumni_User_Db`) và **`ModerationReport`** (`FAlumni_Admin_Db`).
  2. **Các chuyển trạng thái hợp lệ (`Event [Guard] / Action`):**
     - `PendingVerification` $\xrightarrow{\text{verifyEmail [OTP hợp lệ \& chưa hết hạn] / EmailVerified=true}}$ `Active`
     - `Active` $\xrightarrow{\text{suspend [Admin role \& Reason >= 3 chars] / Lưu Reason, RevokeAllRefreshTokens, AuditLog}}$ `Suspended`
     - `Suspended` $\xrightarrow{\text{unsuspend [Admin role] / Xóa SuspensionReason, AuditLog}}$ `Active`
     - `ModerationReport: Pending` $\xrightarrow{\text{resolve/dismiss [Admin role \& ActionTaken/Reason hợp lệ] / Ghi AuditLog}}$ `Resolved` / `Dismissed`
  3. **Phân biệt với UI state:** State nghiệp vụ lưu bền vững trong DB khác biệt hoàn toàn với UI loading/empty/error state ở tầng Client.
  4. **Chuyển trạng thái BỊ CẤM (đã có kiểm thử chặn):**
     - **Cấm:** `PendingVerification → Active` khi OTP sai/hết hạn (`400 Bad Request`).
     - **Cấm:** `PendingVerification → Authenticated Session` (`403 Forbidden` khi login chưa verify email).
     - **Cấm:** `Suspended → Active` tự kích hoạt hoặc `Suspended → Login` (`403 Forbidden` kèm lý do vi phạm).
- **Minh chứng bắt buộc:** State Diagram có node khởi đầu `●`, `Event [Guard] / Action` trên từng cung, và khung cảnh báo các chuyển trạng thái bị cấm.

---

### SLIDE 09 / SYSTEM ARCHITECTURE *(Thời lượng: 50s)*
- **Câu hỏi hội đồng:** Hệ thống gồm những khối nào, chạy ở đâu, vì sao thiết kế như vậy?
- **Thông điệp kết luận:** **Kiến trúc Microservices phân tách 5 dịch vụ độc lập qua YARP Gateway, giao tiếp đồng bộ (REST/gRPC/SignalR) & bất đồng bộ (Kafka)**
- **Nội dung (4 ý):**
  1. **Mô hình C4 Container:**
     - **Clients:** Web App (React/Vite) & Mobile App (Flutter Android).
     - **API Gateway:** YARP Reverse Proxy (`Port 5000`) — điểm vào duy nhất cho REST (`/api/v1/*`) & SignalR (`/hubs/*`).
     - **5 Microservices (.NET 10):** `User` (`:5001`, gRPC `:50051`), `Chat` (`:5002`), `Forum` (`:5003`), `AI` (`:5004`), `Admin` (`:5005`).
     - **Data & Broker:** 5 SQL Server 2022 DBs cô lập (`:14333`) + Apache Kafka KRaft (`:9092/:29092`).
     - **External Systems:** Google OAuth2, Google Gemini API (`v1beta`), Firebase Project `alumniconnect-295ed` (6 dịch vụ).
  2. **Giao tiếp & Chiều dữ liệu (Sync / Async):**
     - *Sync:* Client ↔ Gateway (HTTPS/REST & SignalR WebSocket); Chat → User (**gRPC Protobuf** `UserProfileGrpc`); Admin → User (REST nội bộ `/api/v1/internal/users/*`).
     - *Async:* User & Forum → **Kafka** (`user.registered.v1`, `post.created.v1`) → `PostCreatedConsumerWorker` trong AI Service.
  3. **Nơi triển khai:** Docker Compose 8 containers (`docker-compose.yml`), tích hợp EF Core auto-migration & Database Seeder; Mobile cấu hình `10.0.2.2:5000` (Emulator) / LAN & Firebase Cloud.
  4. **Quyết định kiến trúc & Trade-off:**
     - **Chọn Microservices + Database-per-Service + YARP Gateway** để tách biệt ranh giới dữ liệu, phát triển song song 5 module và tránh khóa chéo DB — **Đánh đổi:** Tăng chi phí vận hành container, phải đồng bộ hồ sơ qua gRPC/Kafka thay vì SQL `JOIN`, và cần cơ chế fallback khi một service phụ tạm gián đoạn.
- **Minh chứng bắt buộc:** Sơ đồ khối kiến trúc (nhìn hiểu trong 15s, dựa trên `README.md`), kèm hộp `DEPLOYMENT` và `QUYẾT ĐỊNH KIẾN TRÚC`.

---

### SLIDE 10 / TECHNOLOGY STACK *(Thời lượng: 40s)*
- **Câu hỏi hội đồng:** Hệ thống dùng những công nghệ nào, mỗi thứ để làm gì?
- **Thông điệp kết luận:** **Tech Stack đồng bộ từ Backend .NET 10, Kafka/gRPC đến React Web và Flutter Mobile, mỗi công nghệ gắn một vai trò rõ ràng**

| Nhóm | Công nghệ thực tế | Dùng để làm gì & Lý do chọn |
| :--- | :--- | :--- |
| **Web FE** | React 19 • TypeScript • Tailwind CSS • Zustand • React Query • Axios • Zod | Giao diện Web Portal (`/app/*` & `/admin/*`); Axios Interceptor tự gắn JWT & Silent Refresh 401; Zod + React Hook Form validate an toàn. |
| **Mobile** | Flutter (Dart 3.13+) • `flutter_bloc` (Cubit) • `flutter_riverpod` • `go_router` • `signalr_netcore` • Firebase (6 SDK) | Ứng dụng Android Clean Architecture; kết nối SignalR, lưu token bảo mật (`flutter_secure_storage`), tích hợp 6 dịch vụ Firebase BaaS. |
| **Backend** | .NET 10 (ASP.NET Core) • YARP Reverse Proxy • EF Core 10 • SQL Server 2022 • gRPC Protobuf • SignalR | 5 Microservices hiệu năng cao; YARP gom CORS & điều hướng cổng `5000`; gRPC truyền dữ liệu nội bộ Chat → User nhanh hơn REST JSON. |
| **AI / Event** | Apache Kafka (KRaft) • Google Gemini API (`v1beta`) • Hybrid RAG (Lexical + Intent Scoring) | Kafka truyền bất đồng bộ `post.created.v1`; Hybrid RAG truy xuất 12 tài liệu FPTU + bài Forum trả lời có `Citations` & gợi ý Mentor. |
| **Dev & QA** | Git / GitHub • xUnit • Puppeteer-core • Flutter `widget_test` • ARTEMIS • PowerShell SHA-256 | Chạy 205 Backend tests, E2E trình duyệt Chrome thật trên Web, Responsive Widget Tests trên Mobile & khóa hợp đồng Proto/Kafka bằng SHA-256. |
| **DevOps** | Docker • Docker Compose (8 containers) | Đóng gói đồng bộ 5 APIs, Gateway, SQL Server, Kafka giúp dựng môi trường test/demo bằng 1 câu lệnh. |

---

### SLIDE 11 / UI/UX DESIGN *(Thời lượng: 40s)*
- **Câu hỏi hội đồng:** Giao diện được tổ chức thế nào và có nhất quán, dễ dùng không?
- **Thông điệp kết luận:** **Tổ chức giao diện nhất quán trên cả Web Portal & Mobile App với điều hướng phân quyền và xử lý trọn vẹn 4 trạng thái UI**
- **Nội dung (4 ý):**
  1. **Sitemap & Navigation (`UI-xx`):**
     - **Web (`routes.tsx`):** Auth (`/`, `/auth/login`, `/auth/forgot-password`), User Portal (`/app/home` `UI-W01`, `/app/alumni` `UI-W02`, `/app/chat` `UI-W03`, `/app/profile` `UI-W04`), Admin Portal (`/admin/dashboard` `UI-A01`, `/admin/users` `UI-A02`, `/admin/reports` `UI-A03`, `/admin/posts`, `/admin/communities`, `/admin/analytics`, `/admin/settings`).
     - **Mobile:** Adaptive `NavigationBar` (`< 600dp`) ↔ `NavigationRail` (`>= 600dp`) điều hướng qua Home (`UI-M01`), Tạo/Chi tiết bài (`UI-M02`), Danh bạ & Jobs (`UI-M03`), Chat (`UI-M04`), AI Assistant (`UI-M05`), Hồ sơ & Inbox (`UI-M06`).
  2. **Wireframe → Hi-Fi → Final UI:** Đồng bộ bộ nhận diện FPT (`#F37021`), typography sạch và chuẩn hóa component tái sử dụng.
  3. **4 Trạng thái UI:** *Empty* (trạng thái trống kèm CTA), *Loading* (Skeleton/Spinner), *Error* (hiển thị ProblemDetails rõ ràng), *Success* (cập nhật Feed / Toast / Silent Refresh).
  4. **Cải tiến sau góp ý & Audit:**
     - *Web:* Thay thế hoàn toàn mock data tại `PostFeed`/`RightSidebar` bằng API thật, mở khóa `/app/*` cho sinh viên, gỡ banner demo login (`3eb4506`).
     - *Mobile:* Khắc phục 2 lỗi `RenderFlex overflow` trên màn hình `320x568` & phóng chữ `2.0x`; bổ sung **Silent Refresh** ở `PostDetailScreen` giữ nguyên vị trí cuộn.
- **Minh chứng bắt buộc:** Sitemap phân nhánh Web/Mobile + cụm 4 trạng thái UI + box `SAU GÓP Ý / AUDIT`.

---

### SLIDE 12 / SEQUENCE DIAGRAM *(TÙY CHỌN — Thời lượng: 35s)*
- **Câu hỏi hội đồng:** Khi người dùng bấm nút, bên trong hệ thống diễn ra những gì?
- **Thông điệp kết luận:** **Minh họa tương tác xuyên tầng: Đăng bài diễn đàn → SignalR Broadcast → Kafka → AI RAG (kèm nhánh lỗi BR-02)**
- **Nội dung (4 ý):**
  1. **Tương tác tiêu biểu:** Luồng đăng bài viết mới (`POST /api/v1/posts`) từ UI tới DB và đồng bộ sự kiện sang AI Service.
  2. **Lifeline khớp Class Design (Slide 13):** `Alumni` → `PostComposer (UI)` → `Gateway (:5000)` → `PostsController` → `PostService` → `IPostRepository` → `FAlumni_Forum_Db` / `ForumHub (SignalR)` / `KafkaProducerService` → `PostCreatedConsumerWorker (AiService)`.
  3. **Nhánh ngoại lệ (`alt`) theo Business Rule:**
     - `alt [Title < 5 hoặc Content < 10 ký tự · BR-02]`: Trả `400 Bad Request` (`ValidationProblemDetails`), UI giữ nguyên input và hiển thị cảnh báo.
     - `alt [Chưa đăng nhập hoặc Suspended · BR-01]`: Trả `401 / 403`.
     - `else [Hợp lệ]`: `CreatePostAsync()` lưu DB → `201 Created`, đẩy `ReceiveNewPost` qua SignalR & phát `PostCreatedEventV1` lên Kafka để `AiService` gọi `IngestForumPostAsync()`.
  4. **Tên hàm/API thật:** `POST /api/v1/posts`, `CreatePostAsync()`, `AddAsync()`, `SendAsync("ReceiveNewPost")`, `PublishPostCreatedAsync()`.
- **Minh chứng bắt buộc:** Sơ đồ Sequence Diagram chuẩn UML có khối `alt` ngoại lệ và khối bất đồng bộ Kafka.

---

### SLIDE 13 / CLASS DESIGN *(Thời lượng: 30s)*
- **Câu hỏi hội đồng:** Code chia trách nhiệm ra sao, business rule nằm ở đâu?
- **Thông điệp kết luận:** **Phân tầng rõ trách nhiệm (Controller – Service – Repository – Entity) qua DI; toàn bộ Business Rule đặt ở Service/Domain, không đặt trong UI**
- **Nội dung (4 ý):**
  1. **Các lớp cốt lõi theo 4 tầng (khớp tên trong mã nguồn):**
     - *Controller (`*.Api`):* `AuthController`, `PostsController`, `ChatController`, `AiChatController`, `MentorsController`, `DashboardController`, `ModerationReportsController`, `UsersModerationController`.
     - *Service (`*.Services`):* `AuthService`, `PostService`, `ChatAppService`, `AiChatService`, `MentorRecommendationService`, `DashboardService`, `ModerationReportService`, `UserModerationService`, `AuditLogService`, `DataExportService`.
     - *HttpClient / Broker:* `UserServiceClient` (`IUserServiceClient`), `KafkaProducerService`, `UserProfileGrpcService`.
     - *Repository & Entity (`*.Repositories`):* `IUserRepository`, `IPostRepository`, `IChatRepository`, `IRagRepository`, `IAuditLogRepository`, `IModerationReportRepository`, `ISystemAnnouncementRepository`, `AdminServiceDbContext`, ...
  2. **Quan hệ & Dependency:** Controller chỉ phụ thuộc `I*Service`; Service chứa nghiệp vụ & điều phối Repository / Client / `IAuditLogService`; Repository phụ thuộc `DbContext`.
  3. **Domain Entity ≠ Database Table:** Các rule như kiểm tra trạng thái `Suspended`, ngưỡng `>= 2` thành viên chat, tính điểm Match Score Mentor (`+50` Major, `+15` Skill, `+20` CareerGoal), ném `NotFoundException`/`ForbiddenException` nằm ở tầng Service/Entity, không nằm ở UI.
  4. **Đối chiếu Client:** Web tách `services/*` & `authStore` khỏi `pages/*`; Mobile tách `Cubit` & `Repository`/`FirebaseService` khỏi `Screens`.
- **Minh chứng bắt buộc:** Sơ đồ Class Diagram chuẩn UML (minh họa cụm `AdminService` + `ForumService` hoặc lớp đại diện xuyên suốt 4 tầng) khớp 100% tên class thật.

---

### SLIDE 14 / DATABASE DESIGN 1/2 — CONCEPTUAL *(TÙY CHỌN — Thời lượng: 25s)*
- **Câu hỏi hội đồng:** Dữ liệu nghiệp vụ gồm những thực thể nào, quan hệ ra sao?
- **Thông điệp kết luận:** **Mô hình dữ liệu khái niệm gồm 5 miền thực thể độc lập, liên kết logic qua mã định danh thay vì khóa ngoại liên DB**
- **Nội dung (4 ý):**
  1. **5 Cụm Business Entity chính:**
     - *User Domain:* `User`, `UserProfile`, `BlockedUser`
     - *Forum Domain:* `Category`, `Post`, `Comment`, `Reaction`, `Bookmark`, `Event`
     - *Chat Domain:* `Conversation`, `ConversationParticipant`, `Message`
     - *AI Domain:* `RagDocument`, `RagChunk`, `AiConversation`, `AiMessage`, `MentorProfile`
     - *Admin Domain:* `ModerationReport`, `AuditLog`, `SystemAnnouncement`, `SystemSetting`
  2. **Quan hệ & Cardinality chính:**
     - `User` `1 — 1` `UserProfile`; `User` `1 — 0..*` `BlockedUser`
     - `Post` `1 — 0..*` `Comment` (hỗ trợ `ParentCommentId` lồng nhau), `0..*` `Reaction`, `0..*` `Bookmark`
     - `Conversation` `1 — 2..*` `ConversationParticipant` & `1 — 0..*` `Message`
     - `RagDocument` `1 — 1..*` `RagChunk`; `AiConversation` `1 — 0..*` `AiMessage`
  3. **Business Rules chi phối quan hệ:** `BR-04` (phòng chat tối thiểu 2 người), `BR-06` (`AuditLog` độc lập với `SystemAnnouncement` để bảo toàn dấu vết khi xóa thông báo).
  4. **Đối chiếu với Actor:** Student/Alumni sở hữu dữ liệu User/Post/Chat/AI; Admin quản trị Report/Announcement/AuditLog/Setting.
- **Minh chứng bắt buộc:** Conceptual ERD với ký hiệu Cardinality (`1`, `0..*`, `2..*`), tuyệt đối không ghi kiểu dữ liệu SQL vật lý.

---

### SLIDE 15 / DATABASE DESIGN 2/2 — LOGICAL → PHYSICAL *(Thời lượng: 30s)*
- **Câu hỏi hội đồng:** Dữ liệu được hiện thực đúng và có ràng buộc bảo vệ chưa?
- **Thông điệp kết luận:** **Hiện thực 5 CSDL SQL Server cô lập hoàn toàn (`Zero Cross-DB FK`) với 24 bảng, Index tối ưu và Migration EF Core**
- **Nội dung (4 ý):**
  1. **Logical → Physical trên SQL Server 2022:** 5 Database cách ly (`FAlumni_User_Db`, `FAlumni_Chat_Db`, `FAlumni_Forum_Db`, `FAlumni_AiService_Db`, `FAlumni_Admin_Db`).
  2. **Bảng, Cột, Kiểu dữ liệu mẫu (trích từ Migration):**
     - `Users` (`Id uniqueidentifier PK`, `Email nvarchar(150) UNIQUE`, `Role nvarchar(30)`, `Status nvarchar(40)`).
     - `Posts` (`Id uniqueidentifier PK`, `AuthorId uniqueidentifier`, `Title nvarchar(300)`, `PostType nvarchar(50)`, `IsAnswered bit`).
     - `ModerationReports` (`Id uniqueidentifier PK`, `TargetType nvarchar(50)`, `TargetId uniqueidentifier`, `Status nvarchar(50)`).
     - `AuditLogs` (`Id uniqueidentifier PK`, `AdminUserId uniqueidentifier`, `Action nvarchar(100)`, `CreatedAtUtc datetime2`).
  3. **Ràng buộc (Constraints) & Indexes:**
     - *Cascade Delete nội bộ:* `UserProfiles`, `RefreshTokens`, `PostImages`, `Comments`, `Messages`, `RagChunks`, `AiMessages`.
     - *Unique Indexes:* `Users(Email)`, `Categories(Slug)`, `ConversationParticipants(ConversationId, UserId)`, `BlockedUsers(BlockerUserId, BlockedTargetUserId)`.
     - *Query Indexes:* `AuditLogs(Action, TargetEntity)`, `AuditLogs(CreatedAtUtc)`, `ModerationReports(Status, CreatedAtUtc)`, `Posts(PostType, Major, Campus)`.
  4. **Minh chứng:** Bộ file EF Core Migration thật (`InitialCreate`, `AddForumEntities`, `AddAiEntities`, `AddAdminEntities`) tự động chạy khi khởi động Docker.
- **Minh chứng bắt buộc:** Hình 5 cụm bảng Physical + trích dẫn file Migration thật (`20261101101922_AddAdminEntities.cs`, `20261103005740_AddAiEntities.cs`, ...).

---

### SLIDE 16 / TRACEABILITY *(Thời lượng: 30s)*
- **Câu hỏi hội đồng:** Các bản thiết kế có khớp nhau và có được dùng thật không?
- **Thông điệp kết luận:** **Ma trận truy vết khép kín chứng minh mọi tính năng đều xuyên suốt từ UI → Class → DB → Test**

| Nhóm tính năng | Màn hình (`UI-xx`) | Class / Service | Entity / DB | Tests minh chứng |
| :--- | :--- | :--- | :--- | :--- |
| **F1 · Xác thực & Hồ sơ (`US-01..11`)** | `LoginPage`, `ProfilePage` / `login_screen` (`UI-W04`, `UI-M06`) | `authService` / `AuthController`, `AuthService` | `Users`, `UserProfiles`, `RefreshTokens` | `UserService.Tests` + `ART-01..03` |
| **F2 · Diễn đàn & AI RAG (`FO-01..16`)** | `HomePage` (`PostComposer`, `PostFeed`), `home_screen` (`UI-W01`, `UI-M01/02`) | `postsService` / `PostsController`, `PostService`, `RagIngestionService` | `Posts`, `Comments`, `Reactions`, `RagDocuments`, `RagChunks` | `ForumService.Tests` & `AiService.Tests` + Web E2E `POST /posts` (`201`) |
| **F3 · AI Mentor & Chat (`AI-01..04`, `CH-01..10`)** | `RightSidebar`, `ChatPage`, `directory_screen`, `ai_assistant_screen` (`UI-W03`, `UI-M04/05`) | `aiService`, `chatService` / `MentorsController`, `MentorRecommendationService`, `ChatAppService` | `MentorProfiles`, `AiConversations`, `Conversations`, `Messages` | `ChatService.Tests` + Web E2E `recommend` & `/hubs/chat/negotiate` (`200`) |
| **F4 · Kiểm duyệt, Khóa User & Export (`AD-01..09`)** | `DashboardPage`, `ReportsManagementPage`, `UsersManagementPage` (`UI-A01..03`) | `adminService` / `DashboardController`, `ModerationReportsController`, `UsersModerationController` | `ModerationReports`, `AuditLogs`, `SystemAnnouncements`, `Users` | `AdminService.Tests` + Web E2E `/admin/dashboard/summary`, `/admin/users` (`200`) |

- **Dải chốt Design:** `TRUY VẾT: Feature → Screen (UI-xx) → Service / Class → Isolated DB → Test Cases (205 BE + 16 Web E2E + 42 Mobile ARTEMIS)`.

---

## PHẦN 3: DEMO — THEO TỪNG WORKFLOW (`≈ 6.5 phút`)
> *Quy tắc: Chạy trực tiếp end-to-end thành một câu chuyện liền mạch; kết quả workflow trước làm đầu vào cho workflow sau.*

### SLIDE 17 / DEMO PLAN *(Thời lượng: 30s)*
- **Câu hỏi hội đồng:** Hội đồng sắp xem những workflow nào, theo thứ tự nào, vì sao?
- **Thông điệp kết luận:** **Storyboard Demo 3 workflow liên hoàn qua Docker Gateway (`:5000`), ghép nối trọn vẹn Web, Mobile, Kafka, AI và Admin**
- **Nội dung (3 cột Storyboard + Chuẩn bị):**
  1. **01 · WF-01 (`nguyenvana@fpt.edu.vn` — Alumni K17):** Đăng bài chia sẻ kinh nghiệm phỏng vấn/Microservices lên Forum (`UI-W01`) → *Kết quả:* Bài mới lưu DB, đẩy SignalR lên Feed và tự động nạp qua Kafka vào AI RAG.
  2. **02 · WF-02 (`tranthib@fpt.edu.vn` — Student K19):** Nhận gợi ý Mentor AI trên Web/Mobile (`95% Match`) → Bấm *"Kết nối"* tạo phòng chat với Alumni/Mentor và nhắn tin trực tiếp qua SignalR (`UI-W03 / UI-M04`).
  3. **03 · WF-03 (`admin@fpt.edu.vn` — Admin):** Xem KPI Dashboard (`UI-A01`), duyệt báo cáo vi phạm spam đối với `levanc@fpt.edu.vn` (`UI-A03`), khóa tài khoản (`UI-A02`) → Kiểm chứng `levanc` đăng nhập bị chặn `403`, xem `AuditLog` & xuất file CSV.
- **Chuẩn bị:** 8 Docker containers chạy sẵn • Seed data cố định (`DatabaseSeeder`, `ForumDatabaseSeeder`, `AiDatabaseSeeder`, `AdminDatabaseSeeder`) với mật khẩu chung `Password@123` • Video & Screenshots dự phòng • Phân công người thao tác ≠ người thuyết minh.

---

### SLIDE 18 / DEMO WF-01 · ALUMNI *(Thời lượng: ≈ 2 phút)*
- **Câu hỏi hội đồng:** WF-01 có chạy thật, đúng nghiệp vụ, và xử lý lỗi đúng không?
- **Thông điệp kết luận:** **WF-01 · Chia sẻ bài viết Forum & Tự động nạp tri thức AI RAG qua Kafka**
- **Nội dung (4 ý):**
  1. **Scenario:** Cựu sinh viên K17 (`nguyenvana@fpt.edu.vn`) đăng bài chia sẻ kinh nghiệm phỏng vấn & kiến trúc Backend cho sinh viên FPTU.
  2. **Happy Path (5 bước):** `Trigger: cần chia sẻ kinh nghiệm` → `Mở PostComposer trên UI-W01` → `Kiểm tra hợp lệ BR-02` → `POST /api/v1/posts (201 Created)` → `PostFeed cập nhật tức thì (SignalR) + Kafka post.created.v1 nạp vào RagChunks`.
  3. **Exception Path (Cố ý vi phạm `BR-02`):** Nhập nội dung ngắn `< 10` ký tự và bấm **"Đăng bài"** → Hệ thống chặn lại tại Frontend & Backend validation (`400 Bad Request`), không tạo bài viết rác.
  4. **Minh chứng:** Bài viết mới xuất hiện kèm ID thật trên Feed (`02_user_home_feed.png`), log Docker `falumni-forum-service` & `falumni-ai-service` ghi nhận đã publish/consume sự kiện thành công.

---

### SLIDE 19 / DEMO WF-02 · STUDENT *(Thời lượng: ≈ 2 phút)*
- **Câu hỏi hội đồng:** WF-02 có chạy thật, đúng nghiệp vụ, và xử lý lỗi đúng không?
- **Thông điệp kết luận:** **WF-02 · AI Gợi ý Mentor theo Match Score & Kết nối Chat Real-time qua SignalR**
- **Nội dung (4 ý):**
  1. **Scenario:** Sinh viên (`tranthib@fpt.edu.vn`) hỏi AI RAG hoặc tìm kiếm cựu sinh viên cố vấn kỹ năng `C#`, `.NET`, `Microservices`.
  2. **Happy Path (5 bước):** `Trigger: cần Mentor chuyên ngành` → `POST /api/v1/ai/mentors/recommend` → `Hiển thị Mentor Match Score (95%) tại RightSidebar / Directory` → `Bấm "Kết nối" (POST /api/v1/chats/conversations, kiểm tra BR-04)` → `Mở /app/chat (UI-W03) bắt tay /hubs/chat/negotiate & gửi tin nhắn tức thì`.
  3. **Exception Path (Cố ý vi phạm `BR-04`):** Gửi yêu cầu tạo hội thoại thiếu thành viên (`< 2` người phân biệt) → Chat Service trả `400 Bad Request` (*"A conversation requires at least 2 distinct participants"*); hoặc chặn nhắn tin với user đã bị block.
  4. **Minh chứng:** API trả về `matchScore: 95`, tin nhắn hiển thị song song thời gian thực giữa 2 phiên trình duyệt / Web & Mobile (`03_chat_page.png` & `Step_20_TroLy_AIMentor_Chat.png`).

---

### SLIDE 20 / DEMO WF-03 · ADMIN *(Thời lượng: ≈ 2 phút)*
- **Câu hỏi hội đồng:** WF-03 có chạy thật, đúng nghiệp vụ, và xử lý lỗi đúng không?
- **Thông điệp kết luận:** **WF-03 · Kiểm duyệt Báo cáo, Khóa Tài khoản vi phạm, Ghi Audit Log & Xuất CSV**
- **Nội dung (4 ý):**
  1. **Scenario:** Quản trị viên (`admin@fpt.edu.vn`) kiểm tra số liệu vận hành, xử lý báo cáo vi phạm đang chờ duyệt (`Pending`) và xuất báo cáo kiểm toán.
  2. **Happy Path (5 bước):** `Trigger: có báo cáo Pending` → `Xem Dashboard KPI (UI-A01)` → `Duyệt báo cáo (POST /api/v1/admin/moderation/reports/{id}/resolve)` → `Khóa user vi phạm (POST /api/v1/admin/users/{id}/suspend · BR-05/06)` → `Kiểm tra AuditLog & tải file CSV (GET /api/v1/admin/export/audit-logs)`.
  3. **Exception Path (Cố ý vi phạm `BR-01` & `BR-05`):**
     - Tài khoản bị khóa (`levanc@fpt.edu.vn`) thử đăng nhập lại → Bị chặn **`403 Forbidden`** kèm lý do vi phạm.
     - Tài khoản `USER` thường thử truy cập `/admin/*` → `RoleGuard` & Policy `RequireAdminRole` từ chối quyền truy cập.
  4. **Minh chứng:** Ảnh `01_admin_dashboard.png` (`TotalUsers=4`, `PendingReports=2`), `04_admin_users.png`, bản ghi `AuditLog` mới trong DB và file `falumni-audit-logs-*.csv`.

---

## PHẦN 4: PROOF (`≈ 2.5 phút`)

### SLIDE 21 / TESTING & VERIFICATION *(Thời lượng: 50s)*
- **Câu hỏi hội đồng:** Làm sao nhóm biết hệ thống chạy đúng?
- **Thông điệp kết luận:** **Kiểm thử đa tầng từ 205 Backend Tests, Puppeteer Web E2E đến Mobile ARTEMIS & Widget Tests**
- **Nội dung (3 thẻ KPI + Bảng kết quả):**
  - *KPI Cards:* **`205 / 205`** Backend Tests Passed | **`16 / 16`** Web Gateway E2E Calls `2xx` | **`42 / 42`** Mobile ARTEMIS Lượt Passed.

| Cấp độ kiểm thử | Phạm vi & Công cụ (Evidence) | Kết quả thực tế |
| :--- | :--- | :--- |
| **Backend Unit & Integration** | `dotnet test` trên 6 projects (`User`, `Chat`, `Forum`, `Admin`, `AI`, `Gateway`) + xác minh SHA-256 contract (`verify-contracts.ps1`) | **205 / 205 PASSED** (`README.md`) & **100% SHA-256 khớp** |
| **Web Live Browser E2E** | Script `scripts/e2e-web-api-live-test.mjs` (Puppeteer Chrome) tương tác thật UI qua Docker Gateway `:5000` | **16 / 16 requests HTTP 200/201** (0 lỗi 4xx/5xx, 4 screenshots) |
| **Mobile E2E & Widget Tests** | `ARTEMIS` (`14` ca x `3` lượt) + Flutter `test/widget_test.dart` (đa viewport `320x568`–`840x600`, font scale `1.3x`/`2.0x`) | **42 / 42 lượt ARTEMIS Passed** & **Widget Tests Passed** |
| **Exploratory & Bug Fixing** | Phiên test thực tế Android 14 (25 phút) & Integration audit toàn hệ thống | Đã phát hiện & sửa triệt để `RenderFlex overflow`, lỗi `502` Kestrel Admin, EF Concurrency `400`, mock cứng trên Web |

---

### SLIDE 22 / ACHIEVEMENTS *(Thời lượng: 40s)*
- **Câu hỏi hội đồng:** Nhóm đã đạt được những gì đã cam kết ở phần đầu?
- **Thông điệp kết luận:** **Hoàn thiện trọn vẹn bộ MVP đa nền tảng, đồng bộ 3 repository trên GitHub và chạy ổn định qua Docker**
- **Nội dung (3 khối đối chiếu mục tiêu ↔ minh chứng):**
  - **✓ OBJECTIVE — Kiến trúc Microservices chuẩn phân tán:** Vận hành **5 dịch vụ .NET 10 + YARP API Gateway**, cô lập 5 SQL Server DBs, tích hợp gRPC & Apache Kafka (`github.com/F-Alumni/f-alumni-connect`).
  - **✓ IMPLEMENTED — Sản phẩm thực tế trên 2 nền tảng:**
    - Web Portal (`-F-Alumni---Web`): Build production sạch (`tsc -b && vite build` exit code 0), hoàn thiện phân hệ User (`/app/*`) & Admin (`/admin/*`).
    - Mobile App (`F-Alumni-Mobile`): Hoàn thiện **41/41 Non-Admin User Stories** & **6/6 Firebase Services** (`alumniconnect-295ed`).
  - **✓ VALIDATED — Đã kiểm chứng liên thông thực tế:** Hợp đồng Protobuf/Kafka khớp SHA-256; Web E2E gọi thật 10 endpoint Gateway cốt lõi trả HTTP `200/201`; Mobile đạt `42/42` lượt ARTEMIS.
  - **Chưa đạt / Giới hạn cần cải thiện:** Xem chi tiết 3 giới hạn kỹ thuật tại Slide 23.

---

### SLIDE 23 / LIMITATIONS & NEXT STEPS *(Thời lượng: 25s)*
- **Câu hỏi hội đồng:** Sản phẩm chưa làm được gì, và ảnh hưởng thế nào?
- **Thông điệp kết luận:** **Nhìn nhận thẳng 3 giới hạn kỹ thuật hiện tại kèm hướng xử lý ưu tiên**

| Giới hạn hiện tại | Nhóm | Tác động | Ưu tiên | Next Step (Bước tiếp theo) |
| :--- | :--- | :--- | :---: | :--- |
| **1. Duyệt báo cáo ở Admin chưa tự động bắn Kafka event ẩn/xóa bài sang ForumService** | Architecture / Data | Admin `Resolve` báo cáo xong vẫn cần thao tác tách biệt nếu muốn ẩn bài gốc bên Forum; `UserServiceClient` còn fallback mock khi UserService offline. | **Cao** | Bổ sung Kafka topic `moderation.resolved.v1` để ForumService tự ẩn nội dung vi phạm và loại bỏ fallback mock ở Production. |
| **2. AI RAG dùng Lexical/Intent Scoring & phụ thuộc Gemini Quota** | AI / Technology | Chưa có Vector Embedding nên có thể sót từ đồng nghĩa; khi hết quota Gemini sẽ fallback về ghép trích đoạn cục bộ. | **Trung bình** | Tích hợp Vector DB (`pgvector` / Qdrant) nâng cấp tìm kiếm ngữ nghĩa (Semantic Search) cho `RagChunks`. |
| **3. Chưa thực hiện Load/Stress Test quy mô lớn & thiếu WebRTC Call** | Testing / Product | Chưa đo được ngưỡng RPS/Latency khi hàng nghìn user truy cập đồng thời; Mentorship mới hỗ trợ Chat & link Calendar. | **Trung bình** | Chạy k6 / JMeter benchmark qua YARP Gateway và tích hợp gọi Video/Audio 1-1 qua WebRTC. |

---

### SLIDE 24 / CONCLUSION *(Thời lượng: 30s — Giữ trên màn hình khi Q&A)*
- **Câu hỏi hội đồng:** Hội đồng nên nhớ điều gì sau buổi bảo vệ?
- **Thông điệp kết luận:** **FPT Alumni Connect — Hệ sinh thái kết nối cộng đồng FPTU đã chứng minh tính đúng đắn từ Thiết kế đến Kiểm thử thực tế**
- **3 Thông điệp chốt (Bố cục 3 cột lớn):**
  1. **PROBLEM SOLVED:** Hợp nhất kênh giao tiếp FPTU trên 5 cơ sở, lưu trữ tri thức có kiểm chứng (Forum + AI RAG), kết nối Mentor theo điểm tương thích và chuẩn hóa kiểm duyệt `AuditLogs`.
  2. **WORKING PRODUCT:** 5 Microservices .NET 10 + YARP Gateway + Kafka + gRPC + Web React + Flutter Mobile (6 Firebase Services) vận hành mượt mà qua `WF-01..03`.
  3. **VERIFIED RESULT:** **205/205** Backend Tests • **16/16** Web Live E2E Gateway Calls • **42/42** Mobile ARTEMIS Tests • **100%** toàn vẹn hợp đồng SHA-256.
- **Links Repositories & Cảm ơn:**
  - Backend Monorepo: `https://github.com/F-Alumni/f-alumni-connect`
  - Web Frontend: `https://github.com/F-Alumni/-F-Alumni---Web`
  - Mobile App: `https://github.com/F-Alumni/F-Alumni-Mobile`
  - *Trân trọng cảm ơn Hội đồng và Quý Thầy/Cô đã lắng nghe!*

---

## PHỤ LỤC (`Slide 25 · Appendix / Backup — 0s · Chỉ mở khi Q&A`)

- **A1 · Activity & Màn hình phụ *(Khải & Đỗ Trọng Tín)*:** Màn hình Quên mật khẩu/OTP, Quyền riêng tư & Chặn người dùng (`US-09, US-10`), Bộ lọc `ChoiceChip` & `JobsScreen`, trang `SettingsPage` & `CommunitiesManagementPage`.
- **A2 · Đặc tả Use Case & 50 User Stories *(Trương Chí Trung & Đỗ Trọng Tín)*:** Bảng chi tiết 41/41 Non-Admin User Stories (`F-Alumni-Mobile/docs/USER_STORIES_AND_RESPONSIVE_AUDIT.md`) + 9 Admin User Stories (`AD-01..09`), Pre/Post-condition & Alternative flows.
- **A3 · Test Case, Logs & AI Code Review *(Đỗ Trọng Tín & Toàn nhóm)*:** Chi tiết 205 Backend tests, log Puppeteer E2E (`-F-Alumni---Web/scripts/e2e-web-api-live-test.mjs`), 14 ca ARTEMIS (`ART-01..14`), Flutter `test/widget_test.dart`, bảng 6 phát hiện AI Code Review (`F-01..F-06`).
- **A4 · Physical Schema & Migrations đầy đủ *(Trung, Vinh, Nguyễn Quang Tâm)*:** Toàn bộ 24 bảng, cột, khóa, index và Migration của 5 CSDL cô lập (`docs/ERD.md` ở từng service).
- **A5 · Application & Sequence Design chi tiết *(Trung, Vinh, Nguyễn Quang Tâm)*:** Bộ sơ đồ Draw.io (`docs/architecture/BE1_User_And_Chat/*.drawio`), thuật toán Hybrid RAG & Mentor Matching (`+50/+15/+20`), Protobuf `user_profile.proto` & Kafka `EventEnvelope`.
- **A6 · Video Demo Dự Phòng & Kịch bản Reset Data *(Toàn nhóm)*:** Bộ ảnh `evidence/` & video quay `WF-01..03` phòng trường hợp lỗi mạng khi demo trực tiếp.

---

## ✅ QUALITY GATE CHECKLIST (RÀ SOÁT TRƯỚC KHI BẢO VỆ)

- [x] **PROBLEM:** Vấn đề rõ ràng, gắn đối tượng ảnh hưởng *(bổ sung thêm số liệu khảo sát nhóm nếu có)*.
- [x] **FEATURES:** 5 nhóm tính năng gắn với pain point, chọn đúng 3 workflow lõi (`WF-01..03`).
- [x] **DESIGN:** Business Flow, State Machine, Architecture, UI/UX, Class, ERD khớp nhau và khớp mã nguồn thật.
- [x] **TRACEABILITY:** Đủ đường nối `Feature → Screen (UI-xx) → Class/Service → DB Entity → Test`.
- [x] **DEMO WORKFLOW:** 3 workflow ghép thành một câu chuyện liên hoàn (Alumni đăng bài → Student AI Mentor + Chat → Admin duyệt & khóa user), có kịch bản ngoại lệ `BR-01..06`.
- [x] **DEMO DATA:** Đã có seed accounts (`Password@123`), Docker Compose reset sạch, cần chuẩn bị sẵn video quay dự phòng.
- [x] **TESTS:** Đủ số liệu `205/205` Backend, `16/16` Web E2E, `42/42` Mobile ARTEMIS & `SHA-256`.
- [x] **OUTCOME:** Thành quả & giới hạn dựa trên code thực tế, không tự điền số liệu ảo.

---

### 💡 Prompt mẫu khi đưa file này cho AI tạo Slide (Gamma / Canva / PowerPoint / Claude)
> *"Hãy đọc toàn bộ file `SLIDES_PRESENTATION_V11.md` và tạo thành bộ thuyết trình gồm đúng 24 slide chính + 1 nhóm slide Phụ lục (`A1–A6`). Tuân thủ chính xác tiêu đề kết luận của từng slide, giữ bố cục bảng biểu/card/sơ đồ theo mục 'Gợi ý bố cục' của từng slide, sử dụng tông màu chủ đạo Cam FPT (#F37021) phối Slate/Trắng chuyên nghiệp, súc tích để thuyết trình trong 18–20 phút."*
