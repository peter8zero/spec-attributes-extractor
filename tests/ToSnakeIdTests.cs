using SpecOptionExtractor;
using Xunit;

namespace Tests;

public class ToSnakeIdTests
{
    [Theory]
    [InlineData("TrivialCommutation", "trivial_commutation")]
    [InlineData("CpiCappedRevaluation", "cpi_capped_revaluation")]
    [InlineData("FixedRateRevaluation", "fixed_rate_revaluation")]
    [InlineData("GmpEqualiser", "gmp_equaliser")]
    [InlineData("GMPEqualiser", "gmp_equaliser")]
    [InlineData("CETVCalculator", "cetv_calculator")]
    [InlineData("DCSchemeBuilder", "dc_scheme_builder")]
    [InlineData("CheckAntiFranking", "check_anti_franking")]
    [InlineData("ApplySection148", "apply_section148")]
    [InlineData("CalculateProRata", "calculate_pro_rata")]
    [InlineData("CalculatePartialCetv", "calculate_partial_cetv")]
    [InlineData("CalculateClubTransfer", "calculate_club_transfer")]
    public void PascalCase_to_snake_case(string input, string expected)
    {
        Assert.Equal(expected, AttributeParser.ToSnakeId(input));
    }

    [Theory]
    [InlineData("Some Name", "some_name")]
    [InlineData("hyphen-name", "hyphen_name")]
    [InlineData("already_snake", "already_snake")]
    [InlineData("lowercase", "lowercase")]
    [InlineData("A", "a")]
    public void Handles_spaces_hyphens_and_edge_cases(string input, string expected)
    {
        Assert.Equal(expected, AttributeParser.ToSnakeId(input));
    }

    [Theory]
    [InlineData("ABCDef", "abc_def")]
    [InlineData("XMLParser", "xml_parser")]
    [InlineData("HTMLToXML", "html_to_xml")]
    [InlineData("IOStream", "io_stream")]
    public void Consecutive_uppercase_runs(string input, string expected)
    {
        Assert.Equal(expected, AttributeParser.ToSnakeId(input));
    }
}
