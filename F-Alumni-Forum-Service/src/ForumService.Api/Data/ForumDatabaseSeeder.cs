using Microsoft.EntityFrameworkCore;
using ForumService.Repositories.Context;
using ForumService.Repositories.Entities;

namespace ForumService.Api.Data;

/// <summary>
/// Seeds realistic forum data (Categories, Posts, Comments, Reactions, Events, Attendees)
/// matching the shared seed users (UserA: 11111111-..., UserB: 22222222-..., UserC: 33333333-..., Admin: aaaaaaaa-...)
/// so teammates and frontend can immediately test Forum, Community, and Events out of the box.
/// </summary>
public static class ForumDatabaseSeeder
{
    public static readonly Guid AdminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid UserAId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid UserBId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid UserCId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static readonly Guid Post1Id = Guid.Parse("44444444-4444-4444-4444-444444444441");
    public static readonly Guid Post2Id = Guid.Parse("44444444-4444-4444-4444-444444444442");
    public static readonly Guid Event1Id = Guid.Parse("66666666-6666-6666-6666-666666666661");

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ForumDatabaseSeeder");
        var db = scope.ServiceProvider.GetRequiredService<ForumServiceDbContext>();

        try
        {
            if (db.Database.IsRelational())
            {
                await db.Database.MigrateAsync();
            }
            else
            {
                await db.Database.EnsureCreatedAsync();
            }

            // 1. Seed Categories
            if (!await db.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new()
                    {
                        Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                        Name = "Kỹ thuật phần mềm",
                        Slug = "software-engineering",
                        Description = "Thảo luận về kiến trúc phần mềm, công nghệ .NET, Cloud, CI/CD và phát triển hệ thống phân tán.",
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                        Name = "Trí tuệ nhân tạo",
                        Slug = "artificial-intelligence",
                        Description = "Chia sẻ về Machine Learning, Deep Learning, Generative AI, Computer Vision & NLP.",
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                        Name = "An toàn thông tin",
                        Slug = "information-assurance",
                        Description = "Bảo mật hệ thống, an ninh mạng, kiểm thử xâm nhập và mật mã học.",
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
                        Name = "Định hướng nghề nghiệp",
                        Slug = "career-guidance",
                        Description = "Kinh nghiệm phỏng vấn, thực tập, làm việc tại doanh nghiệp và cơ hội việc làm alumni.",
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
                        Name = "Góc đời sống & Kết nối",
                        Slug = "alumni-life",
                        Description = "Giao lưu, thể thao, hội cựu sinh viên FPT và các hoạt động cộng đồng kết nối.",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                await db.Categories.AddRangeAsync(categories);
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} forum categories successfully.", categories.Count);
            }

            // 2. Seed Posts
            if (!await db.Posts.AnyAsync(p => p.Id == Post1Id))
            {
                var now = DateTime.UtcNow;
                var comment1Id = Guid.Parse("55555555-5555-5555-5555-555555555551");

                var post1 = new Post
                {
                    Id = Post1Id,
                    AuthorId = UserAId,
                    AuthorName = "Nguyen Van A",
                    AuthorAvatarUrl = string.Empty,
                    Title = "Chia sẻ kinh nghiệm thiết kế kiến trúc Microservices với .NET 9 và Apache Kafka",
                    Content = "Chào các bạn sinh viên và cựu sinh viên FPT! Hôm nay mình muốn chia sẻ một số kinh nghiệm thực chiến khi xây dựng hệ thống phân tán chịu tải cao bằng .NET 9, EF Core và Apache Kafka. Cụ thể gồm cách tách biệt Schema/Database hoàn toàn giữa các service, quản lý schema migration tự động và xử lý event idempotency.",
                    PostType = "Sharing",
                    Major = "SE",
                    Campus = "FU-HCM",
                    IsAnswered = false,
                    ViewCount = 128,
                    LikeCount = 1,
                    CommentCount = 2,
                    IsPinned = true,
                    IsHidden = false,
                    CreatedAt = now.AddDays(-2),
                    Tags = new List<PostTag>
                    {
                        new() { Id = Guid.NewGuid(), TagName = "microservices" },
                        new() { Id = Guid.NewGuid(), TagName = "dotnet" },
                        new() { Id = Guid.NewGuid(), TagName = "kafka" }
                    },
                    Comments = new List<Comment>
                    {
                        new()
                        {
                            Id = comment1Id,
                            AuthorId = UserBId,
                            AuthorName = "Tran Thi B",
                            AuthorAvatarUrl = string.Empty,
                            Content = "Bài viết rất hữu ích anh A ơi! Cho em hỏi phần Kafka schema registry bên anh quản lý thế nào ạ?",
                            CreatedAt = now.AddDays(-1).AddHours(2)
                        },
                        new()
                        {
                            Id = Guid.Parse("55555555-5555-5555-5555-555555555552"),
                            AuthorId = UserAId,
                            AuthorName = "Nguyen Van A",
                            AuthorAvatarUrl = string.Empty,
                            ParentCommentId = comment1Id,
                            Content = "Bên anh dùng Schema Registry kết hợp JSON Schema validation tại pipeline CI/CD nha B.",
                            CreatedAt = now.AddDays(-1).AddHours(4)
                        }
                    },
                    Reactions = new List<Reaction>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            UserId = UserBId,
                            ReactionType = "Like",
                            CreatedAt = now.AddDays(-1)
                        }
                    }
                };

                var post2 = new Post
                {
                    Id = Post2Id,
                    AuthorId = UserBId,
                    AuthorName = "Tran Thi B",
                    AuthorAvatarUrl = string.Empty,
                    Title = "Cơ hội thực tập và làm việc AI Research / LLM Engineer tại FPT Software 2026",
                    Content = "Team AI Research của FPT Software đang mở đợt tuyển dụng thực tập sinh và kỹ sư AI làm việc trực tiếp với các mô hình Foundation Models và Agentic Workflows. Bạn nào quan tâm inbox hoặc gửi CV trực tiếp qua email mình nhé!",
                    PostType = "Recruitment",
                    Major = "AI",
                    Campus = "FU-HL",
                    IsAnswered = false,
                    ViewCount = 256,
                    LikeCount = 1,
                    CommentCount = 0,
                    IsPinned = false,
                    IsHidden = false,
                    CreatedAt = now.AddDays(-1),
                    Tags = new List<PostTag>
                    {
                        new() { Id = Guid.NewGuid(), TagName = "ai" },
                        new() { Id = Guid.NewGuid(), TagName = "llm" },
                        new() { Id = Guid.NewGuid(), TagName = "recruitment" }
                    },
                    Reactions = new List<Reaction>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            UserId = UserCId,
                            ReactionType = "Like",
                            CreatedAt = now.AddHours(-12)
                        }
                    }
                };

                await db.Posts.AddRangeAsync(post1, post2);
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded demo forum posts successfully.");
            }

            // 3. Seed Events
            if (!await db.Events.AnyAsync(e => e.Id == Event1Id))
            {
                var now = DateTime.UtcNow;
                var event1 = new Event
                {
                    Id = Event1Id,
                    CreatorId = AdminUserId,
                    CreatorName = "FPT Alumni Admin",
                    CreatorAvatarUrl = string.Empty,
                    Title = "FPT Alumni Tech Talk 2026: Kỷ nguyên AI Agents & Cloud-Native Architecture",
                    Description = "Sự kiện giao lưu thường niên kết nối hơn 500 cựu sinh viên FPT làm việc tại các tập đoàn công nghệ hàng đầu trong và ngoài nước. Cùng lắng nghe chia sẻ từ các diễn giả đầu ngành về xu hướng AI và kiến trúc Cloud hiện đại.",
                    BannerUrl = string.Empty,
                    Location = "Hội trường A, Đại học FPT TP. Hồ Chí Minh",
                    Campus = "FU-HCM",
                    StartTime = now.AddDays(14),
                    EndTime = now.AddDays(14).AddHours(4),
                    MaxAttendees = 200,
                    GoingCount = 2,
                    InterestedCount = 1,
                    CreatedAt = now.AddDays(-3),
                    Attendees = new List<EventAttendee>
                    {
                        new()
                        {
                            Id = Guid.NewGuid(),
                            UserId = UserAId,
                            UserName = "Nguyen Van A",
                            UserAvatarUrl = string.Empty,
                            Status = "Going",
                            RegisteredAt = now.AddDays(-2)
                        },
                        new()
                        {
                            Id = Guid.NewGuid(),
                            UserId = UserBId,
                            UserName = "Tran Thi B",
                            UserAvatarUrl = string.Empty,
                            Status = "Going",
                            RegisteredAt = now.AddDays(-1)
                        },
                        new()
                        {
                            Id = Guid.NewGuid(),
                            UserId = UserCId,
                            UserName = "Le Van C",
                            UserAvatarUrl = string.Empty,
                            Status = "Interested",
                            RegisteredAt = now.AddHours(-6)
                        }
                    }
                };

                await db.Events.AddAsync(event1);
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded demo event and attendees successfully.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding forum database.");
        }
    }
}
