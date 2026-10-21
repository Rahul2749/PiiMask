using System.Text.Json;
using PiiMask;

var masked = Pii.MaskText("Email rahul@gmail.com; card=4111 1111 1111 1111");
if (masked != "Email ra***@gmail.com; card=**** **** **** 1111")
    throw new InvalidOperationException("Packaged text masking failed.");

var repeatedEmail = Pii.Mask(Pii.Mask("a.b@example.com", SensitiveKind.Email), SensitiveKind.Email);
if (repeatedEmail != "a.*@example.com")
    throw new InvalidOperationException("Packaged repeated email masking regression check failed.");

var options = new JsonSerializerOptions().AddPiiMasking();
var json = JsonSerializer.Serialize(new Customer("rahul@gmail.com", 42), options);
if (json != """{"Email":"ra***@gmail.com"}""")
    throw new InvalidOperationException("Packaged JSON masking failed.");

Console.WriteLine($"Package smoke test passed on {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}.");

public sealed record Customer(
    [property: Sensitive(SensitiveKind.Email)] string Email,
    [property: Sensitive] int InternalId);
