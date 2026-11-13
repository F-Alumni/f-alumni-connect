# 📋 BÁO CÁO TOÀN DIỆN AUDIT HỆ THỐNG API - GIAO DIỆN WEB & KẾ HOẠCH PHÁT TRIỂN UI CHI TIẾT
> **Dự án**: FPT Alumni Connect Platform (PRN232 — Microservices Architecture)  
> **Thư mục tài liệu**: `C:\Use\University\PRN232\ASM\docs\UI_DEVELOPMENT_PLAN_AND_API_AUDIT.md`  
> **Thời gian cập nhật**: 13/11/2026  
> **Cấu trúc phân rã công việc**: **4 Phase thực thi tuần tự**, mỗi Phase được đảm nhiệm bởi **3 Agents chuyên biệt (2 Agents Implement + 1 Agent Tester / QA)**.  
> **Mục tiêu**: Cung cấp tài liệu chỉ dẫn kỹ thuật cực kỳ chi tiết, chuẩn xác từng dòng lệnh, từng file path, từng interface để các AI Agents (kể cả model cấu hình thấp) có thể đọc hiểu và thực thi ngay lập tức mà không bị hallucination, không tự suy đoán ý hay làm sai lệch kiến trúc.

---

## MỤC LỤC
1. [TỔNG QUAN HIỆN TRẠNG TOÀN DỰ ÁN](#1-tổng-quan-hiện-trạng-toàn-dự-án)
2. [MA TRẬN KIỂM KÊ TOÀN BỘ BACKEND API (6 MICROSERVICES + GATEWAY)](#2-ma-trận-kiểm-kê-toàn-bộ-backend-api-6-microservices--gateway)
3. [KIỂM TOÁN ĐỐI CHIẾU GIAO DIỆN FRONTEND WEB HIỆN TẠI (COVERAGE MATRIX)](#3-kiểm-toán-đối-chiếu-giao-diện-frontend-web-hiện-tại-coverage-matrix)
4. [TỔNG HỢP 5 LỖ HỔNG & ĐIỂM NGHẼN CẦN GIẢI QUYẾT (GAP ANALYSIS)](#4-tổng-hợp-5-lỗ-hổng--điểm-nghẽn-cần-giải-quyết-gap-analysis)
5. [MÔ HÌNH PHÂN CÔNG 3 AGENTS TRONG MỖI PHASE](#5-mô-hình-phân-công-3-agents-trong-mỗi-phase)
6. [KẾ HOẠCH CHI TIẾT 4 PHASE TRIỂN KHAI CHO AI AGENTS](#6-kế-hoạch-chi-tiết-4-phase-triển-khai-cho-ai-agents)
   - 🔹 [PHASE 1: Quản Trị Hệ Thống & Phân Quyền Router (Admin Portal & Routing Governance)](#phase-1-quản-trị-hệ-thống--phân-quyền-router-admin-portal--routing-governance)
   - 🔹 [PHASE 2: Xác Thực Tài Khoản & Mạng Lưới Cựu Sinh Viên (Auth Lifecycle & Alumni Directory)](#phase-2-xác-thực-tài-khoản--mạng-lưới-cựu-sinh-viên-auth-lifecycle--alumni-directory)
   - 🔹 [PHASE 3: Diễn Đàn Thảo Luận, Deep Linking & Sự Kiện (Forum Discussions, Post Detail & Events)](#phase-3-diễn-đàn-thảo-luận-deep-linking--sự-kiện-forum-discussions-post-detail--events)
   - 🔹 [PHASE 4: Trí Tuệ Nhân Tạo RAG, Mentor Matching Hub & Đồng Bộ Realtime (AI RAG, Mentor Hub & Realtime Sync)](#phase-4-trí-tuệ-nhân-tạo-rag-mentor-matching-hub--đồng-bộ-realtime-ai-rag-mentor-hub--realtime-sync)
7. [BẢNG CHỈ DẪN TRÁNH HALLUCINATION DÀNH CHO AI AGENTS MODEL THẤP](#7-bảng-chỉ-dẫn-tránh-hallucination-dành-cho-ai-agents-model-thấp)
8. [TIÊU CHUẨN NGHIỆM THU & BỘ SCRIPTS VERIFICATION](#8-tiêu-chuẩn-nghiệm-thu--bộ-scripts-verification)

---

## 1. TỔNG QUAN HIỆN TRẠNG TOÀN DỰ ÁN

Hệ thống **FPT Alumni Connect** được thiết kế theo chuẩn doanh nghiệp Microservices với **.NET 10** ở backend và **React 19 / TypeScript / Vite / Tailwind CSS** ở web frontend:

```
[Web Client (React 19: Port 5173)]
        │
        ▼ (HTTP / SignalR WebSockets)
[YARP API Gateway: Port 5000]
   ├──> /api/v1/auth, /api/v1/users         ──> [User Service: Port 5001 & gRPC 50051]
   ├──> /api/v1/chats, /hubs/chat           ──> [Chat Service: Port 5002]
   ├──> /api/v1/posts, /events, /categories ──> [Forum Service: Port 5003]
   ├──> /api/v1/ai, /boost                  ──> [AI Service: Port 5004] (Gemini 2.5 Flash & RAG Boost)
   ├──> /api/v1/admin                       ──> [Admin Service: Port 5005]
   └──> /api/v1/notifications, /hubs/...    ──> [Notification Service: Port 5006]
```

- **Tình trạng mã nguồn**:
  - Root repo (`c:\Use\University\PRN232\ASM`): Đã đồng bộ với `origin/main` (commit mới nhất `d698a2c`).
  - Frontend repo (`c:\Use\University\PRN232\ASM\-F-Alumni---Web`): Đã đồng bộ với `origin/main` (commit `cddda07`).
  - Toàn bộ 205 Unit/Integration tests backend: **100% Passed**.
  - Kiểm thử hợp đồng SHA-256 (Kafka events, gRPC Proto): **100% nguyên bản**.
  - Frontend build: `tsc -b && vite build` **PASS 100% (0 Error, 0 Warning)**.

---

## 2. MA TRẬN KIỂM KÊ TOÀN BỘ BACKEND API (6 MICROSERVICES + GATEWAY)

### 2.1. YARP API Gateway (Cổng 5000)
- Base URL API: `http://localhost:5000/api/v1`
- SignalR Chat Hub: `http://localhost:5000/hubs/chat`
- SignalR Forum Hub: `http://localhost:5000/hubs/forum`
- SignalR Notification Hub: `http://localhost:5000/hubs/notifications`
- AI RAG Boost Diagnostics: `http://localhost:5000/boost` và `http://localhost:5000/api/v1/ai/boost`

### 2.2. User Service (Cổng 5001) - 25 Endpoints
1. `POST /api/v1/auth/register`: Đăng ký tài khoản mới (hỗ trợ STUDENT, ALUMNI).
2. `POST /api/v1/auth/verify-email`: Kích hoạt email bằng token/OTP.
3. `POST /api/v1/auth/login`: Đăng nhập bằng Email/Mật khẩu.
4. `POST /api/v1/auth/google`: Đăng nhập Google SSO qua Firebase ID Token.
5. `POST /api/v1/auth/refresh-token`: Cấp mới JWT Token khi token hết hạn.
6. `POST /api/v1/auth/logout`: Đăng xuất và thu hồi JWT token.
7. `POST /api/v1/auth/change-password`: Đổi mật khẩu tài khoản cá nhân.
8. `POST /api/v1/auth/forgot-password`: Gửi yêu cầu đặt lại mật khẩu.
9. `POST /api/v1/auth/reset-password`: Xác nhận OTP đặt lại mật khẩu mới.
10. `GET /api/v1/users`: Tìm kiếm, lọc và phân trang danh bạ người dùng.
11. `GET /api/v1/users/directory`: Alias tra cứu danh bạ cựu sinh viên & giảng viên.
12. `GET /api/v1/users/me`: Lấy thông tin hồ sơ của người dùng hiện tại.
13. `PUT /api/v1/users/me`: Cập nhật thông tin hồ sơ cá nhân.
14. `PUT /api/v1/users/me/avatar`: Cập nhật link URL ảnh đại diện.
15. `POST /api/v1/users/media/upload`: Tải ảnh lên backend chuyển tiếp sang Cloudinary.
16. `POST /api/v1/users/me/avatar/upload`: Tải file ảnh và cập nhật trực tiếp vào avatar.
17. `PUT /api/v1/users/me/calendar`: Cập nhật link Google Calendar đặt lịch hẹn tư vấn.
18. `PUT /api/v1/users/me/privacy`: Cài đặt bật/tắt hiển thị thông tin riêng tư (Email, Lịch hẹn).
19. `GET /api/v1/users/me/blocks`: Lấy danh sách tài khoản đã bị chặn.
20. `POST /api/v1/users/{id}/block`: Chặn tài khoản theo GUID.
21. `DELETE /api/v1/users/{id}/block`: Bỏ chặn tài khoản.
22. `GET /api/v1/users/{id}`: Xem hồ sơ công khai của tài khoản khác (đã lọc quyền riêng tư).
23. `POST /api/v1/users/{id}/suspend`: Khóa tài khoản thành viên (Admin).
24. `POST /api/v1/users/{id}/unsuspend`: Mở khóa tài khoản thành viên (Admin).
25. `PUT /api/v1/users/{id}/role`: Cập nhật quyền vai trò người dùng (Admin).

### 2.3. Chat Service (Cổng 5002) - 8 Endpoints REST + SignalR
1. `GET /api/v1/chats/conversations`: Lấy danh sách phòng chat gần nhất kèm tin nhắn cuối.
2. `POST /api/v1/chats/conversations`: Mở phòng chat 1-1 hoặc tạo nhóm chat.
3. `GET /api/v1/chats/conversations/{id}`: Xem chi tiết phòng chat.
4. `GET /api/v1/chats/conversations/{id}/messages`: Phân trang lịch sử tin nhắn trong phòng.
5. `POST /api/v1/chats/conversations/{id}/messages`: Gửi tin nhắn qua REST API.
6. `POST /api/v1/chats/conversations/{id}/read`: Đánh dấu đã đọc tất cả tin nhắn trong phòng.
7. `PUT /api/v1/chats/conversations/{id}/settings`: Cài đặt tắt/bật chuông thông báo phòng chat.
8. `GET /api/v1/chats/conversations/{id}/participants/{userId}`: Trạng thái người tham gia.
- **SignalR ChatHub (`/hubs/chat`)**:
  - Client gọi: `JoinConversation`, `LeaveConversation`, `SendMessage`, `SendTypingIndicator`, `MarkConversationRead`.
  - Server phát: `ReceiveMessage`, `UserTyping`, `MessagesRead`.

### 2.4. Forum Service (Cổng 5003) - 23 Endpoints REST + SignalR
1. `GET /api/v1/posts`: Bảng tin cộng đồng (lọc theo campus, major, postType, search, sort, isAnswered).
2. `GET /api/v1/posts/me`: Lấy bài viết do chính mình đăng.
3. `GET /api/v1/posts/{id}`: Xem chi tiết bài viết.
4. `POST /api/v1/posts`: Đăng bài viết mới (tự kích hoạt Kafka nạp AI RAG).
5. `PUT /api/v1/posts/{id}`: Sửa bài viết.
6. `DELETE /api/v1/posts/{id}`: Xóa bài viết.
7. `PATCH /api/v1/posts/{id}/answered`: Đánh dấu câu hỏi đã tìm được câu trả lời.
8. `POST /api/v1/posts/{postId}/react`: Thả hoặc hủy Like bài viết (toggle).
9. `POST /api/v1/posts/{postId}/bookmark`: Lưu hoặc hủy lưu bài viết (toggle).
10. `GET /api/v1/bookmarks/me`: Xem danh sách bài viết đã lưu.
11. `GET /api/v1/posts/{postId}/comments`: Danh sách bình luận & phản hồi lồng nhau.
12. `POST /api/v1/posts/{postId}/comments`: Thêm bình luận hoặc trả lời bình luận.
13. `PUT /api/v1/comments/{id}`: Sửa bình luận.
14. `DELETE /api/v1/comments/{id}`: Xóa bình luận.
15. `POST /api/v1/comments/{id}/accept`: Chấp nhận bình luận làm câu trả lời chuẩn.
16. `GET /api/v1/events`: Danh sách sự kiện, workshop, tech-talk.
17. `GET /api/v1/events/{id}`: Xem chi tiết sự kiện.
18. `POST /api/v1/events`: Tạo sự kiện mới.
19. `POST /api/v1/events/{id}/rsvp`: Đăng ký tham gia sự kiện (Going / Interested / NotGoing).
20. `GET /api/v1/events/{id}/attendees`: Danh sách thành viên đăng ký sự kiện.
21. `DELETE /api/v1/events/{id}`: Xóa sự kiện.
22. `GET /api/v1/categories`: Lấy danh sách 5 chuyên mục thảo luận FPT.
23. `POST /api/v1/reports`: Báo cáo vi phạm nội dung sang Admin Service.
- **SignalR ForumHub (`/hubs/forum`)**: `JoinPostGroup`, `LeavePostGroup`, `JoinEventsGroup`, `LeaveEventsGroup`.

### 2.5. AI Service (Cổng 5004) - 6 Endpoints
1. `POST /api/v1/ai/chat`: Chatbot hỏi đáp chính sách, văn hóa FPT có dẫn nguồn (Citations).
2. `GET /api/v1/ai/conversations`: Danh sách các phiên trò chuyện với AI.
3. `GET /api/v1/ai/conversations/{id}`: Xem lại chi tiết phiên trò chuyện.
4. `POST /api/v1/ai/rag/ingest`: Admin kích hoạt nạp lại tri thức RAG từ thư mục tài liệu.
5. `POST /api/v1/ai/mentors/recommend`: Gợi ý Top Mentor cựu sinh viên theo chuyên ngành, kỹ năng và mục tiêu nghề nghiệp.
6. `GET & POST /api/v1/ai/boost` (và `/boost`): Tuyến chẩn đoán RAG Boost điểm số, intent detection, và chuẩn hóa tiếng lóng/diacritics FPT.

### 2.6. Admin Service (Cổng 5005) - 20 Endpoints
1. `GET /api/v1/admin/dashboard/summary`: Thống kê KPI tổng quan hệ thống.
2. `GET /api/v1/admin/dashboard/activity-timeline`: Biểu đồ hoạt động hệ thống theo ngày.
3. `GET /api/v1/admin/users`: Tra cứu, lọc theo vai trò/trạng thái và phân trang người dùng.
4. `GET /api/v1/admin/users/{id}`: Xem chi tiết hồ sơ tài khoản thành viên.
5. `POST /api/v1/admin/users/{id}/suspend`: Khóa tài khoản thành viên vi phạm.
6. `POST /api/v1/admin/users/{id}/unsuspend`: Mở khóa tài khoản thành viên.
7. `GET /api/v1/admin/moderation/reports`: Hàng đợi báo cáo vi phạm cần duyệt.
8. `GET /api/v1/admin/moderation/reports/{id}`: Xem chi tiết nội dung bị báo cáo.
9. `POST /api/v1/admin/moderation/reports/{id}/resolve`: Xử lý báo cáo (cảnh cáo, gỡ bài viết).
10. `POST /api/v1/admin/moderation/reports/{id}/dismiss`: Bác bỏ báo cáo không vi phạm.
11. `GET /api/v1/admin/announcements`: Danh sách thông báo toàn trường.
12. `GET /api/v1/admin/announcements/{id}`: Xem chi tiết thông báo.
13. `POST /api/v1/admin/announcements`: Tạo thông báo mới.
14. `PUT /api/v1/admin/announcements/{id}`: Sửa thông báo.
15. `DELETE /api/v1/admin/announcements/{id}`: Xóa thông báo.
16. `GET /api/v1/admin/audit-logs`: Danh sách nhật ký kiểm toán bảo mật.
17. `GET /api/v1/admin/audit-logs/recent`: Top nhật ký kiểm toán mới nhất.
18. `GET /api/v1/admin/audit-logs/{id}`: Xem chi tiết một bản ghi log.
19. `GET /api/v1/admin/export/audit-logs`: Xuất file CSV nhật ký kiểm toán.
20. `GET /api/v1/admin/export/users-summary`: Xuất file CSV danh sách thành viên.

### 2.7. Notification Service (Cổng 5006) - 6 Endpoints + SignalR
1. `GET /api/v1/notifications`: Lấy danh sách thông báo cá nhân.
2. `GET /api/v1/notifications/unread-count`: Đếm số lượng thông báo chưa đọc cho chuông.
3. `PATCH /api/v1/notifications/{id}/read`: Đánh dấu 1 thông báo đã đọc.
4. `POST /api/v1/notifications/read-all`: Đánh dấu tất cả thông báo đã đọc.
5. `DELETE /api/v1/notifications/read`: Xóa các thông báo đã đọc.
6. `DELETE /api/v1/notifications/{id}`: Xóa một thông báo cụ thể.
- **SignalR NotificationHub (`/hubs/notifications`)**:
  - Client gọi: `JoinUserChannel()`.
  - Server phát: `ReceiveNotification`.

---

## 3. KIỂM TOÁN ĐỐI CHIẾU GIAO DIỆN FRONTEND WEB HIỆN TẠI (COVERAGE MATRIX)

Frontend Web hiện tại đã xây dựng 18 Pages, 7 Feature Modules, 12 Services. Dưới đây là ma trận đối chiếu tình trạng tích hợp:

| Phân hệ / Nghiệp vụ | Backend Endpoint | Frontend Service đã ghép | UI Component / Page phục vụ | Trạng thái tích hợp |
|:---|:---|:---|:---|:---:|
| **Đăng nhập Email/Pass** | `POST /auth/login` | `authService.login` | `src/features/auth/components/LoginForm.tsx` |  **Hoàn thành 100%** |
| **Đăng nhập Google SSO** | `POST /auth/google` | `authService.loginWithGoogle` | `LoginForm.tsx` (Firebase Auth) |  **Hoàn thành 100%** |
| **Đăng ký tài khoản** | `POST /auth/register` | `authService.register` | `src/features/auth/components/RegisterForm.tsx` |  **Hoàn thành 100%** |
| **Kích hoạt Email OTP** | `POST /auth/verify-email` | `authService.verifyEmail` | `RegisterForm.tsx`<br>`VerifyEmailPage.tsx` (/auth/verify-email) |  **Hoàn thành 100%** |
| **Quên & Đặt lại mật khẩu** | `POST /auth/forgot-password`<br>`POST /auth/reset-password` | `authService.forgotPassword`<br>`authService.resetPassword` | `src/features/auth/components/ForgotPasswordForm.tsx` |  **Hoàn thành 100%** |
| **Đổi mật khẩu cá nhân** | `POST /auth/change-password` | `authService.changePassword` | `src/pages/user/ProfilePage.tsx` |  **Hoàn thành 100%** |
| **Xem & Sửa hồ sơ cá nhân** | `GET /users/me`<br>`PUT /users/me` | `usersService.getMe`<br>`usersService.updateMe` | `src/pages/user/ProfilePage.tsx` |  **Hoàn thành 100%** |
| **Upload Avatar** | `POST /users/me/avatar/upload`<br>`POST /users/media/upload` | `uploadImageViaBackend`<br>`usersService.updateAvatar` | `ProfilePage.tsx` |  **Hoàn thành 100%** |
| **Cài đặt quyền riêng tư** | `PUT /users/me/privacy` | `usersService.updatePrivacy` | `ProfilePage.tsx` |  **Hoàn thành 100%** |
| **Chặn & Bỏ chặn người dùng** | `POST /users/{id}/block`<br>`DELETE /users/{id}/block` | `usersService.blockUser`<br>`usersService.unblockUser` | `AlumniPage.tsx`<br>`ProfilePage.tsx` |  **Hoàn thành 100%** |
| **Danh bạ cựu sinh viên** | `GET /users`<br>`GET /users/directory` | `alumniService.getAlumniDirectory` | `src/pages/user/AlumniPage.tsx` |  **Hoàn thành 100%** |
| **Xem hồ sơ công khai** | `GET /users/{id}` | `usersService.getUserProfile` | `src/features/users/components/PublicProfileModal.tsx` |  **Hoàn thành 100%** |
| **Phòng Chat & Tin nhắn** | `GET/POST /chats/conversations`<br>`GET/POST /messages` | `chatService.getRooms`<br>`chatService.getMessages` | `src/pages/user/ChatPage.tsx` |  **Hoàn thành 100%** |
| **Chat Realtime WebSocket** | `/hubs/chat` | `chatHubClient` | `ChatPage.tsx` (ReceiveMessage, UserTyping, MessagesRead) |  **Hoàn thành 100%** |
| **Gửi ảnh trong phòng chat** | `POST /messages` (mediaUrl) | `uploadToCloudinary`<br>`chatService.sendMessage` | `ChatPage.tsx` (Photo picker & preview) |  **Hoàn thành 100%** |
| **Tắt chuông phòng chat** | `PUT /conversations/{id}/settings` | `chatService.updateRoomSettings` | `ChatPage.tsx` (Room Settings Menu) |  **Hoàn thành 100%** |
| **Bảng tin bài viết diễn đàn** | `GET /posts` | `postsService.getPosts` | `src/features/posts/components/PostFeed.tsx` |  **Hoàn thành 100%** |
| **Đăng bài viết mới** | `POST /posts` | `postsService.createPost` | `src/features/posts/components/PostComposer.tsx` |  **Hoàn thành 100%** |
| **Bình luận & Trả lời bình luận** | `GET/POST /posts/{id}/comments` | `postsService.getPostComments`<br>`postsService.createComment` | `src/features/posts/components/PostCommentsSection.tsx` |  **Hoàn thành 100%** |
| **Thả Like & Bookmark bài viết** | `POST /posts/{id}/react`<br>`POST /posts/{id}/bookmark` | `postsService.reactToPost`<br>`postsService.bookmarkPost` | `src/features/posts/components/PostCard.tsx` |  **Hoàn thành 100%** |
| **Đánh dấu câu hỏi đã giải quyết**| `PATCH /posts/{id}/answered` | `postsService.markAnswered` | `PostCard.tsx` (Nút tick xanh dành cho tác giả) |  **Hoàn thành 100%** |
| **Báo cáo vi phạm bài viết** | `POST /reports` | `postsService.reportPost` | `src/features/posts/components/ReportPostModal.tsx` |  **Hoàn thành 100%** |
| **Danh mục chuyên môn & Nhóm** | `GET /categories` | `communitiesService.getCategories` | `src/pages/user/CommunitiesPage.tsx` |  **Hoàn thành 100%** |
| **Sự kiện & Workshop (RSVP)** | `GET/POST /events`<br>`POST /events/{id}/rsvp` | `opportunitiesService.getEvents`<br>`opportunitiesService.rsvpEvent` | `src/features/events/components/EventsSection.tsx`<br>`src/pages/user/OpportunitiesPage.tsx` |  **Hoàn thành 100%** |
| **Bảng tin Việc làm / Học bổng** | `GET /posts` (type=Recruitment) | `postsService.getPosts` | `src/pages/user/OpportunitiesPage.tsx` |  **Hoàn thành 100%** |
| **Trợ lý AI RAG Chatbot** | `POST /ai/chat` | `aiService.ask` | `src/features/ai/components/AiChatWindow.tsx`<br>`FloatingAiWidget.tsx` |  **Hoàn thành 100%** |
| **Gợi ý Mentor AI** | `POST /ai/mentors/recommend` | `aiService.recommendMentors` | `RightSidebar.tsx`<br>`OpportunitiesPage.tsx` (Tab ✨ Gợi Ý Mentor AI) |  **Hoàn thành 100%** |
| **Trung tâm Thông báo** | `GET /notifications`<br>`PATCH /notifications/{id}/read` | `notificationsService.getNotifications`<br>`notificationsService.markAsRead` | `src/pages/user/NotificationsPage.tsx` |  **Hoàn thành 100%** |
| **Thông báo Realtime** | `/hubs/notifications` | `notificationHubClient` | `src/stores/notificationStore.ts`<br>`NotificationsPage.tsx` |  **Hoàn thành 100%** |
| **Admin Dashboard & KPI** | `GET /admin/dashboard/summary`<br>`GET /activity-timeline` | `adminService.getDashboardSummary`<br>`adminService.getActivityTimeline` | `src/pages/admin/DashboardPage.tsx` |  **Hoàn thành 100%** |
| **Quản trị người dùng (Admin)** | `GET /admin/users`<br>`POST /suspend`, `/unsuspend` | `adminService.getUsers`<br>`adminService.suspendUser` | `src/pages/admin/UsersManagementPage.tsx` |  **Hoàn thành 100%** |
| **Kiểm duyệt bài viết (Admin)** | `GET /posts`<br>`DELETE /posts/{id}` | `postsService.getPosts`<br>`postsService.deletePost` | `src/pages/admin/PostsManagementPage.tsx` |  **Hoàn thành 100%** |
| **Xử lý vi phạm & Báo cáo** | `GET /admin/moderation/reports`<br>`POST /resolve`, `/dismiss` | `adminService.getReports`<br>`adminService.resolveReport` | `src/pages/admin/ReportsManagementPage.tsx` |  **Hoàn thành 100%** |
| **Thông báo toàn trường** | `GET/POST/PUT/DELETE /admin/announcements` | `adminService.getAnnouncements`<br>`adminService.createAnnouncement` | `src/pages/admin/SettingsPage.tsx` |  **Hoàn thành 100%** |
| **Quản lý tài liệu AI RAG** | `POST /ai/rag/ingest` | `aiService.ingestRag` | `src/pages/admin/SettingsPage.tsx` |  **Hoàn thành 100%** |
| **Nhật ký kiểm toán & Xuất CSV**| `GET /admin/audit-logs`<br>`GET /admin/export/*` | `adminService.getAuditLogs`<br>`adminService.exportAuditLogs` | `src/pages/admin/AnalyticsPage.tsx` |  **Hoàn thành 100%** |
| **Quản trị Cộng đồng (Admin)** | Phục vụ Category & Chương trình | `communitiesService` | `src/pages/admin/CommunitiesManagementPage.tsx` |  **Hoàn thành 100%** |
| **Quản trị Cơ hội (Admin)** | Quản lý Đợt thực tập / Job | `opportunitiesService` | `src/pages/admin/OpportunitiesManagementPage.tsx` |  **Hoàn thành 100%** |
| **Duyệt hồ sơ Alumni (Admin)** | Xác minh văn bằng cựu sinh viên | `useAdminUsers` | `src/pages/admin/AlumniVerificationPage.tsx` |  **Hoàn thành 100%** |

---

## 4. TỔNG HỢP 5 LỖ HỔNG & ĐIỂM NGHẼN CẦN GIẢI QUYẾT (GAP ANALYSIS)

1. **Lỗ hổng 1**: 3 trang quản trị quan trọng (`CommunitiesManagementPage`, `OpportunitiesManagementPage`, `AlumniVerificationPage`) đã code hoàn chỉnh (tổng cộng hơn 1300 dòng code) nhưng trong `src/app/router/routes.tsx` lại bị cấu hình `<Navigate to={ROUTES.ADMIN.DASHBOARD} replace />`.
2. **Lỗ hổng 2**: Tuyến đường `/auth/verify-email` trong `routes.tsx` chỉ chứa thẻ rỗng `<Outlet />`, không có trang đón người dùng khi họ bấm trực tiếp link kích hoạt từ email (`?email=...&token=...`).
3. **Lỗ hổng 3**: `src/services/alumni/index.ts` chỉ export một object rỗng `{}` (dù `AlumniPage` đang gọi ké qua `usersService`), phá vỡ tính nhất quán kiến trúc service boundary.
4. **Lỗ hổng 4**: Khi người dùng nhận được thông báo thời gian thực có đường dẫn chi tiết bài viết (VD: `/app/posts/{id}`), hệ thống chưa có route bắt riêng nên bị rơi vào wildcard fallback về trang chủ, làm mất ngữ cảnh bài viết người dùng muốn xem.
5. **Lỗ hổng 5**: Endpoint `POST /api/v1/ai/mentors/recommend` hỗ trợ truyền `targetSkills` và `careerGoal` để tìm kiếm cựu sinh viên bảo trợ (mentor) phù hợp nhất, nhưng trên Frontend mới chỉ hiển thị một danh sách tĩnh ở sidebar phải mà chưa có giao diện tương tác chuyên sâu.

---

## 5. MÔ HÌNH PHÂN CÔNG 3 AGENTS TRONG MỖI PHASE

Để đảm bảo tốc độ và chất lượng không bị suy giảm, mỗi Phase được phân chia cho đúng **3 Agents** với vai trò cụ thể:

```
┌────────────────────────────────────────────────────────┐
│                   COORDINATOR (AGENT LEAD)             │
└───────────┬───────────────────┬───────────────────┬────┘
            │                   │                   │
            ▼                   ▼                   ▼
     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
     │   AGENT 1   │     │   AGENT 2   │     │   AGENT 3   │
     │Implementer A│     │Implementer B│     │ QA / Tester │
     │  (Core/API) │     │ (UI/Wiring) │     │(Automated/E2E│
     └─────────────┘     └─────────────┘     └─────────────┘
```

- **AGENT 1 (Implementer A - Core & API Services)**:
  - Phụ trách cấu hình Router, Service Boundary, API calls, Axios instances, Types/DTOs, State Stores.
- **AGENT 2 (Implementer B - UI Components & User Experience)**:
  - Phụ trách xây dựng giao diện, Form validation, Modal, Layout rendering, UX Feedback (Toast, Skeleton, Error states).
- **AGENT 3 (Tester / QA Verifier)**:
  - Phụ trách chạy build (`tsc -b`), viết script test kiểm thử hợp đồng, chạy Puppeteer E2E test, kiểm tra edge cases và đảm bảo không có regression.

---

## 6. KẾ HOẠCH CHI TIẾT 4 PHASE TRIỂN KHAI CHO AI AGENTS

---

### PHASE 1: Quản Trị Hệ Thống & Phân Quyền Router (Admin Portal & Routing Governance) - [✅ ĐÃ HOÀN THÀNH - 100%]

> **Trạng thái nghiệm thu**: Đã hoàn thành 100% nhiệm vụ bởi 3 Agents.  
> - **Agent 1**: Mở khóa 3 routes Admin với `RoleGuard` đúng ma trận phân quyền.  
> - **Agent 2**: Đồng bộ `AdminSidebar.tsx` với icon Tabler, menu items `Communities`, `Opportunities`, `Alumni Verification`.  
> - **Agent 3**: Chạy `npm run build` thành công (0 error, 6.00s), `qa-verifier.mjs` đạt **73/73 PASSED**.

#### 🎯 Mục tiêu Phase 1:
Mở khóa 3 trang Quản trị bị ẩn (`CommunitiesManagementPage`, `OpportunitiesManagementPage`, `AlumniVerificationPage`), đồng bộ phân quyền chặt chẽ theo ma trận Role (Admin, Moderator, Faculty) và liên kết thanh điều hướng Sidebar.

#### 👥 Phân công nhiệm vụ:

##### 🛠️ AGENT 1 (Implementer A - Router & Import Architecture):
- **Tập tin can thiệp**: `src/app/router/routes.tsx`
- **Chỉ dẫn thực thi chính xác**:
  1. Thêm import 3 page components vào đầu file `routes.tsx`:
     ```typescript
     import {
       DashboardPage,
       UsersManagementPage,
       PostsManagementPage,
       ReportsManagementPage,
       AnalyticsPage,
       SettingsPage,
       CommunitiesManagementPage,
       OpportunitiesManagementPage,
       AlumniVerificationPage,
     } from '@/pages/admin'
     ```
  2. Tìm đoạn code bị lỗi dòng 150-161:
     ```typescript
     // [XÓA ĐOẠN NÀY]:
     { path: 'communities', element: <Navigate to={ROUTES.ADMIN.DASHBOARD} replace /> },
     { path: 'opportunities', element: <Navigate to={ROUTES.ADMIN.DASHBOARD} replace /> },
     { path: 'alumni-verification', element: <Navigate to={ROUTES.ADMIN.USERS} replace /> },
     ```
  3. Thay thế bằng khai báo RoleGuard chuẩn xác:
     ```typescript
     // [THAY BẰNG ĐOẠN NÀY]:
     {
       path: 'communities',
       element: (
         <RoleGuard allowedRoles={[ROLES.ADMIN, 'FACULTY']}>
           <CommunitiesManagementPage />
         </RoleGuard>
       ),
     },
     {
       path: 'opportunities',
       element: (
         <RoleGuard allowedRoles={[ROLES.ADMIN, 'FACULTY']}>
           <OpportunitiesManagementPage />
         </RoleGuard>
       ),
     },
     {
       path: 'alumni-verification',
       element: (
         <RoleGuard allowedRoles={[ROLES.ADMIN]}>
           <AlumniVerificationPage />
         </RoleGuard>
       ),
     },
     ```

##### 🎨 AGENT 2 (Implementer B - Admin Navigation & Component Polishing):
- **Tập tin can thiệp**:
  - `src/components/layout/AdminSidebar.tsx`
  - `src/pages/admin/CommunitiesManagementPage.tsx`
  - `src/pages/admin/OpportunitiesManagementPage.tsx`
  - `src/pages/admin/AlumniVerificationPage.tsx`
- **Chỉ dẫn thực thi chính xác**:
  1. Kiểm tra `AdminSidebar.tsx`: Đảm bảo các menu item `communities`, `opportunities`, `alumni-verification` có đúng đường dẫn `ROUTES.ADMIN.COMMUNITIES`, `ROUTES.ADMIN.OPPORTUNITIES`, `ROUTES.ADMIN.ALUMNI_VERIFICATION`.
  2. Kiểm tra `CommunitiesManagementPage.tsx`: Đảm bảo khi Admin/Faculty bấm Ghim thông báo (Pin announcement) hoặc lọc theo khoa, danh sách được cập nhật trạng thái hiển thị rõ ràng.
  3. Kiểm tra `AlumniVerificationPage.tsx`: Đảm bảo 2 nút "Phê duyệt" (Approve) và "Từ chối" (Reject) hoạt động trơn tru với optimistic UI update.

##### 🧪 AGENT 3 (Tester / QA Verifier - Automation & Regression):
- **Lệnh thực thi**:
  ```powershell
  cd c:\Use\University\PRN232\ASM\-F-Alumni---Web
  npm run build
  node scripts/qa-verifier.mjs
  ```
- **Kịch bản kiểm thử**:
  - Đảm bảo `qa-verifier.mjs` chạy 73/73 test PASSED.
  - Chạy `scripts/e2e-admin-all-pages.mjs` xác nhận 3 URL:
    - `http://localhost:5173/admin/communities` $\rightarrow$ Render đúng tiêu đề "Quản lý Cộng đồng & Chuyên ngành".
    - `http://localhost:5173/admin/opportunities` $\rightarrow$ Render đúng tiêu đề "Quản lý Cơ hội Nghề nghiệp & Học bổng".
    - `http://localhost:5173/admin/alumni-verification` $\rightarrow$ Render đúng tiêu đề "Xét duyệt Xác minh Cựu Sinh viên".
  - Chụp màn hình lưu vào `screenshots/` làm bằng chứng nghiệm thu.

---

### PHASE 2: Xác Thực Tài Khoản & Mạng Lưới Cựu Sinh Viên (Auth Lifecycle & Alumni Directory) - [✅ ĐÃ HOÀN THÀNH - 100%]

> **Trạng thái nghiệm thu**: Đã hoàn thành 100% nhiệm vụ bởi 3 Agents.  
> - **Agent 1**: Hoàn thiện `src/services/alumni/index.ts` chuẩn microservice boundary (`getAlumniDirectory`, `getAlumniProfile`, `blockMember`, `unblockMember`, `getBlockedMembers`) và nâng cấp `authService.verifyEmail` linh hoạt nhận cả OTP và direct token.  
> - **Agent 2**: Xây dựng trang [VerifyEmailPage.tsx](file:///c:/Use/University/PRN232/ASM/-F-Alumni---Web/src/pages/auth/VerifyEmailPage.tsx) (hỗ trợ tự động kích hoạt qua query URL `?email=...&token=...`, form nhập mã OTP thủ công, resend OTP và màn hình báo thành công), xuất trang tại `src/pages/auth/index.ts`, gắn router tại `routes.tsx` và đồng bộ `AlumniPage.tsx` sang `alumniService`.  
> - **Agent 3**: Chạy `npm run build` thành công (0 lỗi TypeScript, 9.29s), `scripts/qa-phase2-verifier.mjs` đạt **6/6 PASSED**, `scripts/qa-verifier.mjs` đạt **73/73 PASSED**.

#### 🎯 Mục tiêu Phase 2:
Xây dựng trang xác thực Email độc lập `VerifyEmailPage.tsx` phục vụ việc kích hoạt tài khoản qua liên kết email, và chuẩn hóa module dịch vụ `alumniService` để quản lý danh bạ cựu sinh viên chuẩn kiến trúc microservices.

#### 👥 Phân công nhiệm vụ:

##### 🛠️ AGENT 1 (Implementer A - Auth Service & Alumni Service Boundary):
- **Tập tin can thiệp**:
  - `src/services/alumni/index.ts`
  - `src/services/auth/index.ts`
  - `src/types/alumni.types.ts`
- **Chỉ dẫn thực thi chính xác**:
  1. Viết lại hoàn chỉnh `src/services/alumni/index.ts`:
     ```typescript
     import { usersService } from '@/services/users'
     import type {
       BackendUserSummary,
       BackendPagedResult,
       GetUsersParams,
       BackendPublicProfileResponse,
     } from '@/types/user.types'

     export const alumniService = {
       async getAlumniDirectory(params?: GetUsersParams): Promise<BackendPagedResult<BackendUserSummary>> {
         return usersService.searchUsers({
           ...params,
           role: params?.role || 'Alumni',
         })
       },

       async getAlumniProfile(userId: string): Promise<BackendPublicProfileResponse> {
         return usersService.getUserProfile(userId)
       },

       async blockMember(userId: string): Promise<void> {
         return usersService.blockUser(userId)
       },
     }
     ```
  2. Xác minh `authService.verifyEmail` trong `src/services/auth/index.ts` có nhận đúng `{ email, token }` và trả về kết quả chuẩn RFC.

##### 🎨 AGENT 2 (Implementer B - Standalone Verify Email UI & Alumni Directory View):
- **Tập tin can thiệp**:
  - `src/pages/auth/VerifyEmailPage.tsx` [TẠO MỚI]
  - `src/pages/auth/index.ts`
  - `src/app/router/routes.tsx`
  - `src/pages/user/AlumniPage.tsx`
- **Chỉ dẫn thực thi chính xác**:
  1. Tạo file mới `src/pages/auth/VerifyEmailPage.tsx` tiếp nhận tham số URL:
     ```tsx
     const [searchParams] = useSearchParams()
     const emailParam = searchParams.get('email') || ''
     const tokenParam = searchParams.get('token') || ''
     ```
     - Tự động gọi `authService.verifyEmail(emailParam, tokenParam)` nếu URL có sẵn token.
     - Nếu không có tham số, hiển thị 2 ô input nhập Email và mã OTP 6 số.
     - Hiển thị Toast / Alert thành công kèm nút chuyển hướng sang `ROUTES.APP.HOME` hoặc `ROUTES.AUTH.LOGIN`.
  2. Export tại `src/pages/auth/index.ts`: `export * from './VerifyEmailPage'`.
  3. Cập nhật `routes.tsx`: Gán `{ path: 'verify-email', element: <VerifyEmailPage /> }`.
  4. Cập nhật `src/pages/user/AlumniPage.tsx`: Nhập và sử dụng `alumniService.getAlumniDirectory` để truy vấn danh bạ cựu sinh viên.

##### 🧪 AGENT 3 (Tester / QA Verifier - Automation & Edge Cases):
- **Lệnh thực thi**:
  ```powershell
  cd c:\Use\University\PRN232\ASM\-F-Alumni---Web
  npm run build
  ```
- **Kịch bản kiểm thử**:
  - Test case 1: Truy cập `http://localhost:5173/auth/verify-email?email=test@fpt.edu.vn&token=123456` $\rightarrow$ Kiểm tra giao diện hiển thị trạng thái đang xác thực (loading spinner).
  - Test case 2: Truy cập `http://localhost:5173/auth/verify-email` không kèm param $\rightarrow$ Kiểm tra form nhập liệu thủ công hiển thị đầy đủ, validation bắt buộc nhập đủ trường.
  - Test case 3: Mở trang `/app/alumni` $\rightarrow$ Xác nhận danh bạ tải mượt mà qua `alumniService`, các bộ lọc Major, Campus hoạt động chính xác.

---

### PHASE 3: Diễn Đàn Thảo Luận, Deep Linking & Sự Kiện (Forum Discussions, Post Detail & Events) - [✅ ĐÃ HOÀN THÀNH - 100%]

> **Trạng thái nghiệm thu**: Đã hoàn thành 100% nhiệm vụ bởi 3 Agents.  
> - **Agent 1**: Khai báo tuyến đường con `/app/posts/:id` & `/app/posts/:postId` tại `routes.tsx`, nâng cấp `HomePage.tsx` và `PostFeed.tsx` tự động tìm nạp bài viết đích theo ID, đẩy lên vị trí đầu tiên của bảng tin.  
> - **Agent 2**: Cập nhật `PostCard.tsx` tự động cuộn trang (`scrollIntoView`), hiệu ứng highlight nổi bật bài viết đích, mở sẵn bình luận và nút thao tác 1 chạm chuyển đổi trạng thái "Đã giải quyết" cho tác giả/Admin (FO-16). Nâng cấp `PostCommentsSection.tsx` chọn Best Answer và kích hoạt callback đồng bộ.  
> - **Agent 3**: Chạy `npm run build` thành công (0 lỗi TypeScript, 7.67s), `scripts/qa-phase3-verifier.mjs` đạt **6/6 PASSED**, `scripts/qa-verifier.mjs` đạt **73/73 PASSED**.

#### 🎯 Mục tiêu Phase 3:
Xây dựng tuyến đường Deep Link bài viết chi tiết `/app/posts/:id` (kết nối với hệ thống thông báo SignalR), hoàn thiện tính năng đánh dấu câu trả lời chuẩn (`isAcceptedAnswer`) và nâng cấp khu vực sự kiện cộng đồng.

#### 👥 Phân công nhiệm vụ:

##### 🛠️ AGENT 1 (Implementer A - Deep Link Routing & Post Data Binding):
- **Tập tin can thiệp**:
  - `src/app/router/routes.tsx`
  - `src/pages/user/HomePage.tsx`
  - `src/features/posts/components/PostFeed.tsx`
  - `src/services/posts/index.ts`
- **Chỉ dẫn thực thi chính xác**:
  1. Thêm tuyến đường con trong `src/app/router/routes.tsx` tại nhánh `/app/*`:
     ```typescript
     {
       path: 'posts/:id',
       element: <HomePage />,
     },
     ```
  2. Trong `src/pages/user/HomePage.tsx`:
     - Sử dụng hook `useParams<{ id?: string }>()`.
     - Nếu có `id`, gọi `postsService.getPostById(id)` và đẩy bài viết đó lên vị trí đầu tiên của bảng tin.
     - Tự động cuộn trang (`scrollIntoView`) đến thẻ bài viết và tự động mở sẵn vùng bình luận.

##### 🎨 AGENT 2 (Implementer B - Question Lifecycle & Event Attendees Experience):
- **Tập tin can thiệp**:
  - `src/features/posts/components/PostCard.tsx`
  - `src/features/posts/components/PostCommentsSection.tsx`
  - `src/features/events/components/EventAttendeesModal.tsx`
  - `src/features/events/components/EventsSection.tsx`
- **Chỉ dẫn thực thi chính xác**:
  1. Trong `PostCard.tsx`:
     - Nếu bài viết thuộc loại `Question` và do tài khoản hiện tại đăng, hiển thị nút toggle "Đã giải quyết" gọi `postsService.markAnswered(postId, true)`.
  2. Trong `PostCommentsSection.tsx`:
     - Với mỗi bình luận trong bài viết dạng `Question`, hiển thị nút "Chấp nhận câu trả lời" (`postsService.acceptAnswer(commentId)`). Bình luận được chấp nhận sẽ có viền xanh lá và huy hiệu "Câu trả lời chuẩn".
  3. Trong `EventsSection.tsx`:
     - Kết nối nút "Xem người tham gia" mở `EventAttendeesModal.tsx` gọi API `opportunitiesService.getAttendees(eventId)`.

##### 🧪 AGENT 3 (Tester / QA Verifier - Forum Contract & Real-time Verification):
- **Lệnh thực thi**:
  ```powershell
  cd c:\Use\University\PRN232\ASM\-F-Alumni---Web
  node scripts/qa-api-contract-verifier.mjs
  npm run build
  ```
- **Kịch bản kiểm thử**:
  - Tạo một bài viết dạng `Question` $\rightarrow$ Thêm 2 bình luận $\rightarrow$ Bấm nút chấp nhận 1 bình luận $\rightarrow$ Kiểm tra trạng thái lưu vào database và giao diện hiển thị đúng huy hiệu.
  - Bấm vào một link thông báo có định dạng `/app/posts/{id}` $\rightarrow$ Xác nhận không bị đá về Home mặc định, bài viết mục tiêu được hiển thị nổi bật.
  - Kiểm tra RSVP sự kiện (Going / Interested) $\rightarrow$ Danh sách người tham gia trong Modal nhảy số tức thì.

---

### PHASE 4: Trí Tuệ Nhân Tạo RAG, Mentor Matching Hub & Đồng Bộ Realtime (AI RAG, Mentor Hub & Realtime Sync) - [✅ ĐÃ HOÀN THÀNH - 100%]

> **Trạng thái nghiệm thu**: Đã hoàn thành 100% nhiệm vụ bởi 3 Agents.  
> - **Agent 1**: Xây dựng Trung tâm Gợi ý Mentor AI chuyên sâu (Tab `✨ Gợi Ý Mentor AI`) trong [OpportunitiesPage.tsx](file:///c:/Use/University/PRN232/ASM/-F-Alumni---Web/src/pages/user/OpportunitiesPage.tsx) với bộ lọc kỹ năng, chuyên ngành và nguyện vọng sự nghiệp gọi `aiService.recommendMentors`, cùng công cụ chẩn đoán AI Boost (`aiService.testBoost`) trong [SettingsPage.tsx](file:///c:/Use/University/PRN232/ASM/-F-Alumni---Web/src/pages/admin/SettingsPage.tsx).  
> - **Agent 2**: Tối ưu cơ chế tự động kết nối lại SignalR kép `.withAutomaticReconnect([0, 2000, 5000, 10000, 30000])` trong `signalr.ts` và `notifications/index.ts`, đồng bộ realtime số lượng thông báo chưa đọc và cập nhật nhận diện `Gemini 2.5 Flash • RAG Boost` trong `AiChatWindow.tsx`.  
> - **Agent 3**: Chạy `npm run build` thành công (0 lỗi TypeScript, 6.23s), `scripts/qa-phase4-verifier.mjs` đạt **6/6 PASSED**, toàn bộ hệ thống đạt **91/91 TEST PASSED (100%)**.

#### 🎯 Mục tiêu Phase 4:
Tận dụng tối đa backend AI mới cập nhật (`gemini-2.5-flash`, RAG Boost tiếng Việt, chẩn đoán `/boost`), xây dựng Trung tâm Gợi ý Mentor AI chuyên sâu, và tối ưu kết nối SignalR kép (ChatHub + NotificationHub).

#### 👥 Phân công nhiệm vụ:

##### 🛠️ AGENT 1 (Implementer A - Mentor Matching Hub & AI Boost Diagnostics):
- **Tập tin can thiệp**:
  - `src/services/ai/index.ts`
  - `src/app/router/routes.tsx`
  - `src/pages/user/OpportunitiesPage.tsx`
  - `src/pages/admin/SettingsPage.tsx`
- **Chỉ dẫn thực thi chính xác**:
  1. Xác minh hàm `testBoost` trong `src/services/ai/index.ts`:
     ```typescript
     async testBoost(query: string, major?: string, campus = 'All', topK = 5): Promise<any> {
       const res = await aiApiClient.get<any>('/ai/boost', {
         params: { q: query, major, campus, topK },
       })
       return res.data
     }
     ```
  2. Trong `OpportunitiesPage.tsx`: Bổ sung thêm Tab "Gợi ý Mentor AI" (`activeTab === 'mentors'`):
     - Cho phép sinh viên bấm chọn nhanh các tag kỹ năng: `Microservices`, `.NET 10`, `Docker`, `Kafka`, `React`, `System Design`, `AI / ML`, `DevOps`.
     - Nhập mục tiêu nghề nghiệp (Career Goal).
     - Bấm "Tìm Mentor Phù Hợp" $\rightarrow$ Gọi `aiService.recommendMentors({ major, targetSkills, careerGoal })`.
  3. Trong `SettingsPage.tsx` (Dành cho Admin): Bổ sung khu vực "Chẩn đoán RAG & Intent Booster" cho phép Admin nhập câu hỏi thử nghiệm để xem điểm số phân tích RAG trực tiếp từ backend AI mới.

##### 🎨 AGENT 2 (Implementer B - SignalR Reconnection Resilience & Media Upload):
- **Tập tin can thiệp**:
  - `src/services/chat/signalr.ts`
  - `src/services/notifications/index.ts`
  - `src/services/media/uploadService.ts`
  - `src/features/ai/components/AiChatWindow.tsx`
- **Chỉ dẫn thực thi chính xác**:
  1. Trong `signalr.ts` và `notifications/index.ts`: Bổ sung cấu hình `.withAutomaticReconnect([0, 2000, 5000, 10000, 30000])` đảm bảo khi rớt mạng WebSocket sẽ tự động khôi phục kết nối mà không bắt reload trang.
  2. Trong `uploadService.ts`: Đảm bảo quy trình upload ảnh 2 bước:
     - Bước 1: Gọi `/api/v1/users/media/upload` (Backend upload an toàn).
     - Bước 2: Tự động fallback sang Cloudinary direct upload nếu backend gateway bận.
  3. Trong `AiChatWindow.tsx`: Tối ưu giao diện hiển thị câu trả lời từ model Gemini 2.5 Flash, hiển thị trích dẫn (Citations) đẹp mắt với nhãn văn bản trường FPT.

##### 🧪 AGENT 3 (Tester / QA Verifier - Final End-to-End Certification):
- **Lệnh thực thi**:
  ```powershell
  cd c:\Use\University\PRN232\ASM\-F-Alumni---Web
  npm run build
  npm run lint
  node scripts/qa-verifier.mjs
  node scripts/qa-api-contract-verifier.mjs
  node scripts/e2e-live-browser-test.mjs
  ```
- **Kịch bản kiểm thử toàn diện**:
  - Test Chat Realtime: Mở 2 trình duyệt ẩn danh độc lập, chat qua lại $\rightarrow$ tin nhắn hiển thị tức thì, chỉ báo gõ phím mượt mà, tick đã đọc cập nhật chính xác.
  - Test AI RAG: Đặt câu hỏi tiếng Việt không dấu (VD: "hoc phi hola nganh se la bao nhieu") $\rightarrow$ Xác nhận AI trả về đúng câu trả lời học phí kèm trích dẫn văn bản FPT.
  - Kiểm tra 0 lỗi Console, 0 lỗi TypeScript build, toàn bộ test cases đạt 100%.

---

## 7. BẢNG CHỈ DẪN TRÁNH HALLUCINATION DÀNH CHO AI AGENTS MODEL THẤP

Để ngăn chặn các AI Agent model thấp tự ý suy đoán hoặc làm sai cấu trúc, **bắt buộc tuân thủ 5 nguyên tắc vàng**:

1. **Tuyệt đối không đổi tên cổng và route gốc**:
   - Cổng Gateway duy nhất: `http://localhost:5000/api/v1`.
   - Không được tự ý đổi prefix `/api/v1/` thành `/api/` hoặc gọi trực tiếp cổng nội bộ `5001-5006` trừ khi kiểm thử độc lập.
2. **Tuân thủ đúng định dạng DTO**:
   - Mọi phân trang backend đều dùng thuộc tính: `{ items, totalCount, page, pageSize }`. Không tự ý đổi thành `data`, `total`, `totalPages`.
   - Kết quả trả về của API User/Admin thường được bọc trong `res.data?.data || res.data`. Luôn dùng cú pháp an toàn này khi bóc tách dữ liệu.
3. **Giữ nguyên định dạng ID là GUID**:
   - Mọi `id`, `userId`, `conversationId`, `postId` đều là chuỗi GUID (VD: `550e8400-e29b-41d4-a716-446655440000`). Không tạo ID giả dạng số integer `1, 2, 3`.
4. **Không xóa bỏ code đã có**:
   - 3 trang `CommunitiesManagementPage.tsx`, `OpportunitiesManagementPage.tsx`, `AlumniVerificationPage.tsx` đã được viết rất hoàn chỉnh. Tuyệt đối không xóa nội dung của các file này, chỉ cần mở khóa route trong `routes.tsx` và gắn vào menu.
5. **Đọc kỹ file trước khi chỉnh sửa**:
   - Luôn sử dụng công cụ đọc code trước khi sửa để nắm đúng các import hiện có, tránh tạo ra duplicate import làm lỗi build.

---

## 8. TIÊU CHUẨN NGHIỆM THU & BỘ SCRIPTS VERIFICATION

Mỗi Phase chỉ được coi là **HOÀN THÀNH (DONE)** khi Agent Tester xác nhận đạt đủ 4 tiêu chí:

| Tiêu chuẩn | Lệnh kiểm tra | Kết quả yêu cầu |
|:---|:---|:---:|
| **1. Biên dịch TypeScript** | `npm run build` | Exit Code `0`, 0 Error |
| **2. Kiểm tra Linting** | `npm run lint` | 0 Error |
| **3. Phân quyền Router** | `node scripts/qa-verifier.mjs` | `73/73 PASSED` (100%) |
| **4. Hợp đồng API** | `node scripts/qa-api-contract-verifier.mjs` | Toàn bộ hợp đồng `PASSED` |

---
*Tài liệu kế hoạch 4 Phase phân công 3 Agents được thiết lập chuẩn hóa bởi Deepmind Antigravity phục vụ bài tập lớn PRN232.*
