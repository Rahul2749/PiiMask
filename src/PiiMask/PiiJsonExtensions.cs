using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace PiiMask;

/// <summary>Reflection-based System.Text.Json masking integration.</summary>
public static class PiiJsonExtensions
{
    /// <summary>
    /// Adds masking of annotated string members and omission of annotated non-string members.
    /// Call before first serialization. Only the default reflection resolver is supported.
    /// Calling again on the same mutable options replaces the masker.
    /// </summary>
    [RequiresUnreferencedCode("PiiMask JSON integration requires reflected member attributes and is not supported with trimming or NativeAOT.")]
    [RequiresDynamicCode("PiiMask JSON integration requires reflection-based System.Text.Json serialization.")]
    public static JsonSerializerOptions AddPiiMasking(this JsonSerializerOptions options, PiiMasker? masker = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.IsReadOnly)
            throw new InvalidOperationException("Configure PII masking before using the JSON options.");
        var resolver = options.TypeInfoResolver;
        if (resolver is MaskingResolver existing)
            resolver = existing.Inner;
        if (resolver is not null && resolver.GetType() != typeof(DefaultJsonTypeInfoResolver))
            throw new NotSupportedException("PiiMask supports only DefaultJsonTypeInfoResolver. Source-generated contexts and custom resolver chains are not supported.");
        options.TypeInfoResolver = new MaskingResolver(
            (DefaultJsonTypeInfoResolver?)resolver ?? new DefaultJsonTypeInfoResolver(), masker ?? new PiiMasker());
        return options;
    }

    private sealed class MaskingResolver(DefaultJsonTypeInfoResolver inner, PiiMasker masker) : IJsonTypeInfoResolver
    {
        internal DefaultJsonTypeInfoResolver Inner => inner;

        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            var info = inner.GetTypeInfo(type, options);
            if (info.Kind != JsonTypeInfoKind.Object)
                return info;
            foreach (var property in info.Properties)
            {
                var attribute = property.AttributeProvider is MemberInfo member
                    ? (SensitiveAttribute?)Attribute.GetCustomAttribute(member, typeof(SensitiveAttribute), inherit: true)
                    : property.AttributeProvider?.GetCustomAttributes(typeof(SensitiveAttribute), true)
                        .OfType<SensitiveAttribute>().FirstOrDefault();
                if (attribute is null)
                    continue;
                if (property.PropertyType != typeof(string))
                {
                    property.ShouldSerialize = (_, _) => false;
                    property.Get = null;
                    continue;
                }
                var getter = property.Get;
                if (getter is not null)
                    property.Get = obj => masker.Mask((string?)getter(obj), attribute.Kind);
            }
            return info;
        }
    }
}
