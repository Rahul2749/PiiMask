namespace PiiMask.Tests;

public class DetectionTests
{
    [Theory]
    [InlineData("Email rahul@gmail.com today.", "Email ra***@gmail.com today.")]
    [InlineData("Call +1 415-555-2671.", "Call +* ***-***-2671.")]
    [InlineData("Call (415) 555-2671.", "Call (***) ***-2671.")]
    [InlineData("Card 4111 1111 1111 1111.", "Card **** **** **** 1111.")]
    [InlineData("Card 4111111111111111.", "Card ************1111.")]
    [InlineData("Authorization: Bearer abcdefghijklmnop", "Authorization: Bearer abcd********")]
    [InlineData("password=hunter2", "password=hunt********")]
    [InlineData("{\"api_key\": \"abcdefghijk\"}", "{\"api_key\": \"abcd********\"}")]
    [InlineData("secret='some secret words'", "secret='some********'")]
    [InlineData("key AKIAIOSFODNN7EXAMPLE", "key AKIA********")]
    [InlineData("key ghp_abcdefghijklmnopqrstuvwxyz", "key ghp_********")]
    [InlineData("key github_pat_abcdefghijklmnopqrstuvwxyz", "key gith********")]
    [InlineData("key sk_test_abcdefghijklmnopqrstuv", "key sk_t********")]
    [InlineData("key xoxb-1234567890-abcdefghijk", "key xoxb********")]
    [InlineData("jwt eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.abcdefghijk", "jwt eyJh********")]
    public void Detects_supported_formats(string input, string expected)
    {
        Assert.Equal(expected, Pii.MaskText(input));
        Assert.Equal(expected, Pii.MaskText(expected));
    }

    [Theory]
    [InlineData("Invalid card 4111 1111 1111 1112")]
    [InlineData("Invalid card 4111111111111112")]
    [InlineData("Zeros 0000000000000000")]
    [InlineData("Date 2026-10-20")]
    [InlineData("Date 2026-10-20 12:30:00")]
    [InlineData("Version 10.0.303")]
    [InlineData("Epoch 1729425600")]
    [InlineData("Epoch 1729425600000")]
    [InlineData("Price 1234567890.12")]
    [InlineData("ID abc4111111111111111xyz")]
    [InlineData("Too long 41111111111111111111")]
    [InlineData("ID 4155552671")]
    [InlineData("Ordinary text")]
    public void Avoids_common_false_positives(string input) => Assert.Equal(input, Pii.MaskText(input));

    [Fact]
    public void Bare_phones_require_opt_in()
    {
        var masker = new PiiMasker(new() { DetectBarePhoneNumbers = true });
        Assert.Equal("Call ******2671", masker.MaskText("Call 4155552671"));
    }

    [Fact]
    public void Detector_flags_do_not_affect_explicit_masking()
    {
        var masker = new PiiMasker(new() { DetectTypes = PiiTypes.None });
        Assert.Equal("rahul@gmail.com", masker.MaskText("rahul@gmail.com"));
        Assert.Equal("ra***@gmail.com", masker.Mask("rahul@gmail.com", SensitiveKind.Email));
    }

    [Fact]
    public void Overlaps_cannot_expose_a_password_domain()
    {
        // '=' is legal in an email local part, so the unquoted assignment is
        // ambiguous and its entire overlapping span is deliberately redacted.
        Assert.Equal(new string('*', 24), Pii.MaskText("password=rahul@gmail.com"));
        Assert.Equal("password=\"" + new string('*', 15) + "\"", Pii.MaskText("password=\"rahul@gmail.com\""));
        var masker = new PiiMasker(new() { CustomPatterns = ["gmail"] });
        Assert.Equal(new string('*', 15), masker.MaskText("rahul@gmail.com"));
    }

    [Fact]
    public void Multiple_disjoint_matches_preserve_surrounding_text()
    {
        Assert.Equal("ra***@gmail.com; card=**** **** **** 1111; done",
            Pii.MaskText("rahul@gmail.com; card=4111 1111 1111 1111; done"));
    }

    [Fact]
    public void Custom_regex_timeout_masks_the_entire_input()
    {
        var masker = new PiiMasker(new()
        {
            DetectTypes = PiiTypes.None,
            CustomPatterns = ["(a+)+$"],
            CustomRegexTimeout = TimeSpan.FromMilliseconds(1)
        });
        var input = "prefix " + new string('a', 100_000) + "!";
        Assert.Equal(new string('*', input.Length), masker.MaskText(input));
    }

    [Fact]
    public void One_masker_can_be_shared_by_concurrent_callers()
    {
        var masker = new PiiMasker();
        Parallel.For(0, 200, _ =>
            Assert.Equal("ra***@gmail.com", masker.MaskText("rahul@gmail.com")));
    }
}
