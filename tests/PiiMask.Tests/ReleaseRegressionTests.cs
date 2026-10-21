using System.Text.Json;
using System.Text.Json.Serialization;

namespace PiiMask.Tests;

public class ReleaseRegressionTests
{
    [Theory]
    [InlineData("a.b@example.com", "a.*@example.com")]
    [InlineData("o'connor@example.com", "o'******@example.com")]
    [InlineData("+alias@example.com", "+a****@example.com")]
    [InlineData("!user@example.com", "!u***@example.com")]
    public void Repeated_email_masking_preserves_supported_punctuation(string input, string expected)
    {
        Assert.Equal(expected, Pii.Mask(input, SensitiveKind.Email));
        Assert.Equal(expected, Pii.Mask(expected, SensitiveKind.Email));
    }

    [Theory]
    [InlineData('*')]
    [InlineData('#')]
    [InlineData('?')]
    public void Configured_email_prefix_and_character_remain_stable(char mask)
    {
        var masker = new PiiMasker(new() { MaskCharacter = mask, EmailVisiblePrefix = 6 });
        var expected = "john.d" + new string(mask, 2) + "@example.com";
        Assert.Equal(expected, masker.Mask("john.doe@example.com", SensitiveKind.Email));
        Assert.Equal(expected, masker.Mask(expected, SensitiveKind.Email));
    }

    [Theory]
    [InlineData("a.***@example.com trailing-secret")]
    [InlineData("o'***@example.com@attacker.invalid")]
    [InlineData("ra***@example.com/secret")]
    public void Malformed_partially_masked_email_is_fully_hidden(string input)
    {
        Assert.Equal(new string('*', input.Length), Pii.Mask(input, SensitiveKind.Email));
    }

    [Theory]
    [InlineData(SensitiveKind.Phone, "+1 415-555-2671", "+# ###-###-####")]
    [InlineData(SensitiveKind.CardNumber, "4111 1111 1111 1111", "#### #### #### ####")]
    [InlineData(SensitiveKind.Partial, "customer-1234", "#############")]
    [InlineData(SensitiveKind.Token, "abcdefghijk", "########")]
    public void Zero_visible_counts_hide_every_sensitive_character(SensitiveKind kind, string input, string expected)
    {
        var masker = new PiiMasker(new()
        {
            MaskCharacter = '#', PhoneVisibleSuffix = 0, CardVisibleSuffix = 0,
            PartialVisibleSuffix = 0, TokenVisiblePrefix = 0
        });
        Assert.Equal(expected, masker.Mask(input, kind));
        Assert.Equal(expected, masker.Mask(expected, kind));
    }

    [Theory]
    [InlineData("API_KEY=\"abcdefghijk\"", "API_KEY=\"abcd********\"")]
    [InlineData("client_secret='abcdefghijk'", "client_secret='abcd********'")]
    [InlineData("refresh-token=abcdefghijk", "refresh-token=abcd********")]
    [InlineData("secret=\"word with spaces\"", "secret=\"word********\"")]
    public void Quoted_and_case_insensitive_credentials_are_stable(string input, string expected)
    {
        Assert.Equal(expected, Pii.MaskText(input));
        Assert.Equal(expected, Pii.MaskText(expected));
    }

    [Theory]
    [InlineData("password=ab", "password=********")]
    [InlineData("password=abcd", "password=********")]
    [InlineData("secret='ab'", "secret='********'")]
    public void Repeated_scanning_of_fully_masked_short_credentials_is_stable(string input, string expected)
    {
        Assert.Equal(expected, Pii.MaskText(input));
        Assert.Equal(expected, Pii.MaskText(expected));
    }

    [Fact]
    public void Custom_rules_run_with_built_in_detection_disabled_and_zero_length_matches_are_ignored()
    {
        var masker = new PiiMasker(new() { DetectTypes = PiiTypes.None, CustomPatterns = ["^", @"EMP-\d{6}"] });
        Assert.Equal("Employee **********; rahul@example.com", masker.MaskText("Employee EMP-123456; rahul@example.com"));
    }

    [Fact]
    public void Timeout_discards_earlier_redactions_and_hides_the_entire_message()
    {
        var masker = new PiiMasker(new()
        {
            DetectTypes = PiiTypes.Email, CustomPatterns = ["(a+)+$"],
            CustomRegexTimeout = TimeSpan.FromMilliseconds(1)
        });
        var input = "rahul@example.com " + new string('a', 100_000) + "!";
        Assert.Equal(new string('*', input.Length), masker.MaskText(input));
    }

    [Fact]
    public void Ignored_members_required_values_and_nulls_keep_json_semantics()
    {
        var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }.AddPiiMasking();
        var value = new RequiredValues { SecretId = 42, Email = "rahul@example.com" };
        Assert.Equal("{\"Email\":\"ra***@example.com\"}", JsonSerializer.Serialize(value, options));
        var restored = JsonSerializer.Deserialize<RequiredValues>("{\"SecretId\":42,\"Email\":\"rahul@example.com\"}", options)!;
        Assert.Equal(42, restored.SecretId);
        Assert.Equal("rahul@example.com", restored.Email);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<RequiredValues>("{}", options));
    }

    [Fact]
    public void Annotated_objects_and_collections_are_omitted_as_whole_values()
    {
        var options = new JsonSerializerOptions().AddPiiMasking();
        Assert.Equal("{\"Public\":\"visible\"}", JsonSerializer.Serialize(new SensitiveContainers(), options));
    }

    public sealed class RequiredValues
    {
        [Sensitive] public required int SecretId { get; init; }
        [Sensitive(SensitiveKind.Email)] public string? Email { get; init; }
        [Sensitive] public string? Optional { get; init; }
        [Sensitive, JsonIgnore] public string Ignored => throw new InvalidOperationException("Ignored getter must not run.");
    }

    public sealed class SensitiveContainers
    {
        public string Public => "visible";
        [Sensitive] public object Secret => throw new InvalidOperationException("Sensitive getter must not run.");
        [Sensitive] public string[] Secrets => throw new InvalidOperationException("Sensitive getter must not run.");
    }
}
