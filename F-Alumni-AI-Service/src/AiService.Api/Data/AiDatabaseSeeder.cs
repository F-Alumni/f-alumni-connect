using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AiService.Repositories.Context;
using AiService.Repositories.Entities;

namespace AiService.Api.Data;

/// <summary>
/// Seeds realistic AI data (MentorProfiles, Sample RAG Documents/Chunks, AiConversations, AiMessages)
/// matching the shared seed users (UserA: 11111111-..., UserB: 22222222-..., UserC: 33333333-..., Admin: aaaaaaaa-...)
/// so teammates and frontend can immediately test AI Chat, RAG search, and Mentor recommendations out of the box.
/// </summary>
public static class AiDatabaseSeeder
{
    public static readonly Guid AdminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid UserAId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid UserBId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid UserCId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static readonly Guid Mentor1Id = Guid.Parse("77777777-7777-7777-7777-777777777771");
    public static readonly Guid Mentor2Id = Guid.Parse("77777777-7777-7777-7777-777777777772");
    public static readonly Guid Mentor3Id = Guid.Parse("77777777-7777-7777-7777-777777777773");

    public static readonly Guid Conversation1Id = Guid.Parse("88888888-8888-8888-8888-888888888881");

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AiDatabaseSeeder");
        var db = scope.ServiceProvider.GetRequiredService<AiServiceDbContext>();

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

            // 1. Seed Mentor Profiles
            if (!await db.MentorProfiles.AnyAsync())
            {
                var mentors = new List<MentorProfile>
                {
                    new()
                    {
                        Id = Mentor1Id,
                        UserId = UserAId,
                        FullName = "Nguyễn Hữu Trí",
                        Email = "tri.nguyen@fpt.com",
                        Major = "SE",
                        CurrentCompany = "FPT Software",
                        JobTitle = "Senior Software Engineer / Tech Lead",
                        Skills = "C#, .NET, Microservices, Cloud, System Architecture, Docker, Kubernetes, SQL",
                        Bio = "Alumni K13 ĐH FPT. Hơn 6 năm kinh nghiệm phát triển hệ thống backend phân tán và mentoring sinh viên năm cuối hướng nghiệp phần mềm.",
                        AvatarUrl = "https://cdn.falumni.local/avatars/mentor1.jpg"
                    },
                    new()
                    {
                        Id = Mentor2Id,
                        UserId = UserBId,
                        FullName = "Lê Hoàng Yến",
                        Email = "yen.le@vinai.io",
                        Major = "AI",
                        CurrentCompany = "VinAI Research",
                        JobTitle = "AI Research Engineer",
                        Skills = "Python, PyTorch, LLM, Computer Vision, RAG, NLP, Deep Learning, MLOps",
                        Bio = "Alumni K14 ĐH FPT. Chuyên sâu về Generative AI, RAG và xử lý ngôn ngữ tự nhiên. Sẵn sàng hỗ trợ định hướng đồ án Capstone AI và nghiên cứu khoa học.",
                        AvatarUrl = "https://cdn.falumni.local/avatars/mentor2.jpg"
                    },
                    new()
                    {
                        Id = Mentor3Id,
                        UserId = UserCId,
                        FullName = "Phạm Quang Minh",
                        Email = "minh.pham@viettelcyber.vn",
                        Major = "IA",
                        CurrentCompany = "Viettel Cyber Security",
                        JobTitle = "Security Consultant",
                        Skills = "Penetration Testing, OWASP, Cloud Security, SIEM, Incident Response, Network Security",
                        Bio = "Alumni K15 ĐH FPT. Đạt nhiều giải thưởng CTF và có kinh nghiệm bảo mật hệ thống ngân hàng, fintech.",
                        AvatarUrl = "https://cdn.falumni.local/avatars/mentor3.jpg"
                    }
                };

                await db.MentorProfiles.AddRangeAsync(mentors);
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} mentor profiles successfully.", mentors.Count);
            }

            // 2. Seed Initial RAG Documents & Chunks (if empty)
            if (!await db.RagDocuments.AnyAsync())
            {
                var doc1 = new RagDocument
                {
                    Id = Guid.Parse("90000000-0000-0000-0000-000000000001"),
                    Title = "Quy định Học vụ và Đào tạo Đại học FPT",
                    SourcePath = "regulations/academic-regulations-2024.docx",
                    DocumentType = "Regulations",
                    ProgramCode = "SE",
                    Campus = "All",
                    CreatedAt = DateTime.UtcNow
                };

                var chunk1_1 = new RagChunk
                {
                    Id = Guid.Parse("91000000-0000-0000-0000-000000000001"),
                    DocumentId = doc1.Id,
                    ChunkIdentifier = "fpt-reg-01",
                    Section = "Điều kiện tốt nghiệp và Capstone Project",
                    Content = "Sinh viên ngành CNTT (Kỹ thuật phần mềm, Trí tuệ nhân tạo, An toàn thông tin) phải hoàn thành tối thiểu 144 tín chỉ, đạt chứng chỉ tiếng Anh IELTS 6.0 hoặc tương đương, hoàn thành kỳ thực tập doanh nghiệp (OJT) và bảo vệ thành công đồ án tốt nghiệp Capstone Project (SEP490/AIP490/IAP490) với điểm số từ 5.0 trở lên.",
                    Keywords = "tốt nghiệp capstone ojt tín chỉ ielts sep490 aip490 iap490"
                };

                var chunk1_2 = new RagChunk
                {
                    Id = Guid.Parse("91000000-0000-0000-0000-000000000002"),
                    DocumentId = doc1.Id,
                    ChunkIdentifier = "fpt-reg-02",
                    Section = "Quy định kỳ thực tập doanh nghiệp (OJT)",
                    Content = "Kỳ thực tập On-the-Job Training (OJT) diễn ra vào học kỳ 6 của chương trình đào tạo. Sinh viên làm việc toàn thời gian tại các doanh nghiệp đối tác như FPT Software, Viettel, VNPT hoặc các công ty công nghệ trong và ngoài nước trong vòng 4-6 tháng để tích lũy kinh nghiệm thực tế.",
                    Keywords = "ojt thực tập doanh nghiệp học kỳ 6 fpt software viettel vnpt"
                };

                doc1.Chunks.Add(chunk1_1);
                doc1.Chunks.Add(chunk1_2);

                var doc2 = new RagDocument
                {
                    Id = Guid.Parse("90000000-0000-0000-0000-000000000002"),
                    Title = "Cẩm nang Tiếng lóng và Văn hóa Sinh viên FPT",
                    SourcePath = "culture/fptu-culture-slang.docx",
                    DocumentType = "Slang",
                    Campus = "All",
                    CreatedAt = DateTime.UtcNow
                };

                var chunk2_1 = new RagChunk
                {
                    Id = Guid.Parse("91000000-0000-0000-0000-000000000003"),
                    DocumentId = doc2.Id,
                    ChunkIdentifier = "fpt-culture-01",
                    Section = "Các thuật ngữ đặc trưng: Cóc, 7 Kỳ chuyên ngành, Võ Vovinam",
                    Content = "Tại FPT University, sinh viên thường gọi nhau là 'Cóc' (lấy cảm hứng từ linh vật Cóc của trường). '7 Kỳ' là thời gian đào tạo chuyên ngành sau khi hoàn thành giai đoạn tiếng Anh dự bị (Little UK). Môn học 'Võ Vovinam' và 'Nhạc cụ dân tộc' là hai môn thể chất và nghệ thuật đặc trưng bắt buộc đối với tất cả sinh viên FPTU.",
                    Keywords = "cóc tiếng lóng vovinam nhạc cụ dân tộc 7 kỳ linh vật little uk"
                };

                doc2.Chunks.Add(chunk2_1);

                var doc3 = new RagDocument
                {
                    Id = Guid.Parse("90000000-0000-0000-0000-000000000003"),
                    Title = "Chương trình Kết nối FPT Alumni & Học bổng Mentorship",
                    SourcePath = "alumni/alumni-network-mentorship.docx",
                    DocumentType = "Curriculum",
                    Campus = "All",
                    CreatedAt = DateTime.UtcNow
                };

                var chunk3_1 = new RagChunk
                {
                    Id = Guid.Parse("91000000-0000-0000-0000-000000000004"),
                    DocumentId = doc3.Id,
                    ChunkIdentifier = "fpt-alumni-01",
                    Section = "Mạng lưới Cựu sinh viên và Học bổng Alumni Wings",
                    Content = "FPT Alumni Connect kết nối mạng lưới hơn 50,000 cựu sinh viên FPT trên toàn cầu. Hàng năm, mạng lưới cựu sinh viên tài trợ các suất học bổng 'Alumni Wings' và cung cấp chương trình 1-on-1 Mentorship dành riêng cho sinh viên năm 3, năm 4 định hướng nghề nghiệp và tham gia các dự án nghiên cứu.",
                    Keywords = "học bổng alumni wings mentorship cựu sinh viên kết nối tài trợ"
                };

                doc3.Chunks.Add(chunk3_1);

                await db.RagDocuments.AddRangeAsync(new[] { doc1, doc2, doc3 });
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded {DocCount} RAG documents with {ChunkCount} chunks successfully.", 3, 4);
            }

            // 3. Seed Sample Conversation & Messages for UserA
            if (!await db.AiConversations.AnyAsync(c => c.Id == Conversation1Id))
            {
                var conv1 = new AiConversation
                {
                    Id = Conversation1Id,
                    UserId = UserAId,
                    Title = "Tư vấn lộ trình chuyên ngành Kỹ thuật Phần mềm (SE)",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    LastMessageAt = DateTime.UtcNow.AddDays(-1)
                };

                var msg1 = new AiMessage
                {
                    Id = Guid.Parse("92000000-0000-0000-0000-000000000001"),
                    ConversationId = Conversation1Id,
                    Role = "user",
                    Content = "Chào AI, mình đang phân vân giữa chọn hướng Web/Backend hay Mobile. Ngành SE tại FPTU có những môn học cốt lõi nào hỗ trợ tốt nhất?",
                    CitationsJson = "[]",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                };

                var msg2 = new AiMessage
                {
                    Id = Guid.Parse("92000000-0000-0000-0000-000000000002"),
                    ConversationId = Conversation1Id,
                    Role = "assistant",
                    Content = "Chào bạn! Trong chương trình đào tạo ngành Kỹ thuật Phần mềm (SE) tại Đại học FPT, các môn học nền tảng và cốt lõi bao gồm PRN211/PRN221 (.NET C#), SWP391 (Software Project), PRM392 (Mobile Application Development), và SWD392 (Software Architecture & Design). Nếu bạn hướng tới Web/Backend, bạn nên tập trung vững kiến trúc Microservices, RESTful API, Docker và CSDL; nếu theo Mobile thì Flutter/React Native kết hợp Cloud backend là lợi thế lớn.",
                    CitationsJson = "[{\"documentTitle\":\"Quy định Học vụ và Đào tạo Đại học FPT\",\"section\":\"Điều kiện tốt nghiệp và Capstone Project\"}]",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                };

                conv1.Messages.Add(msg1);
                conv1.Messages.Add(msg2);

                await db.AiConversations.AddAsync(conv1);
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded sample AI conversation and messages for UserA successfully.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AiDatabaseSeeder encountered an error or was skipped: {Message}", ex.Message);
        }
    }
}
