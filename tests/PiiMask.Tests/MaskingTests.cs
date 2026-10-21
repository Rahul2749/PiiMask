namespace PiiMask.Tests;

public class MaskingTests
{
    [Theory]
    [InlineData("rahul@gmail.com", SensitiveKind.Email, "ra***@gmail.com")]
    [InlineData("a@gmail.com", SensitiveKind.Email, "*@gmail.com")]
    [InlineData("ab@gmail.com", SensitiveKind.Email, "**@gmail.com")]
    [InlineData("not-email", SensitiveKind.Email, "*********")]
    [InlineData("+1 415-555-2671", SensitiveKind.Phone, "+* ***-***-2671")]
    [InlineData("4111 1111 1111 1111", SensitiveKind.CardNumber, "**** **** **** 1111")]
    [InlineData("1234", SensitiveKind.CardNumber, "****")]
    [InlineData("bad", SensitiveKind.Phone, "***")]
    [InlineData("abcdefghi", SensitiveKind.Partial, "*****fghi")]
    [InlineData("abc", SensitiveKind.Partial, "***")]
    [InlineData("abcd", SensitiveKind.Partial, "****")]
    [InlineData("abcdefghijklmnop", SensitiveKind.Token, "abcd********")]
    [InlineData("ab", SensitiveKind.Token, "********")]
    [InlineData("unknown secret", SensitiveKind.Auto, "**************")]
    [InlineData("hello", SensitiveKind.Full, "*****")]
    public void Explicit_values_are_masked(string input, SensitiveKind kind, string expected)
    {
        Assert.Equal(expected, Pii.Mask(input, kind));
    }

    [Theory]
    [InlineData(SensitiveKind.Full)]
    [InlineData(SensitiveKind.Partial)]
    [InlineData(SensitiveKind.Email)]
    [InlineData(SensitiveKind.Phone)]
    [InlineData(SensitiveKind.CardNumber)]
    [InlineData(SensitiveKind.Token)]
    [InlineData(SensitiveKind.Auto)]
    public void Null_and_empty_are_preserved(SensitiveKind kind)
    {
        Assert.Null(Pii.Mask(null, kind));
        Assert.Equal("", Pii.Mask("", kind));
        Assert.Null(Pii.MaskText(null));
        Assert.Equal("", Pii.MaskText(""));
    }

    [Theory]
    [InlineData("rahul@gmail.com", SensitiveKind.Email)]
    [InlineData("+1 415-555-2671", SensitiveKind.Phone)]
    [InlineData("4111-1111-1111-1111", SensitiveKind.CardNumber)]
    [InlineData("abcdefghijklmnop", SensitiveKind.Token)]
    [InlineData("ab", SensitiveKind.Token)]
    [InlineData("abcdefgh", SensitiveKind.Partial)]
    [InlineData("hello", SensitiveKind.Full)]
    public void Explicit_masking_is_idempotent(string input, SensitiveKind kind)
    {
        var once = Pii.Mask(input, kind);
        Assert.Equal(once, Pii.Mask(once, kind));
    }

    [Fact]
    public void Options_are_honored_and_custom_rules_are_snapshotted()
    {
        var patterns = new List<string> { @"EMP-\d+" };
        var masker = new PiiMasker(new()
        {
            MaskCharacter = '#', EmailVisiblePrefix = 1, CardVisibleSuffix = 2,
            PartialVisibleSuffix = 0, CustomPatterns = patterns
        });
        patterns.Clear();
        Assert.Equal("r####@gmail.com", masker.Mask("rahul@gmail.com", SensitiveKind.Email));
        Assert.Equal("#### #### #### ##11", masker.Mask("4111 1111 1111 1111", SensitiveKind.CardNumber));
        Assert.Equal("###", masker.Mask("abc", SensitiveKind.Partial));
        Assert.Equal("id=#######", masker.MaskText("id=EMP-123"));
    }

    [Fact]
    public void Unsafe_or_invalid_options_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PiiMasker(new() { MaskCharacter = '1' }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PiiMasker(new() { CardVisibleSuffix = 16 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PiiMasker(new() { TokenMaskLength = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PiiMasker(new() { CustomRegexTimeout = Timeout.InfiniteTimeSpan }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PiiMasker(new() { DetectTypes = (PiiTypes)128 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Pii.Mask("x", (SensitiveKind)100));
        Assert.ThrowsAny<ArgumentException>(() => new PiiMasker(new() { CustomPatterns = ["["] }));
    }

    [Fact]
    public void A_partially_masked_email_with_a_malformed_domain_fails_closed()
    {
        const string input = "ra***@example.com secret";
        Assert.Equal(new string('*', input.Length), Pii.Mask(input, SensitiveKind.Email));
    }
}
