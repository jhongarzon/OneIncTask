using FluentAssertions;
using OneIncTask.Application.Services;

namespace OneIncTask.UnitTests.Services;

public class StringProcessingServiceTests
{
    private readonly StringProcessingService _service = new();

    [Fact]
    public void BuildProcessedString_HelloWorld_ReturnsExpectedResult()
    {
        var result = _service.BuildProcessedString("Hello, World!");

        // Space=1, !=1, ,=1, H=1, W=1, d=1, e=1, l=3, o=2, r=1 + / + Base64
        var expectedBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Hello, World!"));
        result.Should().EndWith($"/{expectedBase64}");
        result.Should().Be($" 1!1,1H1W1d1e1l3o2r1/{expectedBase64}");
    }

    [Fact]
    public void BuildProcessedString_EmptyString_ReturnsEmpty()
    {
        var result = _service.BuildProcessedString(string.Empty);
        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildProcessedString_Null_ReturnsEmpty()
    {
        var result = _service.BuildProcessedString(null!);
        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildProcessedString_SingleCharacter_ReturnsCharCountAndBase64()
    {
        var result = _service.BuildProcessedString("a");
        var expectedBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("a"));
        result.Should().Be($"a1/{expectedBase64}");
    }

    [Fact]
    public void BuildProcessedString_RepeatedCharacters_ReturnsCorrectCount()
    {
        var result = _service.BuildProcessedString("aaa");
        var expectedBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("aaa"));
        result.Should().Be($"a3/{expectedBase64}");
    }

    [Fact]
    public void BuildProcessedString_SpecialCharacters_HandlesCorrectly()
    {
        var result = _service.BuildProcessedString("!@#");
        var expectedBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("!@#"));
        result.Should().Contain("!1");
        result.Should().Contain("@1");
        result.Should().Contain("#1");
        result.Should().EndWith($"/{expectedBase64}");
    }

    [Fact]
    public void BuildProcessedString_CharsSortedByAscii()
    {
        var result = _service.BuildProcessedString("bac");
        // ASCII order: a, b, c
        result.Should().StartWith("a1b1c1");
    }

    [Fact]
    public void BuildProcessedString_ContainsSlashSeparator()
    {
        var result = _service.BuildProcessedString("test");
        result.Should().Contain("/");
    }
}
