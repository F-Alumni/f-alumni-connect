# 🌐 Cẩm Nang Tích Hợp Web Frontend (React 18 / Vite / Next.js)

> **Dự án**: FPT Alumni Connect (PRN232)  
> **Dành cho**: Đội ngũ phát triển Frontend Web (React / TypeScript / Vite / Next.js)  
> **Điểm vào API duy nhất**: `http://localhost:5000` (API Gateway)  
> **Trạng thái**:  **CORS ĐÃ ĐƯỢC MỞ TOÀN BỘ** (`AllowCredentials`, mọi origin/header/method).

---

## 📑 Mục Lục
1. [Cấu Hình Biến Môi Trường (.env)](#1-cấu-hình-biến-môi-trường-env)
2. [Cơ Chế Xác Thực JWT & Axios Interceptor Tự Động Refresh Token](#2-cơ-chế-xác-thực-jwt--axios-interceptor-tự-động-refresh-token)
3. [Tích Hợp Thời Gian Thực (SignalR WebSockets)](#3-tích-hợp-thời-gian-thực-signalr-websockets)
4. [Bảng Tra Cứu API Endpoints Chi Tiết (Full API Cheatsheet)](#4-bảng-tra-cứu-api-endpoints-chi-tiết-full-api-cheatsheet)
5. [Mã Nguồn Mẫu Hoàn Chỉnh (Ready-to-use Code Snippets)](#5-mã-nguồn-mẫu-hoàn-chỉnh-ready-to-use-code-snippets)

---

## 1. Cấu Hình Biến Môi Trường (.env)

Tạo file `.env.local` hoặc `.env` trong thư mục dự án React/Vite:

```ini
# Base URL trỏ thẳng về API Gateway (Port 5000)
VITE_API_BASE_URL=http://localhost:5000/api/v1

# Điểm kết nối SignalR Hubs
VITE_CHAT_HUB_URL=http://localhost:5000/hubs/chat
VITE_FORUM_HUB_URL=http://localhost:5000/hubs/forum
```

> ⚠️ **LƯU Ý**: Không bao giờ gọi trực tiếp tới cổng nội bộ `5001, 5002, 5003, 5004, 5005`. Mọi request phải đi qua Gateway cổng **`5000`** để được định tuyến tự động.

---

## 2. Cơ Chế Xác Thực JWT & Axios Interceptor Tự Động Refresh Token

- **Access Token**: JWT có thời hạn ngắn (60 phút). Cần gửi trong header: `Authorization: Bearer <accessToken>`.
- **Refresh Token**: Dùng để cấp lại access token mới khi hết hạn mà không bắt người dùng đăng nhập lại.
- **Xử lý lỗi chuẩn RFC 7807**: Khi backend trả mã lỗi 4xx/5xx, body luôn có dạng:
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    "title": "Bad Request",
    "status": 400,
    "detail": "Email đã được đăng ký trong hệ thống."
  }
  ```

---

## 3. Tích Hợp Thời Gian Thực (SignalR WebSockets)

Cài đặt thư viện chính thức của Microsoft:
```bash
npm install @microsoft/signalr
```

### 3.1. Chat Hub (`http://localhost:5000/hubs/chat`)
- **Xác thực**: Gửi token qua `accessTokenFactory`.
- **Sự kiện lắng nghe (Client receives)**:
  - `ReceiveMessage`: Nhận tin nhắn mới tức thì.
  - `UserTyping`: Đối phương đang gõ tin nhắn (`{ conversationId, userId, isTyping }`).
  - `MessagesRead`: Đã xem tin nhắn (`{ conversationId, readByUserId, readAt }`).
- **Phương thức gọi lên (Client invokes)**:
  - `SendMessage(conversationId, content)`
  - `SendTyping(conversationId, isTyping)`
  - `MarkAsRead(conversationId)`

### 3.2. Forum Hub (`http://localhost:5000/hubs/forum`)
- **Sự kiện lắng nghe (Client receives)**:
  - `PostCreated`: Có bài viết mới trên diễn đàn.
  - `CommentAdded`: Có người bình luận vào bài viết bạn đang xem.
  - `ReactionUpdated`: Số lượt like/thả tim thay đổi realtime.

---

## 4. Bảng Tra Cứu API Endpoints Chi Tiết (Full API Cheatsheet)

### 4.1. Phân Hệ Xác Thực & Thành Viên (User Service via Gateway)

| Endpoint | Method | Yêu cầu Auth | Mô tả & Chức năng |
| :--- | :---: | :---: | :--- |
| `/api/v1/auth/login` | `POST` | Không | Đăng nhập bằng `email` & `password`. Trả về `accessToken`, `refreshToken`, `user`. |
| `/api/v1/auth/register` | `POST` | Không | Đăng ký tài khoản mới (`email`, `password`, `fullName`, `accountType`). |
| `/api/v1/auth/refresh-token`| `POST` | Không | Cấp mới token (`{ refreshToken }`). |
| `/api/v1/users/me` | `GET` | Bearer | Lấy thông tin hồ sơ của chính mình (StudentCode, Major, Company, Bio, Avatar). |
| `/api/v1/users/me` | `PUT` | Bearer | Cập nhật hồ sơ cá nhân (`displayName`, `company`, `jobTitle`, `skills`, `bio`). |
| `/api/v1/users` | `GET` | Bearer | Tra cứu danh bạ cựu sinh viên (Hỗ trợ phân trang, lọc theo `search`, `major`, `graduationYear`). |

---

### 4.2. Phân Hệ Trò Chuyện & Nhắn Tin (Chat Service via Gateway)

| Endpoint | Method | Yêu cầu Auth | Mô tả & Chức năng |
| :--- | :---: | :---: | :--- |
| `/api/v1/chat/conversations` | `GET` | Bearer | Lấy danh sách cuộc trò chuyện gần nhất kèm thông tin người đối thoại và tin nhắn cuối. |
| `/api/v1/chat/conversations` | `POST` | Bearer | Tạo hoặc mở phòng chat với một User (`{ recipientId: "..." }`). |
| `/api/v1/chat/conversations/{id}/messages` | `GET` | Bearer | Lấy lịch sử tin nhắn của cuộc trò chuyện (phân trang theo `pageNumber`, `pageSize`). |
| `/api/v1/chat/conversations/{id}/messages` | `POST` | Bearer | Gửi tin nhắn văn bản (`{ content: "Xin chào bạn!" }`). |

---

### 4.3. Phân Hệ Diễn Đàn & Sự Kiện (Forum Service via Gateway)

| Endpoint | Method | Yêu cầu Auth | Mô tả & Chức năng |
| :--- | :---: | :---: | :--- |
| `/api/v1/categories` | `GET` | Không | Lấy danh sách 5 chuyên mục: Software Engineering, AI, Security, Career, Life. |
| `/api/v1/posts` | `GET` | Không/Bearer | Lấy danh sách bài viết (lọc theo `categoryId`, `search`, phân trang). |
| `/api/v1/posts` | `POST` | Bearer | Đăng bài viết mới (`title`, `content`, `categoryId`, `tags`). *(Tự kích hoạt Kafka sang AI Service)* |
| `/api/v1/posts/{id}` | `GET` | Không/Bearer | Xem chi tiết bài viết kèm danh sách bình luận và tổng số reaction. |
| `/api/v1/posts/{id}/comments`| `POST` | Bearer | Gửi bình luận vào bài viết (`{ content: "Bài viết rất hay!" }`). |
| `/api/v1/posts/{id}/reactions`| `POST` | Bearer | Thả biểu cảm (`Like`, `Love`, `Insightful`). |
| `/api/v1/events` | `GET` | Không | Lấy danh sách sự kiện, hội thảo Alumni Tech Talk. |
| `/api/v1/events/{id}/register`| `POST` | Bearer | Đăng ký tham gia sự kiện. |

---

### 4.4. Phân Hệ Trợ Lý AI & Đề Xuất Mentor (AI Service via Gateway)

| Endpoint | Method | Yêu cầu Auth | Mô tả & Chức năng |
| :--- | :---: | :---: | :--- |
| `/api/v1/ai/chat` | `POST` | Tùy chọn | Đặt câu hỏi cho AI Chatbot FPT Alumni (`{ question: "...", conversationId?: "..." }`). |
| `/api/v1/ai/conversations` | `GET` | Bearer | Lấy lịch sử các phiên chat với AI của người dùng. |
| `/api/v1/ai/conversations/{id}`| `GET` | Bearer | Lấy chi tiết câu hỏi và câu trả lời kèm **trích dẫn nguồn tài liệu FPT** (Citations). |
| `/api/v1/ai/mentors/recommend`| `POST`| Bearer | Gợi ý Mentor phù hợp (`{ major: "SE", targetSkills: ["Microservices", "Docker"], careerGoal: "Backend Tech Lead" }`). |

---

### 4.5. Phân Hệ Quản Trị Hệ Thống (Admin Service via Gateway)

| Endpoint | Method | Yêu cầu Auth | Mô tả & Chức năng |
| :--- | :---: | :---: | :--- |
| `/api/v1/admin/users` | `GET` | Role ADMIN | Danh sách người dùng hệ thống kèm trạng thái (`Active`, `Suspended`). |
| `/api/v1/admin/users/{id}/suspend`| `POST` | Role ADMIN | Khóa tài khoản vi phạm kèm lý do (`{ reason: "Spam diễn đàn" }`). |
| `/api/v1/admin/users/{id}/activate`| `POST` | Role ADMIN | Mở khóa lại tài khoản người dùng. |
| `/api/v1/admin/reports` | `GET` | Role ADMIN | Danh sách báo cáo vi phạm bài viết hoặc người dùng. |
| `/api/v1/admin/statistics` | `GET` | Role ADMIN | Thống kê tổng số users, bài viết, lượt chat và tỷ lệ sinh viên kết nối. |

---

## 5. Mã Nguồn Mẫu Hoàn Chỉnh (Ready-to-use Code Snippets)

### 5.1. File `src/services/apiClient.ts` (Axios Interceptor tự động Refresh Token)

```typescript
import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api/v1';

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true, // Cho phép gửi cookies nếu cần
});

// Request Interceptor: Tự động đính kèm JWT Access Token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('accessToken');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response Interceptor: Tự động gọi Refresh Token khi gặp lỗi 401
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;
      try {
        const refreshToken = localStorage.getItem('refreshToken');
        if (!refreshToken) throw new Error('No refresh token');

        // Gọi refresh token sang Gateway
        const res = await axios.post(`${API_BASE_URL}/auth/refresh-token`, {
          refreshToken,
        });

        const { accessToken, refreshToken: newRefresh } = res.data;
        localStorage.setItem('accessToken', accessToken);
        localStorage.setItem('refreshToken', newRefresh);

        // Gửi lại request ban đầu với token mới
        originalRequest.headers.Authorization = `Bearer ${accessToken}`;
        return apiClient(originalRequest);
      } catch (refreshErr) {
        // Hết hạn cả refresh token -> Đăng xuất
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        window.location.href = '/login';
        return Promise.reject(refreshErr);
      }
    }
    return Promise.reject(error);
  }
);
```

---

### 5.2. File `src/hooks/useSignalRChat.ts` (React Hook kết nối Realtime Chat)

```typescript
import { useEffect, useState, useRef } from 'react';
import * as signalR from '@microsoft/signalr';

interface ChatMessage {
  id: string;
  conversationId: string;
  senderId: string;
  content: string;
  createdAt: string;
}

export function useSignalRChat(conversationId?: string) {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isTyping, setIsTyping] = useState<boolean>(false);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    const hubUrl = import.meta.env.VITE_CHAT_HUB_URL || 'http://localhost:5000/hubs/chat';

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => localStorage.getItem('accessToken') || '',
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    connection.on('ReceiveMessage', (message: ChatMessage) => {
      if (!conversationId || message.conversationId === conversationId) {
        setMessages((prev) => [...prev, message]);
      }
    });

    connection.on('UserTyping', (data: { conversationId: string; isTyping: boolean }) => {
      if (data.conversationId === conversationId) {
        setIsTyping(data.isTyping);
      }
    });

    connection
      .start()
      .then(() => console.log('SignalR Chat Connected'))
      .catch((err) => console.error('SignalR Connection Error:', err));

    connectionRef.current = connection;

    return () => {
      connection.stop();
    };
  }, [conversationId]);

  const sendMessage = async (convId: string, content: string) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('SendMessage', convId, content);
    }
  };

  const sendTyping = async (convId: string, typing: boolean) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('SendTyping', convId, typing);
    }
  };

  return { messages, isTyping, sendMessage, sendTyping };
}
```
