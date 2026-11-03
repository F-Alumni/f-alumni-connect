using Microsoft.EntityFrameworkCore;
using AdminService.Repositories.Context;
using AdminService.Repositories.Entities;

namespace AdminService.Api.Data;

/// <summary>
/// Seeds realistic administrative data (Announcements, Audit Logs, Moderation Reports, Settings)
/// matching the shared seed users (Admin: aaaaaaaa-..., UserA: 11111111-..., UserB: 22222222-..., UserC: 33333333-...)
/// so teammates and frontend can test Admin Portal immediately out of the box.
/// </summary>
public static class AdminDatabaseSeeder
{
    public static readonly Guid AdminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid UserAId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid UserBId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid UserCId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AdminDatabaseSeeder");
        var db = scope.ServiceProvider.GetRequiredService<AdminServiceDbContext>();

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

            if (await db.SystemAnnouncements.AnyAsync())
            {
                return;
            }

            var now = DateTime.UtcNow;

            // 1. Seed Announcements (AD-08)
            var announcements = new List<SystemAnnouncement>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Chào mừng gia nhập mạng lưới FPT Alumni Connect 2026",
                    Content = "Hệ thống FPT Alumni Connect chính thức đi vào hoạt động để kết nối cựu sinh viên, sinh viên và giảng viên toàn trường.",
                    Type = "General",
                    Priority = "High",
                    CreatedByAdminId = AdminUserId,
                    CreatedByAdminEmail = "admin@fpt.edu.vn",
                    IsPublished = true,
                    PublishedAtUtc = now.AddDays(-5),
                    CreatedAtUtc = now.AddDays(-5)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Thông báo Lễ hội FPT Alumni Gala Dinner Kỷ niệm 20 năm",
                    Content = "Đại học FPT trân trọng kính mời toàn thể cựu sinh viên tham dự đêm hội kết nối và giao lưu doanh nghiệp tại cơ sở Hòa Lạc.",
                    Type = "Event",
                    Priority = "Normal",
                    CreatedByAdminId = AdminUserId,
                    CreatedByAdminEmail = "admin@fpt.edu.vn",
                    IsPublished = true,
                    PublishedAtUtc = now.AddDays(-2),
                    CreatedAtUtc = now.AddDays(-2)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Thông báo bảo trì hệ thống và nâng cấp hạ tầng Microservices",
                    Content = "Hệ thống sẽ thực hiện bảo trì định kỳ cụm máy chủ và nâng cấp các dịch vụ nội bộ vào lúc 02:00 sáng Chủ Nhật.",
                    Type = "Maintenance",
                    Priority = "Normal",
                    CreatedByAdminId = AdminUserId,
                    CreatedByAdminEmail = "admin@fpt.edu.vn",
                    IsPublished = true,
                    PublishedAtUtc = now.AddHours(-12),
                    CreatedAtUtc = now.AddHours(-12)
                }
            };

            await db.SystemAnnouncements.AddRangeAsync(announcements);

            // 2. Seed Audit Logs (AD-07)
            var auditLogs = new List<AuditLog>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    AdminUserId = AdminUserId,
                    AdminEmail = "admin@fpt.edu.vn",
                    Action = "SuspendUser",
                    TargetEntity = "User",
                    TargetId = UserCId.ToString(),
                    Reason = "Spamming forum posts",
                    Details = "{\"statusBefore\": \"Active\", \"statusAfter\": \"Suspended\"}",
                    IpAddress = "127.0.0.1",
                    CreatedAtUtc = now.AddDays(-3)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    AdminUserId = AdminUserId,
                    AdminEmail = "admin@fpt.edu.vn",
                    Action = "BroadcastAnnouncement",
                    TargetEntity = "SystemAnnouncement",
                    TargetId = announcements[0].Id.ToString(),
                    Reason = "Welcome broadcast to all alumni",
                    IpAddress = "127.0.0.1",
                    CreatedAtUtc = now.AddDays(-5)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    AdminUserId = AdminUserId,
                    AdminEmail = "admin@fpt.edu.vn",
                    Action = "CreateCategory",
                    TargetEntity = "Category",
                    TargetId = Guid.NewGuid().ToString(),
                    Reason = "Added Software Engineering & AI Careers forum category",
                    IpAddress = "127.0.0.1",
                    CreatedAtUtc = now.AddDays(-4)
                }
            };

            await db.AuditLogs.AddRangeAsync(auditLogs);

            // 3. Seed Moderation Reports (AD-05)
            var reports = new List<ModerationReport>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ReporterUserId = UserAId,
                    ReporterEmail = "nguyenvana@fpt.edu.vn",
                    TargetType = "Post",
                    TargetId = Guid.NewGuid(),
                    Reason = "Quảng cáo spam khóa học ngoài trường không được kiểm duyệt",
                    Details = "Người đăng liên tục spam link mời chào vào nhóm Zalo thương mại.",
                    Status = "Pending",
                    CreatedAtUtc = now.AddHours(-6)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ReporterUserId = UserBId,
                    ReporterEmail = "tranthib@fpt.edu.vn",
                    TargetType = "Comment",
                    TargetId = Guid.NewGuid(),
                    Reason = "Ngôn từ thô tục và công kích cá nhân",
                    Details = "Bình luận vi phạm tiêu chuẩn cộng đồng FPT.",
                    Status = "Pending",
                    CreatedAtUtc = now.AddHours(-2)
                }
            };

            await db.ModerationReports.AddRangeAsync(reports);

            // 4. Seed System Settings
            var settings = new List<SystemSetting>
            {
                new()
                {
                    Key = "PlatformName",
                    Value = "FPT Alumni Connect",
                    Description = "Tên hiển thị chính thức của nền tảng mạng lưới cựu sinh viên",
                    Category = "General",
                    UpdatedByAdminId = AdminUserId,
                    UpdatedAtUtc = now
                },
                new()
                {
                    Key = "MaxDirectChatMembers",
                    Value = "2",
                    Description = "Giới hạn số người tham gia cuộc trò chuyện cá nhân 1-1",
                    Category = "Chat",
                    UpdatedByAdminId = AdminUserId,
                    UpdatedAtUtc = now
                },
                new()
                {
                    Key = "MaxGroupChatMembers",
                    Value = "50",
                    Description = "Giới hạn số thành viên tối đa trong nhóm chat cựu sinh viên",
                    Category = "Chat",
                    UpdatedByAdminId = AdminUserId,
                    UpdatedAtUtc = now
                }
            };

            await db.SystemSettings.AddRangeAsync(settings);

            await db.SaveChangesAsync();
            logger.LogInformation("AdminDatabaseSeeder completed successfully. Seeded {Announcements} announcements, {Logs} audit logs, {Reports} reports, {Settings} settings.",
                announcements.Count, auditLogs.Count, reports.Count, settings.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Admin database seeding encountered non-fatal error: {Message}", ex.Message);
        }
    }
}
