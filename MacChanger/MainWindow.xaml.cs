using System.Windows;
using MacChanger.Models;
using MacChanger.Services;

namespace MacChanger;

public partial class MainWindow : Window
{
    private readonly NetworkAdapterService _adapterService = new();
    private List<NetworkAdapterInfo> _adapters = new();

    public MainWindow()
    {
        InitializeComponent();
        LoadAdapters();
    }

    private void LoadAdapters()
    {
        try
        {
            _adapters = _adapterService.GetWifiAdapters();
            AdaptersCombo.ItemsSource = null;
            AdaptersCombo.ItemsSource = _adapters;

            if (_adapters.Count > 0)
            {
                AdaptersCombo.SelectedIndex = 0;
                Log($"تم العثور على {_adapters.Count} محول واي فاي.");
            }
            else
            {
                Log("لم يتم العثور على كرت واي فاي فيزيائي حقيقي (تم استبعاد المحولات الافتراضية تلقائيًا). " +
                    "تأكد أن الواي فاي مفعّل من إعدادات ويندوز، وأن تعريف الكرت مثبّت بشكل صحيح.");
            }
        }
        catch (Exception ex)
        {
            Log("خطأ أثناء قراءة محولات الشبكة: " + ex.Message);
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e) => LoadAdapters();

    private void AdaptersCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateInfoPanel();
    }

    private void UpdateInfoPanel()
    {
        var adapter = AdaptersCombo.SelectedItem as NetworkAdapterInfo;
        if (adapter is null)
        {
            CurrentMacText.Text = "—";
            FactoryMacText.Text = "غير محفوظ بعد";
            return;
        }

        CurrentMacText.Text = adapter.CurrentMac;
        FactoryMacText.Text = adapter.FactoryMac ?? "غير محفوظ بعد (لم يتم تغيير العنوان من قبل هذا البرنامج)";
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        var mac = MacAddressService.GenerateRandom();
        NewMacTextBox.Text = MacAddressService.ToDisplayFormat(mac);
        Log("تم توليد عنوان MAC عشوائي: " + NewMacTextBox.Text);
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        var adapter = AdaptersCombo.SelectedItem as NetworkAdapterInfo;
        if (adapter is null)
        {
            MessageBox.Show("الرجاء اختيار محول واي فاي أولًا.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var input = NewMacTextBox.Text;
        if (!MacAddressService.IsValid(input))
        {
            MessageBox.Show("صيغة عنوان MAC غير صحيحة. مثال صحيح: AA:BB:CC:DD:EE:FF",
                "خطأ في الإدخال", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var normalized = MacAddressService.Normalize(input);

        try
        {
            Log($"جارٍ تطبيق العنوان {MacAddressService.ToDisplayFormat(normalized)} على {adapter.Name} ...");
            _adapterService.SetMacAddress(adapter, normalized);
            Log("تم التطبيق بنجاح، وأُعيد تشغيل المحول.");
            LoadAdapters();
        }
        catch (Exception ex)
        {
            Log("فشل تطبيق العنوان: " + ex.Message);
            MessageBox.Show(
                "فشلت العملية. تأكد من تشغيل البرنامج كمسؤول (Run as Administrator)، " +
                "ومن أن تعريف كرت الشبكة يدعم خاصية Network Address.\n\nالتفاصيل: " + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        var adapter = AdaptersCombo.SelectedItem as NetworkAdapterInfo;
        if (adapter is null)
        {
            MessageBox.Show("الرجاء اختيار محول واي فاي أولًا.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Log($"جارٍ استعادة العنوان الأصلي لـ {adapter.Name} ...");
            _adapterService.RestoreOriginalMacAddress(adapter);
            Log("تمت الاستعادة بنجاح، وأُعيد تشغيل المحول.");
            LoadAdapters();
        }
        catch (Exception ex)
        {
            Log("فشلت الاستعادة: " + ex.Message);
            MessageBox.Show(
                "فشلت العملية. تأكد من تشغيل البرنامج كمسؤول (Run as Administrator).\n\nالتفاصيل: " + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Log(string message)
    {
        LogTextBox.Text += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
        LogTextBox.ScrollToEnd();
    }
}
