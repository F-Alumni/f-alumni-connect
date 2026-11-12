# Forum — Diagrams

Source snapshot: 2026-11-11.

[Offline viewer](Forum-Diagrams.html) · [Editable diagrams.net](Forum-Diagrams.drawio)

- Forum DB theo appsettings: FAlumni_Forum_Db. ERD gồm đủ 11 bảng trong DbContext, bao gồm SampleItems.
- Kafka publish dùng fire-and-forget; lỗi được log. SignalR broadcast được await trước HTTP 201.
- Categories chưa liên kết FK tới Posts. Reports dùng TargetType + TargetId, không phải FK vật lý.
- Comment.ParentCommentId nullable, delete Restrict; các FK còn lại delete Cascade.

ERD reflects current EF Core entities and Fluent API; the live SQL Server schema has not been inspected.

## 01 Sequence

![01 Sequence](Forum-01.svg)

## 02 Class - Application

![02 Class - Application](Forum-02.svg)

## 03 Class - Domain

![03 Class - Domain](Forum-03.svg)

```mermaid
classDiagram
    direction TB
    class Category {
        +Guid Id
        +string Name
        +string Slug
        +string Description
        +DateTime CreatedAt
    }
    class Event {
        +Guid Id
        +Guid CreatorId
        +string CreatorName
        +string CreatorAvatarUrl
        +string Title
        +string Description
        +string BannerUrl
        +string Location
        +string Campus
        +DateTime StartTime
        +DateTime EndTime
        +int? MaxAttendees
        +int GoingCount
        +int InterestedCount
        +DateTime CreatedAt
        +ICollection~EventAttendee~ Attendees
    }
    class EventAttendee {
        +Guid Id
        +Guid EventId
        +Guid UserId
        +string UserName
        +string UserAvatarUrl
        +string Status
        +DateTime RegisteredAt
        +Event? Event
    }
    class Comment {
        +Guid Id
        +Guid PostId
        +Guid AuthorId
        +string AuthorName
        +string AuthorAvatarUrl
        +string Content
        +Guid? ParentCommentId
        +bool IsAcceptedAnswer
        +DateTime CreatedAt
        +DateTime? UpdatedAt
        +Post? Post
        +Comment? ParentComment
        +ICollection~Comment~ Replies
    }
    class Reaction {
        +Guid Id
        +Guid PostId
        +Guid UserId
        +string ReactionType
        +DateTime CreatedAt
        +Post? Post
    }
    class Bookmark {
        +Guid Id
        +Guid PostId
        +Guid UserId
        +DateTime CreatedAt
        +Post? Post
    }
    class Report {
        +Guid Id
        +string TargetType
        +Guid TargetId
        +Guid ReporterId
        +string ReporterName
        +string Reason
        +string? Details
        +string Status
        +DateTime CreatedAt
    }
    class PostImage {
        +Guid Id
        +Guid PostId
        +string ImageUrl
        +int Order
        +Post? Post
    }
    class PostTag {
        +Guid Id
        +Guid PostId
        +string TagName
        +Post? Post
    }
    class Post {
        +Guid Id
        +Guid AuthorId
        +string AuthorName
        +string AuthorAvatarUrl
        +string Title
        +string Content
        +string PostType
        +string Major
        +string Campus
        +bool IsAnswered
        +int ViewCount
        +int LikeCount
        +int CommentCount
        +bool IsPinned
        +bool IsHidden
        +DateTime CreatedAt
        +DateTime? UpdatedAt
        +ICollection~PostImage~ Images
        +ICollection~PostTag~ Tags
        +ICollection~Comment~ Comments
        +ICollection~Reaction~ Reactions
        +ICollection~Bookmark~ Bookmarks
    }
    class SampleItem {
        +Guid Id
        +string Name
        +string Description
        +bool IsActive
        +DateTime CreatedAtUtc
    }
    Post "1" --> "0..*" PostImage : PostId
    Post "1" --> "0..*" PostTag : PostId
    Post "1" --> "0..*" Comment : PostId
    Post "1" --> "0..*" Reaction : PostId
    Post "1" --> "0..*" Bookmark : PostId
    Comment "0..1" --> "0..*" Comment : ParentCommentId
    Event "1" --> "0..*" EventAttendee : EventId
```

## 04 ERD

![04 ERD](Forum-04.svg)

```mermaid
erDiagram
    Posts ||--o{ PostImages : "PostId"
    Posts ||--o{ PostTags : "PostId"
    Posts ||--o{ Comments : "PostId"
    Posts ||--o{ Reactions : "PostId"
    Posts ||--o{ Bookmarks : "PostId"
    Comments |o--o{ Comments : "ParentCommentId"
    Events ||--o{ EventAttendees : "EventId"
    Categories {
        uniqueidentifier Id PK
        nvarchar_150_ Name
        nvarchar_150_ Slug
        nvarchar_max_ Description
        datetime2 CreatedAt
    }
    Events {
        uniqueidentifier Id PK
        uniqueidentifier CreatorId
        nvarchar_200_ CreatorName
        nvarchar_max_ CreatorAvatarUrl
        nvarchar_300_ Title
        nvarchar_max_ Description
        nvarchar_max_ BannerUrl
        nvarchar_300_ Location
        nvarchar_50_ Campus
        datetime2 StartTime
        datetime2 EndTime
        int MaxAttendees "NULL"
        int GoingCount
        int InterestedCount
        datetime2 CreatedAt
    }
    EventAttendees {
        uniqueidentifier Id PK
        uniqueidentifier EventId FK
        uniqueidentifier UserId
        nvarchar_200_ UserName
        nvarchar_max_ UserAvatarUrl
        nvarchar_30_ Status
        datetime2 RegisteredAt
    }
    Comments {
        uniqueidentifier Id PK
        uniqueidentifier PostId FK
        uniqueidentifier AuthorId
        nvarchar_200_ AuthorName
        nvarchar_max_ AuthorAvatarUrl
        nvarchar_max_ Content
        uniqueidentifier ParentCommentId FK "NULL"
        bit IsAcceptedAnswer
        datetime2 CreatedAt
        datetime2 UpdatedAt "NULL"
    }
    Reactions {
        uniqueidentifier Id PK
        uniqueidentifier PostId FK
        uniqueidentifier UserId
        nvarchar_30_ ReactionType
        datetime2 CreatedAt
    }
    Bookmarks {
        uniqueidentifier Id PK
        uniqueidentifier PostId FK
        uniqueidentifier UserId
        datetime2 CreatedAt
    }
    Reports {
        uniqueidentifier Id PK
        nvarchar_30_ TargetType
        uniqueidentifier TargetId
        uniqueidentifier ReporterId
        nvarchar_max_ ReporterName
        nvarchar_200_ Reason
        nvarchar_max_ Details "NULL"
        nvarchar_30_ Status
        datetime2 CreatedAt
    }
    PostImages {
        uniqueidentifier Id PK
        uniqueidentifier PostId FK
        nvarchar_1000_ ImageUrl
        int Order
    }
    PostTags {
        uniqueidentifier Id PK
        uniqueidentifier PostId FK
        nvarchar_100_ TagName
    }
    Posts {
        uniqueidentifier Id PK
        uniqueidentifier AuthorId
        nvarchar_200_ AuthorName
        nvarchar_max_ AuthorAvatarUrl
        nvarchar_300_ Title
        nvarchar_max_ Content
        nvarchar_50_ PostType
        nvarchar_50_ Major
        nvarchar_50_ Campus
        bit IsAnswered
        int ViewCount
        int LikeCount
        int CommentCount
        bit IsPinned
        bit IsHidden
        datetime2 CreatedAt
        datetime2 UpdatedAt "NULL"
    }
    SampleItems {
        uniqueidentifier Id PK
        nvarchar_100_ Name
        nvarchar_500_ Description
        bit IsActive
        datetime2 CreatedAtUtc
    }
```

## Source references

- `ForumService.Api/Controllers/PostsController.cs`
- `ForumService.Services/Implementations/PostService.cs`
- `ForumService.Services/Implementations/KafkaProducerService.cs`
- `ForumService.Repositories/Implementations/PostRepository.cs`
- `ForumService.Repositories/Entities/*.cs`
- `ForumService.Repositories/Configurations/ForumConfiguration.cs`
- `ForumService.Repositories/Context/ForumServiceDbContext.cs`
