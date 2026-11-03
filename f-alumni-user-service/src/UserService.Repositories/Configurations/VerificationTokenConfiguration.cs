using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserService.Repositories.Entities;

namespace UserService.Repositories.Configurations;

public class VerificationTokenConfiguration : IEntityTypeConfiguration<VerificationToken>
{
    public void Configure(EntityTypeBuilder<VerificationToken> builder)
    {
        builder.ToTable("VerificationTokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OtpCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Purpose)
            .IsRequired()
            .HasMaxLength(40)
            .HasDefaultValue("EmailVerification");
    }
}
