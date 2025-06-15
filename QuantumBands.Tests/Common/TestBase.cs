using AutoFixture;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumBands.Application.Interfaces;

namespace QuantumBands.Tests.Common;

public abstract class TestBase
{
    protected readonly IFixture Fixture;
    protected readonly Mock<IUnitOfWork> MockUnitOfWork;
    protected readonly Mock<IEmailService> MockEmailService;
    protected readonly Mock<IJwtTokenGenerator> MockJwtTokenGenerator;
    protected readonly Mock<IConfiguration> MockConfiguration;
    protected readonly Mock<ICachingService> MockCachingService;
    protected readonly Mock<ILogger> MockLogger;

    protected TestBase()
    {
        Fixture = new Fixture();
        
        // Configure AutoFixture to handle circular references
        Fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => Fixture.Behaviors.Remove(b));
        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Initialize mocks
        MockUnitOfWork = new Mock<IUnitOfWork>();
        MockEmailService = new Mock<IEmailService>();
        MockJwtTokenGenerator = new Mock<IJwtTokenGenerator>();
        MockConfiguration = new Mock<IConfiguration>();
        MockCachingService = new Mock<ICachingService>();
        MockLogger = new Mock<ILogger>();

        SetupDefaultMocks();
    }

    private void SetupDefaultMocks()
    {
        // Default configuration setup
        MockConfiguration.Setup(x => x["JwtSettings:Secret"])
            .Returns("SuperSecretKeyForJwtTokenGenerationThatIsAtLeast32CharactersLong");
        MockConfiguration.Setup(x => x["JwtSettings:Issuer"])
            .Returns("QuantumBands.API");
        MockConfiguration.Setup(x => x["JwtSettings:Audience"])
            .Returns("QuantumBands.Client");
        MockConfiguration.Setup(x => x["JwtSettings:ExpirationInMinutes"])
            .Returns("1440");

        // Default email service setup
        MockEmailService.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    protected void ResetAllMocks()
    {
        MockUnitOfWork.Reset();
        MockEmailService.Reset();
        MockJwtTokenGenerator.Reset();
        MockConfiguration.Reset();
        MockCachingService.Reset();
        MockLogger.Reset();
        
        SetupDefaultMocks();
    }
} 