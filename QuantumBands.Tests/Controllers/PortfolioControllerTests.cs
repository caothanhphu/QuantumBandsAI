// SCRUM-62: Unit Tests for PortfolioController
// This test file provides comprehensive coverage for the PortfolioController endpoints
// covering portfolio calculation, financial data accuracy, authentication, and edge cases

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using QuantumBands.API.Controllers;
using QuantumBands.Application.Features.Portfolio.Dtos;
using QuantumBands.Application.Interfaces;
using QuantumBands.Tests.Common;
using QuantumBands.Tests.Fixtures;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace QuantumBands.Tests.Controllers
{
    public class PortfolioControllerTests : TestBase
    {
        private readonly Mock<ISharePortfolioService> _mockPortfolioService;
        private readonly Mock<ILogger<PortfolioController>> _mockLogger;
        private readonly PortfolioController _controller;

        public PortfolioControllerTests()
        {
            _mockPortfolioService = new Mock<ISharePortfolioService>();
            _mockLogger = new Mock<ILogger<PortfolioController>>();
            _controller = new PortfolioController(_mockPortfolioService.Object, _mockLogger.Object);
        }

        #region GetMyPortfolio Tests - SCRUM-62

        // SCRUM-62: Unit Tests for GET /portfolio/me - Get My Portfolio Endpoint
        // This section provides comprehensive test coverage for the PortfolioController.GetMyPortfolio endpoint:
        // - GetMyPortfolio (GET /portfolio/me): User portfolio retrieval with calculation validation and authentication tests
        //
        // Test Coverage Summary:
        // 1. Happy Path Tests: Valid portfolio retrieval with multiple positions and calculations
        // 2. Calculation Tests: Financial calculations accuracy (PnL, average prices, current values)
        // 3. Authentication Tests: User data isolation and authentication handling
        // 4. Edge Cases: Empty portfolios, zero quantities, high-value positions
        // 5. Error Handling: Service failures, authentication errors, data validation
        // 6. Business Logic: Portfolio privacy protection and real-time data accuracy
        //
        // Each test validates the controller's ability to:
        // - Authenticate users and extract user context
        // - Call the portfolio service with correct user context
        // - Handle service responses appropriately
        // - Return correct HTTP status codes and response formats
        // - Maintain data privacy and user isolation
        // - Calculate financial metrics accurately

        [Fact]
        public async Task GetMyPortfolio_WithValidUser_ShouldReturnPortfolioData()
        {
            // Arrange
            var user = CreateAuthenticatedUser("123", "testuser");
            _controller.ControllerContext = CreateControllerContext(user);
            
            var expectedPortfolio = TestDataBuilder.GetMyPortfolio.ValidPortfolioResponse();
            _mockPortfolioService.Setup(x => x.GetMyPortfolioAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((expectedPortfolio, (string?)null));

            // Act
            var result = await _controller.GetMyPortfolio(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var portfolioData = okResult!.Value as List<SharePortfolioItemDto>;
            
            portfolioData.Should().NotBeNull();
            portfolioData.Should().HaveCount(2);
            portfolioData![0].TradingAccountName.Should().Be("Tech Solutions Inc.");
            portfolioData[0].Quantity.Should().Be(150);
            portfolioData[0].AverageBuyPrice.Should().Be(25.50m);
            portfolioData[0].CurrentSharePrice.Should().Be(28.75m);
            portfolioData[0].UnrealizedPAndL.Should().Be(487.50m);

            // Verify service was called with user context
            _mockPortfolioService.Verify(x => x.GetMyPortfolioAsync(
                It.Is<ClaimsPrincipal>(u => u.FindFirst(ClaimTypes.NameIdentifier) != null && u.FindFirst(ClaimTypes.NameIdentifier).Value == "123"),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetMyPortfolio_WithProfitablePositions_ShouldReturnCorrectPnLCalculations()
        {
            // Arrange
            var user = CreateAuthenticatedUser("456", "profitableuser");
            _controller.ControllerContext = CreateControllerContext(user);
            
            var expectedPortfolio = TestDataBuilder.GetMyPortfolio.ProfitablePortfolioResponse();
            _mockPortfolioService.Setup(x => x.GetMyPortfolioAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((expectedPortfolio, (string?)null));

            // Act
            var result = await _controller.GetMyPortfolio(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var portfolioData = okResult!.Value as List<SharePortfolioItemDto>;
            
            portfolioData.Should().NotBeNull();
            portfolioData.Should().HaveCount(1);
            portfolioData![0].UnrealizedPAndL.Should().Be(1000.00m);
            portfolioData[0].CurrentValue.Should().Be(3000.00m);
            
            // Verify calculation accuracy: (current price - average buy price) * quantity
            var expectedPnL = (portfolioData[0].CurrentSharePrice - portfolioData[0].AverageBuyPrice) * portfolioData[0].Quantity;
            portfolioData[0].UnrealizedPAndL.Should().Be(expectedPnL);
        }

        [Fact]
        public async Task GetMyPortfolio_WithLosingPositions_ShouldReturnNegativePnL()
        {
            // Arrange
            var user = CreateAuthenticatedUser("789", "losinguser");
            _controller.ControllerContext = CreateControllerContext(user);
            
            var expectedPortfolio = TestDataBuilder.GetMyPortfolio.LosingPortfolioResponse();
            _mockPortfolioService.Setup(x => x.GetMyPortfolioAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((expectedPortfolio, (string?)null));

            // Act
            var result = await _controller.GetMyPortfolio(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var portfolioData = okResult!.Value as List<SharePortfolioItemDto>;
            
            portfolioData.Should().NotBeNull();
            portfolioData.Should().HaveCount(1);
            portfolioData![0].UnrealizedPAndL.Should().Be(-520.00m);
            portfolioData[0].CurrentValue.Should().Be(2280.00m);
            
            // Verify negative PnL calculation
            portfolioData[0].UnrealizedPAndL.Should().BeLessThan(0);
        }

        [Fact]
        public async Task GetMyPortfolio_WithEmptyPortfolio_ShouldReturnEmptyList()
        {
            // Arrange
            var user = CreateAuthenticatedUser("999", "emptyuser");
            _controller.ControllerContext = CreateControllerContext(user);
            
            var expectedPortfolio = TestDataBuilder.GetMyPortfolio.EmptyPortfolioResponse();
            _mockPortfolioService.Setup(x => x.GetMyPortfolioAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((expectedPortfolio, (string?)null));

            // Act
            var result = await _controller.GetMyPortfolio(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var portfolioData = okResult!.Value as List<SharePortfolioItemDto>;
            
            portfolioData.Should().NotBeNull();
            portfolioData.Should().BeEmpty();
            
            // Verify service was still called
            _mockPortfolioService.Verify(x => x.GetMyPortfolioAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetMyPortfolio_WithZeroQuantityPositions_ShouldReturnZeroValues()
        {
            // Arrange
            var user = CreateAuthenticatedUser("111", "zerouser");
            _controller.ControllerContext = CreateControllerContext(user);
            
            var expectedPortfolio = TestDataBuilder.GetMyPortfolio.ZeroQuantityPortfolioResponse();
            _mockPortfolioService.Setup(x => x.GetMyPortfolioAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((expectedPortfolio, (string?)null));

            // Act
            var result = await _controller.GetMyPortfolio(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var portfolioData = okResult!.Value as List<SharePortfolioItemDto>;
            
            portfolioData.Should().NotBeNull();
            portfolioData.Should().HaveCount(1);
            portfolioData![0].Quantity.Should().Be(0);
            portfolioData[0].CurrentValue.Should().Be(0.00m);
            portfolioData[0].UnrealizedPAndL.Should().Be(0.00m);
        }

        [Fact]
        public async Task GetMyPortfolio_WithLargePortfolio_ShouldHandleHighValues()
        {
            // Arrange
            var user = CreateAuthenticatedUser("222", "wealthyuser");
            _controller.ControllerContext = CreateControllerContext(user);
            
            var expectedPortfolio = TestDataBuilder.GetMyPortfolio.LargePortfolioResponse();
            _mockPortfolioService.Setup(x => x.GetMyPortfolioAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((expectedPortfolio, (string?)null));

            // Act
            var result = await _controller.GetMyPortfolio(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var portfolioData = okResult!.Value as List<SharePortfolioItemDto>;
            
            portfolioData.Should().NotBeNull();
            portfolioData.Should().HaveCount(1);
            portfolioData![0].CurrentValue.Should().Be(627500.00m);
            portfolioData[0].UnrealizedPAndL.Should().Be(127500.00m);
            portfolioData[0].Quantity.Should().Be(5000);
        }

        [Fact]
        public async Task GetMyPortfolio_WithMultipleTradingAccounts_ShouldReturnAllPositions()
        {
            // Arrange
            var user = CreateAuthenticatedUser("333", "multiuser");
            _controller.ControllerContext = CreateControllerContext(user);
            
            var expectedPortfolio = TestDataBuilder.GetMyPortfolio.MultiAccountPortfolioResponse();
            _mockPortfolioService.Setup(x => x.GetMyPortfolioAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((expectedPortfolio, (string?)null));

            // Act
            var result = await _controller.GetMyPortfolio(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var portfolioData = okResult!.Value as List<SharePortfolioItemDto>;
            
            portfolioData.Should().NotBeNull();
            portfolioData.Should().HaveCount(3);
            
            // Verify different trading accounts
            portfolioData![0].TradingAccountName.Should().Be("Tech Solutions Inc.");
            portfolioData[1].TradingAccountName.Should().Be("Green Energy Corp.");
            portfolioData[2].TradingAccountName.Should().Be("Financial Holdings Ltd.");
            
            // Verify mixed PnL scenarios
            portfolioData[0].UnrealizedPAndL.Should().BeGreaterThan(0); // Profit
            portfolioData[1].UnrealizedPAndL.Should().BeGreaterThan(0); // Profit
            portfolioData[2].UnrealizedPAndL.Should().BeLessThan(0);    // Loss
        }

        [Fact]
        public async Task GetMyPortfolio_WithPreciseDecimalCalculations_ShouldMaintainAccuracy()
        {
            // Arrange
            var user = CreateAuthenticatedUser("444", "preciseuser");
            _controller.ControllerContext = CreateControllerContext(user);
            
            var expectedPortfolio = TestDataBuilder.GetMyPortfolio.PreciseDecimalPortfolioResponse();
            _mockPortfolioService.Setup(x => x.GetMyPortfolioAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((expectedPortfolio, (string?)null));

            // Act
            var result = await _controller.GetMyPortfolio(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var portfolioData = okResult!.Value as List<SharePortfolioItemDto>;
            
            portfolioData.Should().NotBeNull();
            portfolioData.Should().HaveCount(1);
            
            // Verify precise decimal handling
            portfolioData![0].AverageBuyPrice.Should().Be(12.3456m);
            portfolioData[0].CurrentSharePrice.Should().Be(13.7891m);
            portfolioData[0].CurrentValue.Should().Be(1696.0593m);
            portfolioData[0].UnrealizedPAndL.Should().Be(177.7548m);
        }

        [Fact]
        public async Task GetMyPortfolio_WithServiceError_ShouldReturnInternalServerError()
        {
            // Arrange
            var user = CreateAuthenticatedUser("555", "erroruser");
            _controller.ControllerContext = CreateControllerContext(user);
            
            _mockPortfolioService.Setup(x => x.GetMyPortfolioAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(((List<SharePortfolioItemDto>?)null, "Database connection failed"));

            // Act
            var result = await _controller.GetMyPortfolio(CancellationToken.None);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
            objectResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetMyPortfolio_WithNotFoundError_ShouldReturnEmptyList()
        {
            // Arrange
            var user = CreateAuthenticatedUser("666", "notfounduser");
            _controller.ControllerContext = CreateControllerContext(user);
            
            _mockPortfolioService.Setup(x => x.GetMyPortfolioAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(((List<SharePortfolioItemDto>?)null, "User not found"));

            // Act
            var result = await _controller.GetMyPortfolio(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var portfolioData = okResult!.Value as List<SharePortfolioItemDto>;
            
            portfolioData.Should().NotBeNull();
            portfolioData.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMyPortfolio_WithAuthenticationError_ShouldReturnEmptyList()
        {
            // Arrange
            var user = CreateAuthenticatedUser("777", "unauthuser");
            _controller.ControllerContext = CreateControllerContext(user);
            
            _mockPortfolioService.Setup(x => x.GetMyPortfolioAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(((List<SharePortfolioItemDto>?)null, "User not authenticated"));

            // Act
            var result = await _controller.GetMyPortfolio(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var portfolioData = okResult!.Value as List<SharePortfolioItemDto>;
            
            portfolioData.Should().NotBeNull();
            portfolioData.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMyPortfolio_ShouldExtractUserIdCorrectly()
        {
            // Arrange
            var expectedUserId = "888";
            var user = CreateAuthenticatedUser(expectedUserId, "specificuser");
            _controller.ControllerContext = CreateControllerContext(user);
            
            var expectedPortfolio = TestDataBuilder.GetMyPortfolio.ValidPortfolioResponse();
            _mockPortfolioService.Setup(x => x.GetMyPortfolioAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((expectedPortfolio, (string?)null));

            // Act
            var result = await _controller.GetMyPortfolio(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            
            // Verify that service was called with the correct user context
            _mockPortfolioService.Verify(x => x.GetMyPortfolioAsync(
                It.Is<ClaimsPrincipal>(u => u.FindFirst(ClaimTypes.NameIdentifier) != null && u.FindFirst(ClaimTypes.NameIdentifier).Value == expectedUserId),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetMyPortfolio_ShouldLogUserRequest()
        {
            // Arrange
            var user = CreateAuthenticatedUser("999", "loguser");
            _controller.ControllerContext = CreateControllerContext(user);
            
            var expectedPortfolio = TestDataBuilder.GetMyPortfolio.ValidPortfolioResponse();
            _mockPortfolioService.Setup(x => x.GetMyPortfolioAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((expectedPortfolio, (string?)null));

            // Act
            var result = await _controller.GetMyPortfolio(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            
            // Verify logging occurred (at least once)
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("requesting their portfolio")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetMyPortfolio_WithServiceFailure_ShouldLogWarning()
        {
            // Arrange
            var user = CreateAuthenticatedUser("000", "failuser");
            _controller.ControllerContext = CreateControllerContext(user);
            
            _mockPortfolioService.Setup(x => x.GetMyPortfolioAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(((List<SharePortfolioItemDto>?)null, "Service temporarily unavailable"));

            // Act
            var result = await _controller.GetMyPortfolio(CancellationToken.None);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            
            // Verify warning was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to retrieve portfolio")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetMyPortfolio_ShouldRespectCancellationToken()
        {
            // Arrange
            var user = CreateAuthenticatedUser("111", "canceluser");
            _controller.ControllerContext = CreateControllerContext(user);
            
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            
            _mockPortfolioService.Setup(x => x.GetMyPortfolioAsync(
                It.IsAny<ClaimsPrincipal>(),
                cancellationToken))
                .ReturnsAsync((TestDataBuilder.GetMyPortfolio.ValidPortfolioResponse(), (string?)null));

            // Act
            var result = await _controller.GetMyPortfolio(cancellationToken);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            
            // Verify service was called with the specific cancellation token
            _mockPortfolioService.Verify(x => x.GetMyPortfolioAsync(
                It.IsAny<ClaimsPrincipal>(),
                cancellationToken),
                Times.Once);
        }

        [Fact]
        public async Task GetMyPortfolio_WithRealTimeDataUpdate_ShouldReturnLatestValues()
        {
            // Arrange
            var user = CreateAuthenticatedUser("222", "realtimeuser");
            _controller.ControllerContext = CreateControllerContext(user);
            
            // Create portfolio with recent timestamp to simulate real-time data
            var recentPortfolio = new List<SharePortfolioItemDto>
            {
                TestDataBuilder.GetMyPortfolio.CustomPortfolioItem(
                    portfolioId: 1,
                    tradingAccountId: 1,
                    tradingAccountName: "Real-time Trading Inc.",
                    quantity: 100,
                    averageBuyPrice: 50.00m,
                    currentSharePrice: 55.25m)
            };
            
            _mockPortfolioService.Setup(x => x.GetMyPortfolioAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((recentPortfolio, (string?)null));

            // Act
            var result = await _controller.GetMyPortfolio(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var portfolioData = okResult!.Value as List<SharePortfolioItemDto>;
            
            portfolioData.Should().NotBeNull();
            portfolioData.Should().HaveCount(1);
            portfolioData![0].CurrentSharePrice.Should().Be(55.25m);
            portfolioData[0].CurrentValue.Should().Be(5525.00m);
            portfolioData[0].UnrealizedPAndL.Should().Be(525.00m);
            
            // Verify LastUpdatedAt is recent (within last hour)
            portfolioData[0].LastUpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(1));
        }

        #endregion

        #region Helper Methods

        private ClaimsPrincipal CreateAuthenticatedUser(string userId, string username)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Name, username),
                new(ClaimTypes.Email, $"{username}@test.com")
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        private ControllerContext CreateControllerContext(ClaimsPrincipal user)
        {
            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user
                }
            };
        }

        #endregion
    }
} 