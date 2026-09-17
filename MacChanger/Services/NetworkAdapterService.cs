using System.Management;
using System.Net.NetworkInformation;
using Microsoft.Win32;
using MacChanger.Models;

namespace MacChanger.Services;

/// <summary>
/// كل التعامل مع محولات الشبكة اللاسلكية: الاكتشاف، قراءة/كتابة الريجستري،
/// وإعادة تشغيل المحول بعد التعديل حتى يتم تطبيق العنوان الجديد فعليًا.
/// يتطلب تشغيل البرنامج بصلاحيات المسؤول (Administrator).
/// </summary>
public class NetworkAdapterService
{
    // GUID الثابت لفئة "محولات الشبكة" في الريجستري (Network Adapters Class)
    private const string NetworkClassGuid = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";
    private const string NetworkAddressValueName = "NetworkAddress";
    private const string OriginalMacBackupValueName = "MacChanger_OriginalMac";

    /// <summary>يرجع كل محولات الواي فاي (Wireless80211) المتوفرة على الجهاز.</summary>
    public List<NetworkAdapterInfo> GetWifiAdapters()
    {
        var result = new List<NetworkAdapterInfo>();

        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211);

        foreach (var nic in interfaces)
        {
            var keyPath = FindRegistryKeyForAdapter(nic.Id);
            if (keyPath is null)
                continue; // لم نتمكن من مطابقة المحول بمفتاح ريجستري، نتجاهله لتفادي تعديل خاطئ

            string? factoryMac = ReadOriginalMacBackup(keyPath);

            result.Add(new NetworkAdapterInfo
            {
                Id = nic.Id,
                Name = nic.Name,
                Description = nic.Description,
                CurrentMac = FormatMac(nic.GetPhysicalAddress().ToString()),
                FactoryMac = factoryMac,
                RegistryKeyPath = keyPath
            });
        }

        return result;
    }

    /// <summary>يبحث عن المفتاح الفرعي (0000, 0001, ...) الذي يطابق NetCfgInstanceId مع معرّف الواجهة.</summary>
    private string? FindRegistryKeyForAdapter(string interfaceId)
    {
        using var classKey = Registry.LocalMachine.OpenSubKey(NetworkClassGuid, writable: false);
        if (classKey is null) return null;

        foreach (var subKeyName in classKey.GetSubKeyNames())
        {
            if (!subKeyName.All(char.IsDigit)) continue; // نتجاهل المفاتيح غير الرقمية مثل "Properties"

            using var sub = classKey.OpenSubKey(subKeyName, writable: false);
            var netCfgId = sub?.GetValue("NetCfgInstanceId") as string;

            if (netCfgId != null && netCfgId.Equals(interfaceId, StringComparison.OrdinalIgnoreCase))
            {
                return $@"{NetworkClassGuid}\{subKeyName}";
            }
        }

        return null;
    }

    /// <summary>يقرأ نسخة MAC الأصلية المحفوظة سابقًا (إن وُجدت) قبل أول تعديل.</summary>
    private string? ReadOriginalMacBackup(string keyPath)
    {
        using var key = Registry.LocalMachine.OpenSubKey(keyPath, writable: false);
        var value = key?.GetValue(OriginalMacBackupValueName) as string;
        return string.IsNullOrWhiteSpace(value) ? null : FormatMac(value);
    }

    /// <summary>
    /// يضبط عنوان MAC جديد لمحول معيّن عبر الريجستري، ثم يعيد تشغيل المحول لتطبيقه.
    /// يحفظ العنوان الفعلي الحالي كـ"أصلي" في أول مرة فقط، حتى تتمكن لاحقًا من الاستعادة.
    /// </summary>
    /// <param name="adapter">المحول المستهدف (من GetWifiAdapters)</param>
    /// <param name="newMacNoSeparators">عنوان MAC الجديد بصيغة 12 خانة سداسية بدون فواصل، مثال: 02AABBCCDDEE</param>
    public void SetMacAddress(NetworkAdapterInfo adapter, string newMacNoSeparators)
    {
        using var key = Registry.LocalMachine.OpenSubKey(adapter.RegistryKeyPath, writable: true)
            ?? throw new InvalidOperationException("تعذر فتح مفتاح الريجستري للمحول. تأكد من تشغيل البرنامج كمسؤول.");

        // نحفظ العنوان الحقيقي الحالي كنسخة احتياطية أول مرة فقط (حتى لا تُستبدل بعنوان مزيّف لاحقًا)
        if (key.GetValue(OriginalMacBackupValueName) is null)
        {
            var currentReal = adapter.CurrentMac.Replace(":", "").Replace("-", "");
            key.SetValue(OriginalMacBackupValueName, currentReal, RegistryValueKind.String);
        }

        key.SetValue(NetworkAddressValueName, newMacNoSeparators, RegistryValueKind.String);

        RestartAdapter(adapter.Id);
    }

    /// <summary>يحذف القيمة المخصّصة من الريجستري بحيث يعود المحول لاستخدام عنوانه الأصلي المصنّعي.</summary>
    public void RestoreOriginalMacAddress(NetworkAdapterInfo adapter)
    {
        using var key = Registry.LocalMachine.OpenSubKey(adapter.RegistryKeyPath, writable: true)
            ?? throw new InvalidOperationException("تعذر فتح مفتاح الريجستري للمحول. تأكد من تشغيل البرنامج كمسؤول.");

        if (key.GetValue(NetworkAddressValueName) != null)
            key.DeleteValue(NetworkAddressValueName, throwOnMissingValue: false);

        RestartAdapter(adapter.Id);
    }

    /// <summary>يعيد تشغيل (تعطيل ثم تفعيل) محول الشبكة عبر WMI حتى يأخذ عنوان MAC الجديد مفعوله.</summary>
    private void RestartAdapter(string interfaceId)
    {
        using var searcher = new ManagementObjectSearcher(
            $"SELECT * FROM Win32_NetworkAdapter WHERE GUID='{interfaceId}'");

        foreach (ManagementObject adapter in searcher.Get())
        {
            try
            {
                adapter.InvokeMethod("Disable", null);
                Thread.Sleep(1500);
                adapter.InvokeMethod("Enable", null);
                Thread.Sleep(1500);
            }
            finally
            {
                adapter.Dispose();
            }
        }
    }

    private static string FormatMac(string raw)
    {
        raw = raw.Replace(":", "").Replace("-", "").ToUpperInvariant();
        return string.Join(":", Enumerable.Range(0, raw.Length / 2).Select(i => raw.Substring(i * 2, 2)));
    }
}
