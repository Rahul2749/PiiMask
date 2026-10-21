using System.Text.RegularExpressions;

namespace PiiMask;

internal static partial class Patterns
{
    [GeneratedRegex(@"(?<![\w.+%*-])[A-Z0-9!#$%&'+/=?^_`{|}~-]+(?:\.[A-Z0-9!#$%&'+/=?^_`{|}~-]+)*@(?:[A-Z0-9](?:[A-Z0-9-]*[A-Z0-9])?\.)+[A-Z]{2,63}(?![\w-])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 100)]
    internal static partial Regex Email();

    [GeneratedRegex("""\bBearer[ \t]+(?<value>[A-Z0-9._~+/-]+=*)""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 100)]
    internal static partial Regex Bearer();

    [GeneratedRegex("""(?<![\w])["']?(?:password|passwd|pwd|secret|client_secret|api[_-]?key|access[_-]?token|refresh[_-]?token)["']?[ \t]*[:=][ \t]*(?:"(?<value>(?:\\.|[^"\\])*)"|'(?<value>(?:\\.|[^'\\])*)'|(?<value>[^\s,;&}\]"']+))""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 100)]
    internal static partial Regex Credential();

    [GeneratedRegex(@"(?<![\w])(?<value>eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+|(?:AKIA|ASIA)[A-Z0-9]{16}|gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,}|[sr]k_(?:live|test)_[A-Za-z0-9]{16,}|xox[baprs]-[A-Za-z0-9-]{10,})(?![\w])", RegexOptions.CultureInvariant, 100)]
    internal static partial Regex Token();

    [GeneratedRegex(@"(?<![\w.+-])[0-9](?:[ -]?[0-9]){12,18}(?![ -]?[0-9]|\w|\.[0-9])", RegexOptions.CultureInvariant, 100)]
    internal static partial Regex Card();

    [GeneratedRegex(@"(?<![\w.+-])(?:\+[0-9]|[0-9]|\([0-9])[0-9 ()-]*[0-9](?![\w]|\.[0-9])", RegexOptions.CultureInvariant, 100)]
    internal static partial Regex Phone();

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}(?:\b|$)|^\d{2}-\d{2}-\d{4}(?:\b|$)", RegexOptions.CultureInvariant, 100)]
    internal static partial Regex Date();
}
