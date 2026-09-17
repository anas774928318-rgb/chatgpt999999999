namespace MacChanger.Models;

public class NetworkAdapterInfo
{
    // معرّف الواجهة كما يظهر في NetworkInterface.Id (يطابق NetCfgInstanceId في الريجستري)
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    // عنوان MAC الحالي الفعلي (كما يقرأه النظام الآن) بصيغة AA:BB:CC:DD:EE:FF
    public string CurrentMac { get; set; } = string.Empty;

    // عنوان MAC الأصلي كما ورد من المصنّع (قبل أي تعديل)، يُقرأ من الجهاز إن أمكن
    public string? FactoryMac { get; set; }

    // مسار مفتاح الريجستري الخاص بهذا المحول تحت فئة محولات الشبكة
    public string RegistryKeyPath { get; set; } = string.Empty;

    public override string ToString() => $"{Name} — {Description}";
}
