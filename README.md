# MacChanger — مغيّر عنوان MAC لكرت الواي فاي (Windows / .NET 8 / WPF)

تطبيق سطح مكتب لويندوز يتيح لك:
- اكتشاف محولات الواي فاي المتصلة بالجهاز.
- عرض عنوان MAC الحالي.
- توليد عنوان MAC عشوائي صالح (unicast / locally administered).
- إدخال عنوان MAC يدويًا والتحقق من صحة صيغته.
- تطبيق العنوان الجديد عبر الريجستري وإعادة تشغيل المحول تلقائيًا.
- استعادة العنوان الأصلي (المصنّعي) في أي وقت.

## ⚠️ ملاحظات مهمة قبل الاستخدام

1. **يجب تشغيل البرنامج كمسؤول (Run as Administrator)** — تعديل الريجستري وإعادة تشغيل
   محول الشبكة يتطلبان صلاحيات إدارية. ملف `app.manifest` يطلب هذه الصلاحية تلقائيًا
   عند تشغيل الملف التنفيذي.
2. بعض تعريفات كروت الواي فاي (خصوصًا بعض شرائح Intel وBroadcom) **لا تدعم** خاصية
   `NetworkAddress` من الريجستري إطلاقًا؛ في هذه الحالة سيفشل التطبيق أو لن يتغيّر
   العنوان الفعلي رغم نجاح الكتابة في الريجستري.
3. تغيير عنوان MAC مفيد لأغراض مشروعة مثل الخصوصية، اختبار الشبكات، أو تفادي القيود
   المرتبطة بعنوان جهاز معيّن على شبكتك الخاصة. **لا تستخدمه لانتحال هوية جهاز آخر
   على شبكة لا تملك صلاحية عليها.**
4. عند تطبيق عنوان جديد، سينقطع اتصال الواي فاي لثوانٍ (يتم تعطيل المحول ثم تفعيله)
   وقد تحتاج لإعادة إدخال كلمة مرور الشبكة إذا لم تكن محفوظة.

## بنية المشروع

```
MacChanger/
├── MacChanger.sln
├── MacChanger/
│   ├── MacChanger.csproj
│   ├── app.manifest              # طلب صلاحيات المسؤول
│   ├── App.xaml / App.xaml.cs
│   ├── MainWindow.xaml / .xaml.cs   # الواجهة الرسومية
│   ├── Models/NetworkAdapterInfo.cs
│   └── Services/
│       ├── NetworkAdapterService.cs  # اكتشاف + ريجستري + WMI لإعادة التشغيل
│       └── MacAddressService.cs      # توليد/تحقق/تنسيق عناوين MAC
└── .github/workflows/build.yml    # بناء تلقائي عبر GitHub Actions
```

## البناء يدويًا على ويندوز

يتطلب [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
cd MacChanger
dotnet restore MacChanger\MacChanger.csproj
dotnet publish MacChanger\MacChanger.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

سيظهر الملف الجاهز في `publish\MacChanger.exe`. شغّله بالنقر بزر الفأرة الأيمن ثم
"Run as administrator".

## البناء تلقائيًا عبر GitHub Actions (مفيد إن كنت تستخدم Jules أو لا تملك ويندوز محليًا)

1. أنشئ مستودع GitHub جديد وارفع محتوى هذا المجلد إليه:
   ```bash
   git init
   git add .
   git commit -m "Initial commit: MacChanger WPF app"
   git branch -M main
   git remote add origin <رابط-مستودعك-على-GitHub>
   git push -u origin main
   ```
2. بمجرد الرفع، سيعمل الـ workflow الموجود في `.github/workflows/build.yml` تلقائيًا
   على عامل ويندوز (`windows-latest`) ويُنتج ملف `MacChanger.exe` جاهزًا.
3. من تبويب **Actions** في المستودع، افتح آخر تشغيل ناجح للـ workflow، وحمّل
   الملف من قسم **Artifacts** باسم `MacChanger-exe`.
4. يمكنك أيضًا تشغيله يدويًا في أي وقت من تبويب Actions عبر زر **Run workflow**
   (مفعّل بفضل `workflow_dispatch` في الملف).

بهذا يمكن لأداة مثل Jules (أو أي بيئة CI متصلة بالمستودع) أن تدفع تعديلاتك على الكود
ويقوم GitHub تلقائيًا ببناء ملف exe صالح للتشغيل على ويندوز دون الحاجة لجهاز ويندوز فعلي.

## كيف يعمل تقنيًا (باختصار)

- يتم العثور على مفتاح الريجستري الخاص بكل محول تحت
  `HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e972-...}\XXXX` بمطابقة
  قيمة `NetCfgInstanceId` مع معرّف الواجهة (`NetworkInterface.Id`).
- يُكتب العنوان الجديد في قيمة `NetworkAddress` (نص من 12 خانة سداسية بدون فواصل).
- في أول تعديل، يُحفظ العنوان الحقيقي الحالي في قيمة مخصّصة
  `MacChanger_OriginalMac` لاستخدامها لاحقًا عند الاستعادة.
- تتم إعادة تشغيل المحول عبر WMI (`Win32_NetworkAdapter.Disable/Enable`) حتى
  يُعاد تحميل تعريف الجهاز بالعنوان الجديد.
- الاستعادة تحذف قيمة `NetworkAddress` فقط، فيعود النظام لاستخدام عنوان الجهاز
  الفعلي (المحروق في الشريحة) تلقائيًا.
