using FluentAssertions;
using LogoVisualizer.Api.Helpers;
using Xunit;

namespace LogoVisualizer.Tests;

public class LogoPlacementCalculatorTests
{
    [Fact]
    public void CalculateFitToZone_OversizedLogo_ScalesDownAndStaysInsideZone()
    {
        // 800x600 logo into a 200x100 zone -> fit by height => 133x100
        var rect = LogoPlacementCalculator.CalculateFitToZone(
            sourceWidth: 800,
            sourceHeight: 600,
            zoneX: 50,
            zoneY: 20,
            zoneWidth: 200,
            zoneHeight: 100);

        rect.Width.Should().Be(133);
        rect.Height.Should().Be(100);
        rect.X.Should().Be(83);
        rect.Y.Should().Be(20);
        rect.Width.Should().BeLessThanOrEqualTo(200);
        rect.Height.Should().BeLessThanOrEqualTo(100);
        rect.X.Should().BeGreaterThanOrEqualTo(50);
        rect.Y.Should().BeGreaterThanOrEqualTo(20);
        (rect.X + rect.Width).Should().BeLessThanOrEqualTo(250);
        (rect.Y + rect.Height).Should().BeLessThanOrEqualTo(120);

        var sourceRatio = 800d / 600d;
        var outputRatio = rect.Width / (double)rect.Height;
        outputRatio.Should().BeApproximately(sourceRatio, 0.02);
    }

    [Fact]
    public void CalculateFitToZone_NonSquareLogo_PreservesAspectRatio()
    {
        // Source ratio 3:2. Zone forces scale down.
        var rect = LogoPlacementCalculator.CalculateFitToZone(
            sourceWidth: 300,
            sourceHeight: 200,
            zoneX: 0,
            zoneY: 0,
            zoneWidth: 120,
            zoneHeight: 90);

        var sourceRatio = 300d / 200d;
        var outputRatio = rect.Width / (double)rect.Height;

        outputRatio.Should().BeApproximately(sourceRatio, 0.02);
    }

    [Fact]
    public void CalculateFitToZone_ResultIsCenteredWithinZone()
    {
        // 100x50 in 100x100 -> expected 100x50 centered vertically at y+25
        var rect = LogoPlacementCalculator.CalculateFitToZone(
            sourceWidth: 100,
            sourceHeight: 50,
            zoneX: 10,
            zoneY: 30,
            zoneWidth: 100,
            zoneHeight: 100);

        rect.Width.Should().Be(100);
        rect.Height.Should().Be(50);
        rect.X.Should().Be(10);
        rect.Y.Should().Be(55);
    }

    [Fact]
    public void CalculateFitToZone_NeverOverflowsZoneBounds()
    {
        var rect = LogoPlacementCalculator.CalculateFitToZone(
            sourceWidth: 9999,
            sourceHeight: 1234,
            zoneX: 7,
            zoneY: 11,
            zoneWidth: 77,
            zoneHeight: 33);

        rect.X.Should().BeGreaterThanOrEqualTo(7);
        rect.Y.Should().BeGreaterThanOrEqualTo(11);
        rect.Width.Should().BeLessThanOrEqualTo(77);
        rect.Height.Should().BeLessThanOrEqualTo(33);
        (rect.X + rect.Width).Should().BeLessThanOrEqualTo(84);
        (rect.Y + rect.Height).Should().BeLessThanOrEqualTo(44);
    }

    [Fact]
    public void CalculateFitToZone_MatchingAspectRatio_FillsZoneAndCenters()
    {
        // Current behavior: a smaller logo with matching aspect ratio scales up to fill the zone.
        var rect = LogoPlacementCalculator.CalculateFitToZone(
            sourceWidth: 80,
            sourceHeight: 40,
            zoneX: 100,
            zoneY: 200,
            zoneWidth: 200,
            zoneHeight: 100);

        rect.Width.Should().Be(200);
        rect.Height.Should().Be(100);
        rect.X.Should().Be(100);
        rect.Y.Should().Be(200);
    }

    [Fact]
    public void CalculateFitToZone_SmallerSquareLogo_ScalesUpAndCentersWithoutOverflow()
    {
        // 25x25 logo into 200x100 zone -> scale 4x => 100x100, centered horizontally.
        var rect = LogoPlacementCalculator.CalculateFitToZone(
            sourceWidth: 25,
            sourceHeight: 25,
            zoneX: 10,
            zoneY: 20,
            zoneWidth: 200,
            zoneHeight: 100);

        rect.Width.Should().Be(100);
        rect.Height.Should().Be(100);
        rect.X.Should().Be(60);
        rect.Y.Should().Be(20);
        (rect.X + rect.Width).Should().BeLessThanOrEqualTo(210);
        (rect.Y + rect.Height).Should().BeLessThanOrEqualTo(120);
    }
}
