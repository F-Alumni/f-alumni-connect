# AI — Diagrams

Source snapshot: 2026-11-11.

[Offline viewer](AI-Diagrams.html) · [Editable diagrams.net](AI-Diagrams.drawio)

- AI DB theo appsettings: FAlumni_AiService_Db; repository fallback dùng FAlumni_AI_Db. ERD gồm 6 bảng.
- RAG dùng keyword/intent scoring, lọc Score > 5 rồi lấy Top 4. Chưa có vector database hoặc embedding.
- Lịch sử hội thoại được lưu/đọc nhưng GenerateWithGeminiAsync hiện chưa chèn history vào prompt.
- IngestForumPostAsync đã có; chưa có Kafka consumer đăng ký trong Program.cs. Đây là điểm tích hợp cần hoàn thiện.
- CitationsJson là JSON, không có FK tới RagChunks. Guest đang dùng chung một GUID theo controller.

ERD reflects current EF Core entities and Fluent API; the live SQL Server schema has not been inspected.

## 01 Sequence

![01 Sequence](AI-01.svg)

## 02 Class - Application

![02 Class - Application](AI-02.svg)

## 03 Class - Domain

![03 Class - Domain](AI-03.svg)

```mermaid
classDiagram
    direction TB
    class RagDocument {
        +Guid Id
        +string Title
        +string SourcePath
        +string DocumentType
        +string? ProgramCode
        +string Campus
        +DateTime CreatedAt
        +ICollection~RagChunk~ Chunks
    }
    class RagChunk {
        +Guid Id
        +Guid DocumentId
        +string ChunkIdentifier
        +string Section
        +string Content
        +string Keywords
        +RagDocument? Document
    }
    class AiConversation {
        +Guid Id
        +Guid UserId
        +string Title
        +DateTime CreatedAt
        +DateTime LastMessageAt
        +ICollection~AiMessage~ Messages
    }
    class AiMessage {
        +Guid Id
        +Guid ConversationId
        +string Role
        +string Content
        +string CitationsJson
        +DateTime CreatedAt
        +AiConversation? Conversation
    }
    class MentorProfile {
        +Guid Id
        +Guid UserId
        +string FullName
        +string Email
        +string Major
        +string CurrentCompany
        +string JobTitle
        +string Skills
        +string Bio
        +string AvatarUrl
    }
    class SampleItem {
        +Guid Id
        +string Name
        +string Description
        +bool IsActive
        +DateTime CreatedAtUtc
    }
    RagDocument "1" --> "0..*" RagChunk : DocumentId
    AiConversation "1" --> "0..*" AiMessage : ConversationId
```

## 04 ERD

![04 ERD](AI-04.svg)

```mermaid
erDiagram
    RagDocuments ||--o{ RagChunks : "DocumentId"
    AiConversations ||--o{ AiMessages : "ConversationId"
    RagDocuments {
        uniqueidentifier Id PK
        nvarchar_300_ Title
        nvarchar_500_ SourcePath
        nvarchar_50_ DocumentType
        nvarchar_50_ ProgramCode "NULL"
        nvarchar_50_ Campus
        datetime2 CreatedAt
    }
    RagChunks {
        uniqueidentifier Id PK
        uniqueidentifier DocumentId FK
        nvarchar_150_ ChunkIdentifier
        nvarchar_300_ Section
        nvarchar_max_ Content
        nvarchar_max_ Keywords
    }
    AiConversations {
        uniqueidentifier Id PK
        uniqueidentifier UserId
        nvarchar_250_ Title
        datetime2 CreatedAt
        datetime2 LastMessageAt
    }
    AiMessages {
        uniqueidentifier Id PK
        uniqueidentifier ConversationId FK
        nvarchar_20_ Role
        nvarchar_max_ Content
        nvarchar_max_ CitationsJson
        datetime2 CreatedAt
    }
    MentorProfiles {
        uniqueidentifier Id PK
        uniqueidentifier UserId
        nvarchar_200_ FullName
        nvarchar_200_ Email
        nvarchar_50_ Major
        nvarchar_max_ CurrentCompany
        nvarchar_max_ JobTitle
        nvarchar_max_ Skills
        nvarchar_max_ Bio
        nvarchar_max_ AvatarUrl
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

- `AiService.Api/Controllers/AiChatController.cs`
- `AiService.Api/Program.cs`
- `AiService.Api/Workers/RagStartupWorker.cs`
- `AiService.Services/Implementations/AiChatService.cs`
- `AiService.Services/Implementations/RagSearchService.cs`
- `AiService.Services/Implementations/RagIngestionService.cs`
- `AiService.Repositories/Implementations/RagRepository.cs`
- `AiService.Repositories/Entities/AiEntities.cs`
- `AiService.Repositories/Configurations/AiConfiguration.cs`
