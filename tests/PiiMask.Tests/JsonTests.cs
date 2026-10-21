using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace PiiMask.Tests;

public class JsonTests
{
    [Fact]
    public void Attributes_mask_strings_and_omit_other_types_without_mutating_the_object()
    {
        var value = new Customer { Email = "rahul@gmail.com", Password = "hunter2", Age = 42 };
        var options = new JsonSerializerOptions().AddPiiMasking();
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(value, options));
        Assert.Equal("ra***@gmail.com", doc.RootElement.GetProperty("Email").GetString());
        Assert.Equal("*******", doc.RootElement.GetProperty("Password").GetString());
        Assert.False(doc.RootElement.TryGetProperty("Age", out _));
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("NullableSecret").ValueKind);
        Assert.Equal("rahul@gmail.com", value.Email);
        Assert.Equal("hunter2", value.Password);
        Assert.Equal(JsonSerializer.Serialize(value, options), JsonSerializer.Serialize(value, options));
    }

    [Fact]
    public void Nested_collections_records_and_naming_policies_work()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }.AddPiiMasking();
        var json = JsonSerializer.Serialize(new { People = new[] { new Person("rahul@gmail.com") } }, options);
        Assert.Equal("{\"people\":[{\"email\":\"ra***@gmail.com\"}]}", json);
    }

    [Fact]
    public void All_kinds_are_applied()
    {
        var json = JsonSerializer.Serialize(new Kinds(), new JsonSerializerOptions().AddPiiMasking());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("*****fghi", doc.RootElement.GetProperty("Partial").GetString());
        Assert.Equal("+* ***-***-2671", doc.RootElement.GetProperty("Phone").GetString());
        Assert.Equal("************1111", doc.RootElement.GetProperty("Card").GetString());
        Assert.Equal("abcd********", doc.RootElement.GetProperty("Token").GetString());
        Assert.Equal("*******", doc.RootElement.GetProperty("Auto").GetString());
    }

    [Fact]
    public void Sensitive_non_string_getters_are_not_invoked()
    {
        Assert.Equal("{}", JsonSerializer.Serialize(new ThrowingGetter(), new JsonSerializerOptions().AddPiiMasking()));
    }

    [Fact]
    public void Included_fields_and_extension_data_cannot_leak()
    {
        var options = new JsonSerializerOptions { IncludeFields = true }.AddPiiMasking();
        var json = JsonSerializer.Serialize(new Fields(), options);
        Assert.Equal("{\"Secret\":\"******\"}", json);
    }

    [Fact]
    public void Deserialization_is_unchanged()
    {
        var options = new JsonSerializerOptions().AddPiiMasking();
        var result = JsonSerializer.Deserialize<Customer>("""{"Email":"rahul@gmail.com","Password":"hunter2","Age":42}""", options)!;
        Assert.Equal("rahul@gmail.com", result.Email);
        Assert.Equal(42, result.Age);
    }

    [Fact]
    public void Existing_modifiers_are_preserved_and_repeat_registration_replaces_masker()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(info =>
        {
            foreach (var property in info.Properties)
                property.Name = property.Name.ToLowerInvariant();
        });
        var options = new JsonSerializerOptions { TypeInfoResolver = resolver };
        options.AddPiiMasking().AddPiiMasking(new(new() { MaskCharacter = '#' }));
        Assert.Equal("{\"email\":\"ra###@gmail.com\"}", JsonSerializer.Serialize(new Person("rahul@gmail.com"), options));
        Assert.Throws<InvalidOperationException>(() => options.AddPiiMasking());
    }

    [Fact]
    public void Source_generated_contexts_are_rejected()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = TestJsonContext.Default };
        Assert.Throws<NotSupportedException>(() => options.AddPiiMasking());
    }

    [Fact]
    public void Overridden_properties_inherit_sensitive_annotations()
    {
        Assert.Equal("{\"Secret\":\"******\"}",
            JsonSerializer.Serialize(new Derived(), new JsonSerializerOptions().AddPiiMasking()));
    }

    public class Base
    {
        [Sensitive] public virtual string Secret => "hidden";
    }

    public sealed class Derived : Base
    {
        public override string Secret => "hidden";
    }

    public sealed class Customer
    {
        [Sensitive(SensitiveKind.Email)] public string? Email { get; set; }
        [Sensitive] public string? Password { get; set; }
        [Sensitive] public string? NullableSecret { get; set; }
        [Sensitive] public int Age { get; set; }
    }

    public sealed record Person([property: Sensitive(SensitiveKind.Email)] string Email);

    public sealed class Kinds
    {
        [Sensitive(SensitiveKind.Partial)] public string Partial => "abcdefghi";
        [Sensitive(SensitiveKind.Phone)] public string Phone => "+1 415-555-2671";
        [Sensitive(SensitiveKind.CardNumber)] public string Card => "4111111111111111";
        [Sensitive(SensitiveKind.Token)] public string Token => "abcdefghijk";
        [Sensitive(SensitiveKind.Auto)] public string Auto => "unknown";
    }

    public sealed class ThrowingGetter
    {
        [Sensitive] public int Secret => throw new InvalidOperationException("Must never read this.");
    }

    public sealed class Fields
    {
        [Sensitive] public string Secret = "hidden";
        [Sensitive, JsonExtensionData] public Dictionary<string, object> Extra { get; set; } = new() { ["leak"] = "hidden" };
    }
}

[JsonSerializable(typeof(JsonTests.Customer))]
internal partial class TestJsonContext : JsonSerializerContext;
