# آخرین تغییرات بازی تانک

تاریخ: ۳۰ اکتبر ۲۰۲۶. این سند تغییرات پس از نسخهٔ `d0e5577` را پوشش می‌دهد.

## تغییرات این تحویل

- دو تانک زیتونی و سرمه‌ای با زره پرجزئیات، شنی و لولهٔ جدا؛ آمار کلاس‌ها حفظ شده است.
- اصلاح اندازه و تماس تانک‌ها با زمین و هماهنگی کولایدر اولیه با سطح قابل‌تخریب.
- منظرهٔ معمولی با کوهستان، جنگل، خانه‌های متروکه، برج نگهبانی و نور عصر.
- محیط برفی با کوه و کاج، برف فشرده، یخ و بارش متحرک که در Pause متوقف می‌شود.
- محیط ROCK با منظرهٔ اختصاصی دره، مسیر ناهموارتر، سکوی شروع امن و بافت سنگ، ترک و شن.
- بهبود پنل‌ها، فونت Barlow و رنگ‌های رابط کاربری.
- سایهٔ نرم زیر تانک‌ها که با شیب زمین هماهنگ و با فاصله از سطح کم‌رنگ می‌شود.
- نور سفید و زرد برخورد که به نارنجی تبدیل و طی ۰٫۶۵ ثانیه محو می‌شود؛ حلقه و جرقه‌های انفجار نیز حفظ شده‌اند. سقف نورهای هم‌زمان دوازده عدد است.

## اجرا

پروژه را با Unity `6000.5.10f1` باز کنید. صحنهٔ `Assets/_Game/Scenes/MVPIntegrated.unity` را اجرا و از صفحهٔ آماده‌سازی GRASS، SNOW یا ROCK را انتخاب کنید. حرکت A/D، نشانه‌گیری W/S، شلیک Space و انتخاب سلاح با کلیدهای عددی است. کنترل‌های روی صفحه نیز موجودند.

## فایل‌های مهم

| فایل یا پوشه | کاربرد |
|---|---|
| `Assets/_Game/Tank/TankLoadout.cs` | اتصال بدنه، شنی، لوله و دهانهٔ تیر |
| `Assets/_Game/Data/TankDefinition.cs` | داده‌های کلاس و ظاهر دو طرف |
| `Assets/_Game/Terrain/DestructibleTerrain.cs` | سطح، گودال، سوختگی، ارتفاع و کولایدر |
| `Assets/_Game/Terrain/TerrainThemeController.cs` | انتخاب محیط و تغییر هندسهٔ زمین تخریب‌نشده |
| `Assets/_Game/Terrain/SnowEnvironment.cs` | منظره و بارش برف |
| `Assets/_Game/Terrain/ValleyEnvironment.cs` | منظرهٔ معمولی و صخره‌ای |
| `Assets/_Game/Presentation/BattleFeedback.cs` | جرقه، حلقه، صدا و بازخورد آسیب |
| `Assets/_Game/Presentation/BattleLighting.cs` | سایهٔ تماس و نور انفجار |
| `Assets/_Game/Art/Battlefield` | تصاویر محیط و قطعات تانک |
| `Assets/_Game/Art/Terrain/Themes` | تعریف محیط‌ها و بافت خاک و سطح |
| `Assets/_Game/Core/Editor` | ابزار نصب و تست‌های ادیتور |

## تست‌های آخرین تغییرات

- [تست موشک واقعی و نور](Docs/Verification/Lighting_RealImpact.txt): پنج بررسی موفق؛ سایهٔ دو تانک، آسیب واقعی موشک، یک نور برای یک انفجار، توقف در Pause و پاک‌شدن افکت.
- [تست هندسه با بافت تازه](Docs/Verification/Lighting_TerrainGeometry.txt): ارتفاع متفاوت ROCK، محل امن شروع، بیش از پنجاه بررسی برخورد، گودال و بازیابی زمین. موفق.
- [تصویر ثبت‌شدهٔ برخورد](Docs/Verification/LightingImpact.png).

آزمون نور از Edit Mode با `Ctrl+Alt+Shift+F2` و ذخیرهٔ آزمایشی جداگانه اجرا می‌شود. گزارش در `Library/LightingVerification.txt` و تصویر در `Library/LightingImpact.png` نوشته می‌شوند. تست هندسهٔ ROCK با `Ctrl+Alt+F3` نقشه را تعویض و زمین را بازنشانی می‌کند؛ در مسابقهٔ مهم اجرا نشود.

گزارش مراحل پیشین در `Docs/Verification` است: `ReferenceArt_Movement.txt` برای حرکت، `SnowEnvironment_Verification.txt` برای محیط برفی و `PresentationPolish_*` برای بررسی‌های آن مرحله. گزارش‌های قبلی به معنی اجرای دوبارهٔ کل آزمون‌های قدیمی پس از آخرین تغییر نیستند. بررسی‌های جدید در Unity Editor ویندوز انجام شده‌اند؛ تست گوشی انجام نشده است.

## بازسازی منابع

منوهای نصب در `Tools > Tank Game` قرار دارند: `Snow > Install Alpine Environment`، `Art > Install Reference Battlefield` و `Art > Install Rugged Canyon`. فقط در Edit Mode اجرا شوند. منابع ساخته‌شده در مخزن هستند و اجرای عادی به بازسازی نیاز ندارد.

ابزار Reference Battlefield بافت ROCK را بازنویسی می‌کند؛ پس از آن Rugged Canyon را اجرا کنید. ابزارهای قدیمی ساخت آرت نیز ممکن است منابع جدید را بازنویسی کنند.

## منابع و محدودیت‌ها

تصاویر با ImageGen ساخته شده‌اند. مشخصات و پرامپت‌ها در `Assets/_Game/Art/Battlefield/PROVENANCE.md`، `Assets/_Game/Art/Terrain/Snow/PROVENANCE.md` و [شرح درهٔ صخره‌ای](Docs/Design/Rock_Canyon.fa.md) هستند. منابع قبلی Kenney همراه مجوز CC0 در `Assets/ThirdParty/KenneyTanks` و فونت Barlow همراه مجوز OFL در Resources نگه داشته شده‌اند.

نور و سایه نمایشی و دوبعدی‌اند. کوه‌ها، ساختمان‌ها و درخت‌های دوردست تصویر تزئینی‌اند و تخریب مستقل ندارند. تعویض ظاهر پس از گودال، حفره‌ها را پر نمی‌کند؛ هندسهٔ تازه روی زمین تخریب‌نشده اعمال می‌شود.

بازی محلی با حریف AI است؛ شبکه، حساب آنلاین و بک‌اند در این تغییرات اضافه نشده‌اند. Library، Logs، خروجی Build و ذخیرهٔ شخصی بازیکن ارسال نمی‌شوند.

شرح تکمیلی: [نور و برخورد](Docs/Design/Lighting_And_Impact.fa.md)، [محیط مرجع](Docs/Design/Reference_Battlefield.fa.md)، [محیط برفی](Docs/Design/Snow_Environment_Update.fa.md) و [سند تحویل اصلی](PROJECT_HANDOFF.fa.md). برای ظاهر و منابع جدید، این سند بر توضیحات قدیمی سند تحویل مقدم است.
