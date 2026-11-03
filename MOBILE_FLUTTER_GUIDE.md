# 📱 Cẩm Nang Tích Hợp Ứng Dụng Mobile Flutter (Android & iOS)

> **Dự án**: FPT Alumni Connect (PRN232)  
> **Dành cho**: Đội ngũ phát triển Flutter Mobile App  
> **Điểm kết nối**: API Gateway (Port 5000)  
> **Hỗ trợ nền tảng**: Android (Máy thật & Emulator), iOS (Máy thật & Simulator)

---

## 📑 Mục Lục
1. [Ma Trận Địa Chỉ IP Mạng (Network & IP Configuration)](#1-ma-trận-địa-chỉ-ip-mạng-network--ip-configuration)
2. [Cấu Hình Quyền Truy Cập Mạng (Cleartext HTTP / ATS)](#2-cấu-hình-quyền-truy-cập-mạng-cleartext-http--ats)
3. [Gói Thư Viện Khuyến Nghị (Dependencies)](#3-gói-thư-viện-khuyến-nghị-dependencies)
4. [Xây Dựng HTTP Client (Dio Interceptor & Tự Động Refresh Token)](#4-xây-dựng-http-client-dio-interceptor--tự-động-refresh-token)
5. [Tích Hợp Realtime SignalR (`signalr_netcore`)](#5-tích-hợp-realtime-signalr-signalr_netcore)
6. [Tài Khoản Seed Data & Quy Trình Kiểm Thử Nhanh](#6-tài-khoản-seed-data--quy-trình-kiểm-thử-nhanh)

---

## 1. Ma Trận Địa Chỉ IP Mạng (Network & IP Configuration)

Trong môi trường Mobile, **`localhost` trỏ vào chính chiếc điện thoại/máy ảo**, KHÔNG PHẢI là máy tính chạy backend. Bạn cần cấu hình `baseUrl` tương ứng với từng thiết bị:

| Môi trường chạy Flutter | Địa chỉ Base URL API Gateway | Địa chỉ SignalR Hub Chat |
| :--- | :--- | :--- |
| **Android Studio Emulator** | `http://10.0.2.2:5000/api/v1` | `http://10.0.2.2:5000/hubs/chat` |
| **Genymotion Emulator** | `http://10.0.3.2:5000/api/v1` | `http://10.0.3.2:5000/hubs/chat` |
| **iOS Simulator** | `http://127.0.0.1:5000/api/v1` | `http://127.0.0.1:5000/hubs/chat` |
| **Thiết bị thật (Cùng WiFi)** | `http://<IP_LAN_MAY_TINH>:5000/api/v1` *(ví dụ: `http://192.168.1.15:5000/api/v1`)* | `http://<IP_LAN_MAY_TINH>:5000/hubs/chat` |

> 💡 **Mẹo tìm IP LAN máy tính**: Mở PowerShell gõ `ipconfig`, tìm dòng **`IPv4 Address`** của mạng Wi-Fi (ví dụ: `192.168.1.x`).

---

## 2. Cấu Hình Quyền Truy Cập Mạng (Cleartext HTTP / ATS)

Mặc định Android 9+ và iOS chặn các kết nối HTTP không bảo mật (Non-SSL). Khi chạy Local Development với HTTP Port 5000, bạn **bắt buộc** phải cấu hình 2 mục sau:

### 2.1. Cấu hình Android (`android/app/src/main/AndroidManifest.xml`)
Thêm `android:usesCleartextTraffic="true"` vào thẻ `<application>`:

```xml
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    <!-- Thêm quyền truy cập Internet -->
    <uses-permission android:name="android.permission.INTERNET"/>

    <application
        android:label="F-Alumni Connect"
        android:name="${applicationName}"
        android:icon="@mipmap/ic_launcher"
        android:usesCleartextTraffic="true"> <!-- BẮT BUỘC ĐỂ KẾT NỐI HTTP LOCALHOST -->
        ...
    </application>
</manifest>
```

### 2.2. Cấu hình iOS (`ios/Runner/Info.plist`)
Thêm cấu hình bỏ qua ATS cho local IP:

```xml
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSAllowsArbitraryLoads</key>
    <true/>
</dict>
```

---

## 3. Gói Thư Viện Khuyến Nghị (Dependencies)

Thêm các package sau vào file `pubspec.yaml`:

```yaml
dependencies:
  flutter:
    sdk: flutter
  dio: ^5.7.0                     # HTTP Client mạnh mẽ với Interceptors
  flutter_secure_storage: ^9.2.2  # Lưu AccessToken và RefreshToken an toàn (Keystore/Keychain)
  signalr_netcore: ^1.4.3         # Client kết nối ASP.NET Core SignalR WebSockets
  flutter_riverpod: ^2.6.1        # State management (hoặc Provider/Bloc tùy bạn chọn)
```

Chạy lệnh cài đặt:
```bash
flutter pub get
```

---

## 4. Xây Dựng HTTP Client (Dio Interceptor & Tự Động Refresh Token)

Tạo file `lib/core/network/api_client.dart`:

```dart
import 'dart:io';
import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class ApiClient {
  static final ApiClient _instance = ApiClient._internal();
  factory ApiClient() => _instance;

  late final Dio dio;
  final FlutterSecureStorage _storage = const FlutterSecureStorage();

  ApiClient._internal() {
    // Tự động nhận diện Android Emulator vs iOS Simulator vs Thiết bị thật
    String baseUrl = 'http://10.0.2.2:5000/api/v1'; // Mặc định Android Emulator
    if (Platform.isIOS) {
      baseUrl = 'http://127.0.0.1:5000/api/v1';
    }

    dio = Dio(
      BaseOptions(
        baseUrl: baseUrl,
        connectTimeout: const Duration(seconds: 15),
        receiveTimeout: const Duration(seconds: 15),
        headers: {'Content-Type': 'application/json'},
      ),
    );

    dio.interceptors.add(
      QueuedInterceptorsWrapper(
        onRequest: (options, handler) async {
          final accessToken = await _storage.read(key: 'access_token');
          if (accessToken != null && accessToken.isNotEmpty) {
            options.headers['Authorization'] = 'Bearer $accessToken';
          }
          return handler.next(options);
        },
        onError: (DioException error, handler) async {
          // Bắt lỗi 401 Unauthorized để tự động Refresh Token
          if (error.response?.statusCode == 401) {
            final refreshToken = await _storage.read(key: 'refresh_token');
            if (refreshToken != null) {
              try {
                // Tạo instance Dio độc lập để tránh vòng lặp interceptor
                final refreshDio = Dio(BaseOptions(baseUrl: dio.options.baseUrl));
                final res = await refreshDio.post('/auth/refresh-token', data: {
                  'refreshToken': refreshToken,
                });

                final newAccessToken = res.data['accessToken'];
                final newRefreshToken = res.data['refreshToken'];

                await _storage.write(key: 'access_token', value: newAccessToken);
                await _storage.write(key: 'refresh_token', value: newRefreshToken);

                // Thử lại request ban đầu với AccessToken mới
                error.requestOptions.headers['Authorization'] = 'Bearer $newAccessToken';
                final retryResponse = await dio.fetch(error.requestOptions);
                return handler.resolve(retryResponse);
              } catch (_) {
                // Hết hạn cả refresh token -> Xóa sạch session
                await _storage.deleteAll();
              }
            }
          }
          return handler.next(error);
        },
      ),
    );
  }
}
```

---

## 5. Tích Hợp Realtime SignalR (`signalr_netcore`)

Tạo file `lib/core/network/signalr_service.dart`:

```dart
import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:signalr_netcore/signalr_netcore.dart';

class SignalRService {
  static final SignalRService _instance = SignalRService._internal();
  factory SignalRService() => _instance;

  HubConnection? _chatHubConnection;
  final FlutterSecureStorage _storage = const FlutterSecureStorage();

  SignalRService._internal();

  Future<void> initChatHub({
    required Function(Map<String, dynamic> message) onReceiveMessage,
    required Function(String convId, bool isTyping) onUserTyping,
  }) async {
    String hubUrl = 'http://10.0.2.2:5000/hubs/chat';
    if (Platform.isIOS) {
      hubUrl = 'http://127.0.0.1:5000/hubs/chat';
    }

    _chatHubConnection = HubConnectionBuilder()
        .withUrl(
          hubUrl,
          options: HttpConnectionOptions(
            accessTokenFactory: () async {
              return await _storage.read(key: 'access_token') ?? '';
            },
          ),
        )
        .withAutomaticReconnect()
        .build();

    // 1. Lắng nghe tin nhắn mới
    _chatHubConnection?.on('ReceiveMessage', (arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final data = arguments[0] as Map<String, dynamic>;
        onReceiveMessage(data);
      }
    });

    // 2. Lắng nghe trạng thái đang gõ
    _chatHubConnection?.on('UserTyping', (arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final data = arguments[0] as Map<String, dynamic>;
        onUserTyping(data['conversationId'], data['isTyping']);
      }
    });

    try {
      await _chatHubConnection?.start();
      debugPrint('[SignalR] Connected to Chat Hub successfully');
    } catch (e) {
      debugPrint('[SignalR] Connection error: $e');
    }
  }

  // Gửi tin nhắn tức thì
  Future<void> sendMessage(String conversationId, String content) async {
    if (_chatHubConnection?.state == HubConnectionState.Connected) {
      await _chatHubConnection?.invoke('SendMessage', args: [conversationId, content]);
    }
  }

  // Báo trạng thái gõ phím
  Future<void> sendTyping(String conversationId, bool isTyping) async {
    if (_chatHubConnection?.state == HubConnectionState.Connected) {
      await _chatHubConnection?.invoke('SendTyping', args: [conversationId, isTyping]);
    }
  }

  Future<void> stop() async {
    await _chatHubConnection?.stop();
  }
}
```

---

## 6. Tài Khoản Seed Data & Quy Trình Kiểm Thử Nhanh

Mật khẩu chung cho tất cả tài khoản: **`Password@123`**

### Kịch bản kiểm thử 2 người chat trên Mobile & Web:
1. **Trên Flutter App**: Đăng nhập bằng `nguyenvana@fpt.edu.vn`.
2. **Trên Trình duyệt Web (Chrome)**: Đăng nhập bằng `tranthib@fpt.edu.vn`.
3. Vào danh sách chat, chọn cuộc trò chuyện mẫu có sẵn:
   - **Conversation ID**: `99999999-9999-9999-9999-999999999999`
4. Gửi tin nhắn từ Flutter $\rightarrow$ Web lập tức hiển thị qua SignalR.
5. Gõ phím trên Web $\rightarrow$ Flutter hiển thị thanh ba chấm "Trần Thị B đang soạn tin nhắn...".

### Kiểm thử tính năng AI Chatbot & Gợi ý Mentor:
- Gửi `POST /api/v1/ai/chat` với body:
  ```json
  { "question": "Điều kiện tốt nghiệp và làm đồ án Capstone SE là gì?" }
  ```
  $\rightarrow$ AI trả lời chính xác dựa trên RAG tri thức nội bộ FPTU.
- Gửi `POST /api/v1/ai/mentors/recommend` với body:
  ```json
  {
    "major": "SE",
    "targetSkills": ["C#", ".NET", "Microservices"],
    "careerGoal": "Backend Engineer"
  }
  ```
  $\rightarrow$ Trả về Mentor Nguyễn Hữu Trí (Alumni K13 - Tech Lead FPT Software) với Match Score cao nhất!
