# 🏛️ Sơ Đồ Kiến Trúc Hệ Thống Chi Tiết — Forum Service (`f-alumni-forum-service`)

> **Người thực hiện**: Vinh (`vinhmt` — Backend 2)  
> **Cổng dịch vụ**: `http://localhost:5003` (HTTPS: `https://localhost:7003`)  
> **Kiến trúc**: 3 tầng chuẩn `.NET 10` (`Api -> Services -> Repositories`), SignalR Real-time Hub (`ForumHub`), Kafka Producer (`post.created.v1`).

---

## 1. Sơ Đồ Kiến Trúc Tổng Thể

![BE2 Architecture](architecture/f-alumni-be2-architecture.svg)

---

## 2. Kiến Trúc 3 Tầng Phân Lập (Clean Layered Architecture)

```mermaid
flowchart TD
    Client["Client (Web React / Mobile Flutter)"] -->|HTTP REST / WSS SignalR| Gateway["YARP Gateway (:5000)"]
    Gateway -->|Route /api/v1/posts, /events, /comments, /hubs/forum| ApiLayer["ForumService.Api (:5003)"]

    subgraph ForumService["Forum Service (.NET 10)"]
        direction TB
        ApiLayer["1. ForumService.Api\n(Controllers, ForumHub, Middlewares, DI)"]
        ServicesLayer["2. ForumService.Services\n(Business Logic, Kafka Producer, DTOs, Mappings)"]
        RepoLayer["3. ForumService.Repositories\n(EF Core DbContext, Repositories, Entities)"]

        ApiLayer -->|Inject Interfaces| ServicesLayer
        ServicesLayer -->|Inject Interfaces| RepoLayer
    end

    RepoLayer -->|TCP 1433| SqlDb[("FAlumni_Forum_Db\n(SQL Server 2022)")]
    ServicesLayer -->|Produce Event\npost.created.v1| KafkaBroker[("Apache Kafka (:29092)")]
    ApiLayer -->|Real-time Push| SignalRClients["Web / Mobile Clients\n(ReceiveNewPost, ReceiveComment, ReceiveReaction, ReceiveNewEvent, ReceiveEventRsvp)"]
```

---

## 3. Quy Trình Thời Gian Thực SignalR & Sự Kiện Bất Đồng Bộ Kafka

### 3.1 Luồng Đăng Bài Viết & Kafka Event Streaming (Sequence Diagram)
```mermaid
sequenceDiagram
    autonumber
    actor User as Sinh viên / Cựu sinh viên
    participant Gateway as YARP Gateway (:5000)
    participant ForumApi as PostsController (:5003)
    participant ForumSvc as PostService
    participant Repo as PostRepository
    participant DB as FAlumni_Forum_Db
    participant Kafka as Apache Kafka Broker
    participant Hub as ForumHub (/hubs/forum)

    User->>Gateway: POST /api/v1/posts (Bearer JWT + CreatePostRequest)
    Gateway->>ForumApi: Chuyển tiếp request
    ForumApi->>ForumSvc: CreatePostAsync(request, authorId, authorName, authorAvatarUrl)
    ForumSvc->>Repo: AddAsync(Post entity)
    Repo->>DB: INSERT INTO Posts, PostImages, PostTags
    DB-->>Repo: Thành công (Post.Id)
    ForumSvc->>Kafka: PublishPostCreatedAsync(PostCreatedEventV1)
    Kafka-->>ForumSvc: Ghi nhận event (Topic: post.created.v1)
    ForumSvc-->>ForumApi: PostResponse
    ForumApi->>Hub: Clients.All.SendAsync("ReceiveNewPost", PostResponse)
    ForumApi-->>Gateway: 201 Created + Location (/api/v1/posts/{id})
    Gateway-->>User: 201 Created (PostResponse payload)
```

### 3.2 Luồng Bình Luận & Tương Tác Thời Gian Thực (Sequence Diagram)
```mermaid
sequenceDiagram
    autonumber
    actor Reader as Người đọc bài
    participant Hub as ForumHub (/hubs/forum)
    participant CommentApi as CommentsController (:5003)
    participant CommentSvc as CommentService
    participant Repo as CommentRepository
    participant DB as FAlumni_Forum_Db

    Reader->>Hub: JoinPostGroup(postId)
    Note over Reader,Hub: Tham gia phòng riêng `post_{postId}`
    Reader->>CommentApi: POST /api/v1/posts/{postId}/comments (CreateCommentRequest)
    CommentApi->>CommentSvc: AddCommentAsync(postId, request, authorId, name, avatar)
    CommentSvc->>Repo: AddAsync(Comment entity)
    Repo->>DB: INSERT INTO Comments & UPDATE Posts SET CommentCount = CommentCount + 1
    DB-->>Repo: Thành công (Comment.Id)
    CommentSvc-->>CommentApi: CommentResponse
    CommentApi->>Hub: Clients.Group($"post_{postId}").SendAsync("ReceiveComment", CommentResponse)
    Hub-->>Reader: Nhận thông báo bình luận tức thì trên giao diện
    CommentApi-->>Reader: 201 Created (CommentResponse payload)
```

### 3.3 Luồng Tạo Sự Kiện Mạng Xã Hội & Đăng Ký RSVP (Sequence Diagram)
```mermaid
sequenceDiagram
    autonumber
    actor Host as Người tạo sự kiện
    actor Guest as Người tham gia
    participant Hub as ForumHub (/hubs/forum)
    participant EventApi as EventsController (:5003)
    participant EventSvc as EventService
    participant DB as FAlumni_Forum_Db

    Host->>Hub: JoinEventsGroup()
    Guest->>Hub: JoinEventsGroup()
    Note over Host,Guest,Hub: Tham gia room `events_channel`

    Host->>EventApi: POST /api/v1/events (CreateEventRequest)
    EventApi->>EventSvc: CreateEventAsync(...)
    EventSvc->>DB: INSERT INTO Events, EventAttendees (Host auto Going)
    EventSvc-->>EventApi: EventResponse
    EventApi->>Hub: Clients.Group("events_channel").SendAsync("ReceiveNewEvent", EventResponse)
    Hub-->>Guest: Hiển thị sự kiện mới tức thì trên bảng sự kiện

    Guest->>EventApi: POST /api/v1/events/{id}/rsvp (Status: Going/Interested/NotGoing)
    EventApi->>EventSvc: RsvpEventAsync(...)
    EventSvc->>DB: Upsert EventAttendees & cập nhật GoingCount/InterestedCount
    EventApi->>Hub: Clients.Group("events_channel").SendAsync("ReceiveEventRsvp", payload)
    Hub-->>Host: Cập nhật số lượng người tham gia thời gian thực
```
