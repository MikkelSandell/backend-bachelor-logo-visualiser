using FluentAssertions;
using LogoVisualizer.Api.Helpers;
using Xunit;

namespace LogoVisualizer.Tests;

public class LogoPlacementCalculatorTests
{
    // =====================================================================
    // Scale-down: logo larger than zone
    // =====================================================================

    [Fact]
    public void CalculateFitToZone_OversizedLogo_ScalesDownAndStaysInsideZone()
    {
        // 800x600 logo → 200x100 zone → constrained by height → 133x100
        var rect = LogoPlacementCalculator.CalculateFitToZone(800, 600, zoneX: 50, zoneY: 20, zoneWidth: 200, zoneHeight: 100);

        rect.Width.Should().Be(133);
        rect.Height.Should().Be(100);
        rect.X.Should().Be(83);
        rect.Y.Should().Be(20);
        AssertWithinZone(rect, zoneX: 50, zoneY: 20, zoneWidth: 200, zoneHeight: 100);
        AssertAspectRatioPreserved(rect, sourceWidth: 800, sourceHeight: 600);
    }

    [Fact]
    public void CalculateFitToZone_TallNarrowLogo_FitsByWidthAndCentersVertically()
    {
        // 50x200 logo → 100x100 zone → constrained by width → 25x100, centred (no vertical gap)
        var rect = LogoPlacementCalculator.CalculateFitToZone(50, 200, zoneX: 0, zoneY: 0, zoneWidth: 100, zoneHeight: 100);

        rect.Width.Should().Be(25);
        rect.Height.Should().Be(100);
        rect.X.Should().Be(37);  // (100 - 25) / 2 = 37 (integer division)
        rect.Y.Should().Be(0);
        AssertWithinZone(rect, 0, 0, 100, 100);
        AssertAspectRatioPreserved(rect, 50, 200);
    }

    [Fact]
    public void CalculateFitToZone_NonSquareLogo_PreservesAspectRatio()
    {
        // 3:2 source ratio, forced scale-down
        var rect = LogoPlacementCalculator.CalculateFitToZone(300, 200, zoneX: 0, zoneY: 0, zoneWidth: 120, zoneHeight: 90);

        AssertAspectRatioPreserved(rect, 300, 200);
        AssertWithinZone(rect, 0, 0, 120, 90);
    }

    // =====================================================================
    // Centering
    // =====================================================================

    [Fact]
    public void CalculateFitToZone_WiderLogoInSquareZone_CentersVertically()
    {
        // 100x50 logo → 100x100 zone → fits width exactly, centred vertically
        var rect = LogoPlacementCalculator.CalculateFitToZone(100, 50, zoneX: 10, zoneY: 30, zoneWidth: 100, zoneHeight: 100);

        rect.Width.Should().Be(100);
        rect.Height.Should().Be(50);
        rect.X.Should().Be(10);
        rect.Y.Should().Be(55);  // 30 + (100 - 50) / 2
    }

    // =====================================================================
    // Scale-up: logo smaller than zone
    // =====================================================================

    [Fact]
    public void CalculateFitToZone_MatchingAspectRatio_FillsZoneCompletely()
    {
        // 80x40 (same 2:1 ratio as 200x100 zone) → scaled up to fill
        var rect = LogoPlacementCalculator.CalculateFitToZone(80, 40, zoneX: 100, zoneY: 200, zoneWidth: 200, zoneHeight: 100);

        rect.Width.Should().Be(200);
        rect.Height.Should().Be(100);
        rect.X.Should().Be(100);
        rect.Y.Should().Be(200);
    }

    [Fact]
    public void CalculateFitToZone_SmallerSquareLogo_ScalesUpAndCentersHorizontally()
    {
        // 25x25 logo → 200x100 zone → constrained by height → 100x100, centred horizontally
        var rect = LogoPlacementCalculator.CalculateFitToZone(25, 25, zoneX: 10, zoneY: 20, zoneWidth: 200, zoneHeight: 100);

        rect.Width.Should().Be(100);
        rect.Height.Should().Be(100);
        rect.X.Should().Be(60);  // 10 + (200 - 100) / 2
        rect.Y.Should().Be(20);
        AssertWithinZone(rect, 10, 20, 200, 100);
    }

    // =====================================================================
    // Boundary / edge cases
    // =====================================================================

    [Fact]
    public void CalculateFitToZone_NeverOverflowsZoneBounds()
    {
        var rect = LogoPlacementCalculator.CalculateFitToZone(9999, 1234, zoneX: 7, zoneY: 11, zoneWidth: 77, zoneHeight: 33);

        AssertWithinZone(rect, 7, 11, 77, 33);
    }

    [Fact]
    public void CalculateFitToZone_ZeroSourceDimensions_TreatedAsOneByOne()
    {
        // Source 0x0 is clamped to 1x1 internally; result should still fit in zone
        var rect = LogoPlacementCalculator.CalculateFitToZone(0, 0, zoneX: 0, zoneY: 0, zoneWidth: 100, zoneHeight: 50);

        rect.Width.Should().BeGreaterThan(0);
        rect.Height.Should().BeGreaterThan(0);
        AssertWithinZone(rect, 0, 0, 100, 50);
    }

    [Fact]
    public void CalculateFitToZone_MinimumZoneSize_ReturnsOneByOnePixel()
    {
        var rect = LogoPlacementCalculator.CalculateFitToZone(400, 300, zoneX: 5, zoneY: 5, zoneWidth: 1, zoneHeight: 1);

        rect.Width.Should().Be(1);
        rect.Height.Should().Be(1);
        AssertWithinZone(rect, 5, 5, 1, 1);
    }

    /// <summary>Invalid zone dimensions should throw immediately rather than produce silent garbage output.</summary>
    [Theory]
    [InlineData(0,  10)]
    [InlineData(10, 0)]
    [InlineData(-1, 10)]
    public void CalculateFitToZone_InvalidZoneDimensions_Throws(int zoneWidth, int zoneHeight)
    {
        Action act = () => LogoPlacementCalculator.CalculateFitToZone(100, 100, 0, 0, zoneWidth, zoneHeight);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private static void AssertWithinZone(LogoRenderRect rect, int zoneX, int zoneY, int zoneWidth, int zoneHeight)
    {
        rect.X.Should().BeGreaterThanOrEqualTo(zoneX);
        rect.Y.Should().BeGreaterThanOrEqualTo(zoneY);
        (rect.X + rect.Width).Should().BeLessThanOrEqualTo(zoneX + zoneWidth);
        (rect.Y + rect.Height).Should().BeLessThanOrEqualTo(zoneY + zoneHeight);
    }

    private static void AssertAspectRatioPreserved(LogoRenderRect rect, int sourceWidth, int sourceHeight, double tolerance = 0.02)
    {
        var expected = sourceWidth / (double)sourceHeight;
        var actual   = rect.Width  / (double)rect.Height;
        actual.Should().BeApproximately(expected, tolerance);
    }
}
