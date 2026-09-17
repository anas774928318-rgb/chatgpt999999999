using System.Text;
using System.Text.RegularExpressions;

namespace MacChanger.Services;

public static class MacAddressService
{
    private static readonly Random Rng = new();

    /// <summary>
    /// يولّد عنوان MAC عشوائي صالح "locally administered / unicast":
    /// - البت الثاني من أقل ترتيب في البايت الأول = 1 (عنوان مُدار محليًا)
    /// - البت الأول من أقل ترتيب في البايت الأول = 0 (عنوان أحادي البث unicast، ليس multicast)
    /// </summary>
    public static string GenerateRandom()
    {
        var bytes = new byte[6];
        Rng.NextBytes(bytes);

        bytes[0] = (byte)((bytes[0] & 0xFC) | 0x02);

        var sb = new StringBuilder();
        foreach (var b in bytes)
            sb.Append(b.ToString("X2"));

        return sb.ToString(); // 12 خانة بدون فواصل، مثال: 02A1B2C3D4E5
    }

    /// <summary>يتحقق أن النص المُدخل عنوان MAC صالح الصيغة (بفواصل : أو - أو بدون فواصل).</summary>
    public static bool IsValid(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        return Regex.IsMatch(input.Trim(), @"^([0-9A-Fa-f]{2}[:\-]){5}[0-9A-Fa-f]{2}$")
            || Regex.IsMatch(input.Trim(), @"^[0-9A-Fa-f]{12}$");
    }

    /// <summary>يحوّل أي صيغة صالحة إلى 12 خانة سداسية بدون فواصل (الصيغة التي يتطلبها الريجستري).</summary>
    public static string Normalize(string input)
    {
        return input.Trim().Replace(":", "").Replace("-", "").ToUpperInvariant();
    }

    /// <summary>يُعيد الصيغة القابلة للعرض AA:BB:CC:DD:EE:FF من 12 خانة بدون فواصل.</summary>
    public static string ToDisplayFormat(string noSeparators)
    {
        var v = Normalize(noSeparators);
        return string.Join(":", Enumerable.Range(0, v.Length / 2).Select(i => v.Substring(i * 2, 2)));
    }
}
