using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace November.Bff.Api.Infrastructure.Validation;

public static partial class BankFormats
{
    public const int ReferenceMaxLength = 35;

    public static bool IsValidIban(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var iban = NormaliseIban(value);
        if (iban.Length is < 15 or > 34 || !iban.All(char.IsAsciiLetterOrDigit))
        {
            return false;
        }

        if (!char.IsAsciiLetterUpper(iban[0]) || !char.IsAsciiLetterUpper(iban[1]) ||
            !char.IsAsciiDigit(iban[2]) || !char.IsAsciiDigit(iban[3]))
        {
            return false;
        }

        return Mod97(iban[4..] + iban[..4]) == 1;
    }

    public static string CreateIban(string countryCode, string bban)
    {
        var check = 98 - (int)Mod97(bban + countryCode + "00");
        return $"{countryCode}{check:00}{bban}";
    }

    public static string NormaliseIban(string value) =>
        value.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();

    public static bool IsValidSwiftBic(string? value) =>
        !string.IsNullOrEmpty(value) && SwiftBicPattern().IsMatch(value);

    public static bool IsValidRoutingNumber(string? value) =>
        !string.IsNullOrEmpty(value) && RoutingNumberPattern().IsMatch(value);

    public static bool IsValidAccountNumber(string? value) =>
        !string.IsNullOrEmpty(value) && AccountNumberPattern().IsMatch(value);

    private static BigInteger Mod97(string text)
    {
        var digits = string.Concat(text.Select(c =>
            char.IsAsciiLetter(c) ? (char.ToUpperInvariant(c) - 'A' + 10).ToString(CultureInfo.InvariantCulture) : c.ToString()));
        return BigInteger.Parse(digits, CultureInfo.InvariantCulture) % 97;
    }

    [GeneratedRegex("^[A-Z]{4}[A-Z]{2}[A-Z0-9]{2}([A-Z0-9]{3})?$")]
    private static partial Regex SwiftBicPattern();

    [GeneratedRegex("^[0-9]{9}$")]
    private static partial Regex RoutingNumberPattern();

    [GeneratedRegex("^[A-Za-z0-9]{4,34}$")]
    private static partial Regex AccountNumberPattern();
}
