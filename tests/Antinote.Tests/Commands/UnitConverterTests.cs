using Knowte.Commands;

namespace Knowte.Tests.Commands;

[TestFixture]
public class UnitConverterTests
{
    // ── Distance ─────────────────────────────────────────────────────────────

    [Test]
    public void Distance_KmToMiles_ReturnsResult()
    {
        var result = UnitConverter.TryConvert("5 km to miles");
        Assert.That(result, Is.EqualTo("3.11 mi"));
    }

    [Test]
    public void Distance_MilesToKm_ReturnsResult()
    {
        var result = UnitConverter.TryConvert("1 miles to km");
        Assert.That(result, Is.EqualTo("1.61 km"));
    }

    [Test]
    public void Distance_CmToInches_ReturnsResult()
    {
        var result = UnitConverter.TryConvert("100 cm to inches");
        Assert.That(result, Is.EqualTo("39.37 in"));
    }

    [Test]
    public void Distance_MToFt_ReturnsResult()
    {
        var result = UnitConverter.TryConvert("1 m to ft");
        Assert.That(result, Is.EqualTo("3.28 ft"));
    }

    // ── Weight ────────────────────────────────────────────────────────────────

    [Test]
    public void Weight_KgToLbs_ReturnsResult()
    {
        var result = UnitConverter.TryConvert("70 kg to lbs");
        Assert.That(result, Is.EqualTo("154.32 lbs"));
    }

    [Test]
    public void Weight_LbsToKg_ReturnsResult()
    {
        var result = UnitConverter.TryConvert("10 lbs to kg");
        Assert.That(result, Is.EqualTo("4.54 kg"));
    }

    [Test]
    public void Weight_GToOz_ReturnsResult()
    {
        var result = UnitConverter.TryConvert("100 g to oz");
        Assert.That(result, Is.EqualTo("3.53 oz"));
    }

    // ── Temperature ───────────────────────────────────────────────────────────

    [Test]
    public void Temperature_FToC_FreezingPoint_ReturnsZero()
    {
        var result = UnitConverter.TryConvert("32 F to C");
        Assert.That(result, Is.EqualTo("0 °C"));
    }

    [Test]
    public void Temperature_CToF_BoilingPoint_Returns212()
    {
        var result = UnitConverter.TryConvert("100 C to F");
        Assert.That(result, Is.EqualTo("212 °F"));
    }

    [Test]
    public void Temperature_CToK_ReturnsResult()
    {
        var result = UnitConverter.TryConvert("0 C to K");
        Assert.That(result, Is.EqualTo("273.15 K"));
    }

    // ── Digital storage ───────────────────────────────────────────────────────

    [Test]
    public void Storage_GbToMb_ReturnsResult()
    {
        var result = UnitConverter.TryConvert("1 GB to MB");
        Assert.That(result, Is.EqualTo("1024 MB"));
    }

    [Test]
    public void Storage_MbToKb_ReturnsResult()
    {
        var result = UnitConverter.TryConvert("2 MB to KB");
        Assert.That(result, Is.EqualTo("2048 KB"));
    }

    [Test]
    public void Storage_TbToGb_ReturnsResult()
    {
        var result = UnitConverter.TryConvert("1 TB to GB");
        Assert.That(result, Is.EqualTo("1024 GB"));
    }

    // ── Area ──────────────────────────────────────────────────────────────────

    [Test]
    public void Area_AcreToSqm_ReturnsResult()
    {
        var result = UnitConverter.TryConvert("1 acre to sqm");
        Assert.That(result, Is.EqualTo("4046.86 m²"));
    }

    [Test]
    public void Area_SqmToSqft_ReturnsResult()
    {
        var result = UnitConverter.TryConvert("1 sqm to sqft");
        Assert.That(result, Is.EqualTo("10.76 ft²"));
    }

    // ── Currency ──────────────────────────────────────────────────────────────

    [Test]
    public void Currency_UsdToEur_ReturnsResult()
    {
        var result = UnitConverter.TryConvert("100 USD to EUR");
        Assert.That(result, Is.EqualTo("92.00 EUR"));
    }

    [Test]
    public void Currency_EurToGbp_ReturnsResult()
    {
        var result = UnitConverter.TryConvert("100 EUR to GBP");
        Assert.That(result, Is.Not.Null);
    }

    // ── px ↔ rem ──────────────────────────────────────────────────────────────

    [Test]
    public void Px_PxToRem_Base16_ReturnsResult()
    {
        var result = UnitConverter.TryConvert("16 px to rem");
        Assert.That(result, Is.EqualTo("1 rem"));
    }

    [Test]
    public void Px_RemToPx_Base16_ReturnsResult()
    {
        var result = UnitConverter.TryConvert("2 rem to px");
        Assert.That(result, Is.EqualTo("32 px"));
    }

    // ── Case / format insensitivity ───────────────────────────────────────────

    [Test]
    public void CaseInsensitive_KmToMiles_Uppercase_ReturnsResult()
    {
        var result = UnitConverter.TryConvert("5 KM TO MILES");
        Assert.That(result, Is.EqualTo("3.11 mi"));
    }

    // ── Invalid / unparseable ─────────────────────────────────────────────────

    [Test]
    public void Invalid_RandomText_ReturnsNull()
    {
        var result = UnitConverter.TryConvert("hello world");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Invalid_UnknownUnit_ReturnsNull()
    {
        var result = UnitConverter.TryConvert("5 zorbs to blorbs");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Invalid_EmptyString_ReturnsNull()
    {
        var result = UnitConverter.TryConvert("");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Invalid_MissingToKeyword_ReturnsNull()
    {
        var result = UnitConverter.TryConvert("5 km miles");
        Assert.That(result, Is.Null);
    }

    // ── DocumentMode detection ────────────────────────────────────────────────

    [Test]
    public void DocumentModeDetector_Convert_DetectsConvertMode()
    {
        var mode = DocumentModeDetector.Detect("convert\n5 km to miles");
        Assert.That(mode, Is.EqualTo(DocumentMode.Convert));
    }

    [Test]
    public void DocumentModeDetector_Convert_CaseInsensitive()
    {
        var mode = DocumentModeDetector.Detect("CONVERT");
        Assert.That(mode, Is.EqualTo(DocumentMode.Convert));
    }
}
