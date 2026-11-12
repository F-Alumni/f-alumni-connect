using Microsoft.EntityFrameworkCore;
using UserService.Repositories.Context;
using UserService.Repositories.Entities;
using UserService.Repositories.Helpers;
using UserService.Repositories.Implementations;
using Xunit;

namespace UserService.Tests.Services;

public class CampusFilteringTests : IDisposable
{
    private readonly UserServiceDbContext _dbContext;
    private readonly UserRepository _repository;

    public CampusFilteringTests()
    {
        var options = new DbContextOptionsBuilder<UserServiceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new UserServiceDbContext(options);
        SeedCampusUsers(_dbContext);
        _repository = new UserRepository(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Theory]
    [InlineData("Hồ Chí Minh", new[] { "HCM User", "FU-HCM User", "Ho Chi Minh User" })]
    [InlineData("Ho Chi Minh", new[] { "HCM User", "FU-HCM User", "Ho Chi Minh User" })]
    [InlineData("FU-HCM", new[] { "HCM User", "FU-HCM User", "Ho Chi Minh User" })]
    [InlineData("TP.HCM", new[] { "HCM User", "FU-HCM User", "Ho Chi Minh User" })]
    [InlineData("HCM", new[] { "HCM User", "FU-HCM User", "Ho Chi Minh User" })]
    [InlineData("Hòa Lạc", new[] { "Hoa Lac User", "Hoa Lac Accented User", "HN User", "FU-HL User" })]
    [InlineData("Hoà Lạc", new[] { "Hoa Lac User", "Hoa Lac Accented User", "HN User", "FU-HL User" })]
    [InlineData("Hoa Lac", new[] { "Hoa Lac User", "Hoa Lac Accented User", "HN User", "FU-HL User" })]
    [InlineData("Hà Nội", new[] { "Hoa Lac User", "Hoa Lac Accented User", "HN User", "FU-HL User" })]
    [InlineData("Ha Noi", new[] { "Hoa Lac User", "Hoa Lac Accented User", "HN User", "FU-HL User" })]
    [InlineData("FU-HL", new[] { "Hoa Lac User", "Hoa Lac Accented User", "HN User", "FU-HL User" })]
    [InlineData("Đà Nẵng", new[] { "Da Nang User", "DN Accented User" })]
    [InlineData("Da Nang", new[] { "Da Nang User", "DN Accented User" })]
    [InlineData("DN", new[] { "Da Nang User", "DN Accented User" })]
    [InlineData("Cần Thơ", new[] { "Can Tho User", "CT Code User" })]
    [InlineData("Can Tho", new[] { "Can Tho User", "CT Code User" })]
    [InlineData("Quy Nhơn", new[] { "Quy Nhon User", "QN Code User" })]
    [InlineData("Quy Nhon", new[] { "Quy Nhon User", "QN Code User" })]
    public async Task GetDirectoryPagedAsync_WithCampusAliases_MatchesAllEquivalentUsers(
        string queryCampus,
        string[] expectedFullNames)
    {
        var (items, totalCount) = await _repository.GetDirectoryPagedAsync(
            search: null,
            campus: queryCampus,
            graduationYear: null,
            major: null,
            company: null,
            status: null,
            role: null,
            pageNumber: 1,
            pageSize: 50);

        Assert.Equal(expectedFullNames.Length, totalCount);
        var actualNames = items.Select(u => u.FullName).OrderBy(n => n).ToArray();
        Assert.Equal(expectedFullNames.OrderBy(n => n).ToArray(), actualNames);
    }

    [Fact]
    public async Task GetDirectoryPagedAsync_WithDecomposedUnicodeCampus_MatchesHcmUsers()
    {
        var decomposedHcm = "Hồ Chí Minh".Normalize(System.Text.NormalizationForm.FormD);

        var (items, totalCount) = await _repository.GetDirectoryPagedAsync(
            search: null,
            campus: decomposedHcm,
            graduationYear: null,
            major: null,
            company: null,
            status: null,
            role: null,
            pageNumber: 1,
            pageSize: 50);

        Assert.Equal(3, totalCount);
        Assert.Contains(items, u => u.FullName == "HCM User");
    }

    [Fact]
    public async Task GetDirectoryPagedAsync_UnknownCampus_ReturnsEmptyResult()
    {
        var (items, totalCount) = await _repository.GetDirectoryPagedAsync(
            search: null,
            campus: "Campus Khong Ton Tai",
            graduationYear: null,
            major: null,
            company: null,
            status: null,
            role: null,
            pageNumber: 1,
            pageSize: 20);

        Assert.Empty(items);
        Assert.Equal(0, totalCount);
    }

    [Theory]
    [InlineData("Hồ Chí Minh", new[] { "HCM User", "FU-HCM User", "Ho Chi Minh User" })]
    [InlineData("Hòa Lạc", new[] { "Hoa Lac User", "Hoa Lac Accented User", "HN User", "FU-HL User" })]
    public async Task GetDirectoryPagedAsync_SearchWithCampusAlias_MatchesCampusUsers(
        string searchTerm,
        string[] expectedFullNames)
    {
        var (items, totalCount) = await _repository.GetDirectoryPagedAsync(
            search: searchTerm,
            campus: null,
            graduationYear: null,
            major: null,
            company: null,
            status: null,
            role: null,
            pageNumber: 1,
            pageSize: 50);

        Assert.Equal(expectedFullNames.Length, totalCount);
        Assert.Equal(
            expectedFullNames.OrderBy(n => n).ToArray(),
            items.Select(u => u.FullName).OrderBy(n => n).ToArray());
    }

    [Fact]
    public void BuildDirectoryQuery_TranslatesCleanlyForSqlServerProvider()
    {
        var sqlOptions = new DbContextOptionsBuilder<UserServiceDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=CampusTranslationCheck;Trusted_Connection=True;")
            .Options;

        using var sqlContext = new UserServiceDbContext(sqlOptions);
        var sqlRepo = new UserRepository(sqlContext);

        var campusSql = sqlRepo.BuildDirectoryQuery(
            search: null,
            campus: "Hồ Chí Minh",
            graduationYear: null,
            major: null,
            company: null,
            status: "Active",
            role: null).ToQueryString();

        var searchAliasSql = sqlRepo.BuildDirectoryQuery(
            search: "Hòa Lạc",
            campus: null,
            graduationYear: null,
            major: null,
            company: null,
            status: null,
            role: null).ToQueryString();

        var searchPlainSql = sqlRepo.BuildDirectoryQuery(
            search: "Nguyen",
            campus: null,
            graduationYear: null,
            major: null,
            company: null,
            status: null,
            role: null).ToQueryString();

        Assert.Contains("UserProfiles", campusSql);
        Assert.Contains("UserProfiles", searchAliasSql);
        Assert.Contains("UserProfiles", searchPlainSql);
    }

    [Theory]
    [InlineData("  Hồ   Chí   Minh  ", "Hồ Chí Minh")]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void CampusNormalizer_NormalizeOrNull_NormalizesWhitespaceAndNull(string? input, string? expected)
    {
        var actual = CampusNormalizer.NormalizeOrNull(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CampusNormalizer_NormalizeOrDefault_FallsBackToHcm()
    {
        Assert.Equal("HCM", CampusNormalizer.NormalizeOrDefault("   "));
        Assert.Equal("Hoa Lac", CampusNormalizer.NormalizeOrDefault("  Hoa   Lac "));
    }

    private static void SeedCampusUsers(UserServiceDbContext dbContext)
    {
        var users = new[]
        {
            CreateUser("HCM User", "HCM"),
            CreateUser("FU-HCM User", "FU-HCM"),
            CreateUser("Ho Chi Minh User", "Hồ Chí Minh"),
            CreateUser("Hoa Lac User", "Hoa Lac"),
            CreateUser("Hoa Lac Accented User", "Hòa Lạc"),
            CreateUser("HN User", "HN"),
            CreateUser("FU-HL User", "FU-HL"),
            CreateUser("Da Nang User", "Da Nang"),
            CreateUser("DN Accented User", "Đà Nẵng"),
            CreateUser("Can Tho User", "Can Tho"),
            CreateUser("CT Code User", "CT"),
            CreateUser("Quy Nhon User", "Quy Nhon"),
            CreateUser("QN Code User", "QN")
        };

        dbContext.Users.AddRange(users);
        dbContext.SaveChanges();
    }

    private static User CreateUser(string fullName, string campus)
    {
        var id = Guid.NewGuid();
        return new User
        {
            Id = id,
            Email = $"{id:N}@fpt.edu.vn",
            PasswordHash = "hash",
            FullName = fullName,
            Role = "ALUMNI",
            AccountType = "Alumni",
            Status = "Active",
            EmailVerified = true,
            Profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = id,
                DisplayName = fullName,
                Campus = campus
            }
        };
    }
}
