using Nodra.Notes.Worker.Notifications;
using Xunit;

namespace Nodra.Notes.Worker.UnitTests.Notifications;

[Trait("Category", "Unit")]
public sealed class AdministrativeSmtpOptionsValidatorTests
{
    [Fact]
    public void ValidateWithCompleteTlsConfigurationSucceeds()
    {
        var result = new AdministrativeSmtpOptionsValidator().Validate(null, CreateValidOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidateWithoutCredentialsFails()
    {
        var result = new AdministrativeSmtpOptionsValidator().Validate(
            null,
            new AdministrativeSmtpOptions
            {
                Host = "smtp.example",
                Port = 587,
                Username = "altivy-alerts",
                From = "altivy-alerts@example.com",
                To = "operations@example.com"
            });

        Assert.False(result.Succeeded);
    }

    private static AdministrativeSmtpOptions CreateValidOptions() => new()
    {
        Host = "smtp.example",
        Port = 587,
        Username = "altivy-alerts",
        Password = "test-password",
        From = "altivy-alerts@example.com",
        To = "operations@example.com"
    };
}
