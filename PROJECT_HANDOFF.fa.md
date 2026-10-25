# راهنمای کامل تحویل پروژهٔ Tank Battle

این سند برای توسعه‌دهنده‌ای نوشته شده که پروژه را از GitHub دریافت می‌کند و می‌خواهد ساختار صحنه، آبجکت‌ها، قوانین بازی، دارایی‌ها و محل اتصال بک‌اند را بشناسد. مبنا، کد و صحنهٔ ذخیره‌شده در مخزن در تاریخ ۲۵ اکتبر ۲۰۲۶، پس از افزودن گدازه و کاهش تخریب زمین است. بخش فهرست آبجکت‌ها و مقادیر دارایی‌ها مستقیماً از فایل‌های پروژه استخراج شده است. جزئیات کامل این تغییر تازه در انتهای همین فایل نیز آمده است.

## ۱. دریافت و اجرای پروژه

مخزن: https://github.com/erissmarket/tanks

Git و Git LFS باید نصب باشند؛ تصاویر و برخی دارایی‌های دودویی این مخزن با LFS نگهداری می‌شوند. برای دریافت کامل دارایی‌ها:

```sh
git lfs install
git clone https://github.com/erissmarket/tanks.git
cd tanks
git lfs pull
```

در Unity Hub پوشهٔ مخزن را با Unity **6000.5.10f1** باز کنید. وابستگی‌ها در `Packages/manifest.json` و `Packages/packages-lock.json` ثبت شده‌اند. پروژه دوبعدی و مبتنی بر URP، Unity Input System و Unity UI است. اولین بازکردن به دریافت بسته‌ها و واردکردن دارایی‌ها نیاز دارد.

صحنهٔ اصلی بازی `Assets/_Game/Scenes/MVPIntegrated.unity` است. همین صحنه تنها صحنهٔ فعال در Build Settings است. Play را بزنید؛ ابتدا منوی اصلی نمایش داده می‌شود. از `PLAY` به انتخاب محیط، سلاح شروع و تجهیزات بروید و `START BATTLE` را بزنید. بازیکن صفر تانک قابل‌کنترل و بازیکن یک حریف کامپیوتری است.

صحنهٔ `BackendTest.unity` و نمونه‌های Mock برای تست و توسعهٔ قدیمی‌اند؛ نقطهٔ شروع نسخهٔ فعلی، `MVPIntegrated` است. وجود کلمهٔ `BackendTest` در نام برخی اسکریپت‌های صحنه به معنی وجود سرور آماده نیست؛ Bootstrap فعلی هنوز مونتاژکنندهٔ مسابقهٔ محلی است.

### کنترل‌ها

| ورودی | عملکرد |
| --- | --- |
| A / D | حرکت چپ و راست در بودجهٔ همان نوبت |
| W / S | بالا و پایین بردن لوله |
| Space | شلیک یک تیر در نوبت فعال |
| ۱ تا ۷ / کلیدهای عددی | انتخاب یکی از هفت سلاح |
| دکمه‌های روی صفحه | حرکت، نشانه‌گیری، شلیک، انتخاب سلاح و توقف |

ورودی هنگام بازبودن منو، توقف، پرواز گلوله و نوبت حریف مسدود می‌شود. رهاشدن ورودی و جابه‌جایی صفحه نیز کنترل نگه‌داشته‌شده را پاک می‌کند. لوله در جهت روبه‌روی تانک در بازهٔ محلی صفر تا ۹۰ درجه محدود می‌شود؛ جهت جهانی با چرخش و مقیاس تانک تبدیل می‌شود.

## ۲. نقشهٔ پوشه‌ها

| مسیر | محتوا |
| --- | --- |
| `Assets/_Game/Core` | حالت مسابقه، نوبت، جان، قوانین، فرمان‌ها و پیشرفت |
| `Assets/_Game/Core/Editor` | نصب و آزمون فازهای اول تا هفتم و ساخت خروجی |
| `Assets/_Game/Data` | ScriptableObject سلاح، تانک، سپر و اقتصاد |
| `Assets/_Game/Tank` | حرکت روی شیب، نشانه‌گیری، مشخصات و تجهیزات |
| `Assets/_Game/Weapons` | انتخاب سلاح، مهمات و ایجاد پرتابه |
| `Assets/_Game/Projectiles` | عمر و گروه شلیک، انفجار، جهش، خوشه و حفاری |
| `Assets/_Game/Terrain` | سطح تخریب‌پذیر، ظاهر محیط و ابزارهای ویرایشگر |
| `Assets/_Game/Presentation` | منوها، HUD، ورودی، صدا و افکت نبرد |
| `Assets/_Game/AI` | حریف سادهٔ توپخانه‌ای |
| `Assets/_Game/Testing` | مونتاژ صحنه، کنترل تست و آزمون تحویل |
| `Assets/_Game/Art` | تصویر تانک، زمین، پس‌زمینه، رابط، سلاح و ابزارهای تولید اولیه |
| `Assets/_Game/Prefabs` | نمونه‌های پرتابه و Mock |
| `Assets/_Game/Scenes` | صحنهٔ اصلی و صحنهٔ تست قدیمی |
| `Assets/Settings` | تنظیمات رندر URP و حجم‌های تصویری |
| `Docs/Design` | شرح تغییرات و قواعد هفت فاز |
| `Docs/Verification` | گزارش‌های ثبت‌شدهٔ آزمون و ساخت |

فایل‌های `.meta` باید همراه دارایی‌ها در Git باقی بمانند؛ اتصال Inspector با GUID و fileID انجام می‌شود. تغییر نام یا انتقال دارایی را داخل Unity انجام دهید تا ارجاع‌ها حفظ شوند. `Library`، `Temp`، `Logs`، `Builds` و دادهٔ پیشرفت بازیکن جزو کد قابل‌تحویل نیستند.

## ۳. جریان اجرای بازی و مسئولیت سیستم‌ها

```text
GameInterface: Home → Preparation / Garage → Battle → Result → Home
BackendTestBootstrap: اتصال مدیرها + ثبت بازیکن‌های 0 و 1 + اعمال تجهیزات
IntegratedInput / SimpleArtilleryAI → GameplayActionController
GameplayActionController → TankMotor / TankAim / WeaponController
WeaponController → Projectile + ShotContext
Projectile / ImpactHandler → ExplosionDamage → HealthComponent + DestructibleTerrain
MatchFlowController → Resolving → نوبت بعدی یا MatchEnded
MatchEnded → ProgressionController → ProgressStore → به‌روزرسانی رابط
رویدادهای شلیک و آسیب → BattleFeedback / FeedbackAudio
```

| سیستم | مسئولیت و نکتهٔ اتصال |
| --- | --- |
| `GameManager` | نگهداری `MatchState` و انتشار تغییر حالت |
| `TurnManager` | بازیکن فعال و جابه‌جایی نوبت |
| `MatchFlowController` | زمان نوبت، بودجهٔ حرکت، گروه پرتابه، فرونشستن فیزیک، نتیجه و امتیاز |
| `GameplayActionController` | درگاه مجاز فرمان‌های حرکت، توقف، هدف‌گیری و شلیک؛ اعتبارسنجی نوبت و مالکیت |
| `BackendTestBootstrap` | اتصال ارجاع‌های Inspector، ثبت کنترلرها، بازنشانی زمین/تانک/مهمات و شروع مسابقه |
| `TankMotor` | فیزیک تماس و شیب، حرکت محدود به میدان و بودجه |
| `TankAim` | محدودسازی زاویهٔ محلی و تبدیل جهت جهانی |
| `TankLoadout` / `TankStats` | اعمال کلاس، سپر، ارتقا و ظاهر روی همان تانک صحنه |
| `HealthComponent` | جان، زره، جذب سپر و رویدادهای آسیب و مرگ |
| `WeaponController` | سلاح انتخابی، تعداد تیر هر سلاح، ایجاد و مقداردهی پرتابه |
| `ShotContext` | مالک و شناسهٔ شلیک، شمار اعضا و لغو کل شلیک همراه ریزگلوله‌ها |
| `GameInterface` | ساخت صفحات، گردش بین منو/نبرد/نتیجه و توقف |
| `BattleInterface` | نمایش جان، سپر، زمان، حرکت، مهمات و کنترل‌های نبرد |
| `GarageHud` | انتخاب کلاس و سپر، خرید و ارتقا با همان فروشگاه پیشرفت |
| `BattleFeedback` | رد تیر، انفجار، حلقهٔ سپر، عدد آسیب، لرزش دوربین و پاک‌سازی افکت |
| `ProgressionController` | وصل‌کردن نتیجهٔ مسابقه به ذخیره و امکان تلاش دوباره برای ثبت نتیجه |
| `ProgressStore` | تراکنش محلی خرید، ارتقا، تنظیمات و پاداش با ذخیرهٔ فایل |

اسکریپت‌های `PhaseOneHud`، `PhaseTwoHud` و `IntegratedHud` سابقهٔ رابط نمونه هستند؛ رابط اصلی فعلی با `GameInterface` و `BattleInterface` ساخته می‌شود. نام یا فعال‌بودن هر کامپوننت را در فهرست صحنهٔ پایین بررسی کنید؛ نصب‌کننده‌های قدیمی را برای اجرای عادی دوباره اجرا نکنید.

## ۴. قوانین مسابقه، امتیاز و آسیب

حالت‌ها مسیر `None → Starting → TurnActive → ProjectileFlying → Resolving` را طی می‌کنند؛ پس از حل شلیک نوبت بعدی فعال می‌شود یا حالت `MatchEnded` رخ می‌دهد. هر نوبت یک شلیک اصلی می‌پذیرد؛ چهار ریزگلولهٔ خوشه‌ای عضو همان `ShotContext` هستند. شمار فعال پرتابه‌ها، زمان حداکثری شلیک و مرحلهٔ فرونشستن مانع قفل‌شدن نوبت می‌شوند.

جان صفر، سقوط زیر حد میدان، سقف نوبت‌ها و تسلیم پایان مسابقه هستند. در سقف نوبت‌ها، نسبت جان باقی‌مانده به جان حداکثری مقایسه می‌شود؛ اختلاف کمتر از ۰٫۰۰۱ مساوی است. نابودی هم‌زمان دو تانک نیز مساوی است. تعویض پنجره یا توقف برنامه مسابقه را متوقف می‌کند.

امتیاز از آسیب واقعی به جان دشمن در شلیک پذیرفته‌شده محاسبه می‌شود؛ جذب سپر و آسیب به خود تانک امتیاز دشمن نمی‌دهند. برنده علاوه بر آسیب، امتیاز برد و امتیاز بقا متناسب با نسبت جان باقی‌مانده می‌گیرد. مساوی امتیاز جداگانه دارد. لغو و بازیکن تسلیم‌شده امتیاز صفر دارند. امتیاز نهایی پیش از انتشار نتیجه ثابت می‌شود.

`ExplosionDamage` نزدیک‌ترین نقطهٔ برخوردگر هر هدف را برای افت شعاعی محاسبه می‌کند و هر هدف را حتی با چند برخوردگر فقط یک‌بار آسیب می‌زند. سهم نفوذ سپر مستقیم وارد مسیر جان می‌شود؛ بخش دیگر اول از سپر کم می‌شود و باقیمانده پس از کاهش زره به جان می‌رسد. تماس اولیهٔ پرتابه با بدنهٔ شلیک‌کننده نادیده گرفته می‌شود، اما آسیب شعاعی انفجار نزدیک به خود تانک فعال است.

### مقادیر فعلی قوانین

#### DefaultMatchRules

| فیلد | مقدار |
| --- | --- |
| turnSeconds | 30 |
| movementPerTurn | 3 |
| maximumTurns | 40 |
| shotTimeoutSeconds | 15 |
| minimumSettleSeconds | 0.3 |
| maximumSettleSeconds | 1.5 |
| minimumX | -8 |
| maximumX | 8 |
| killY | -8 |
| winScore | 100 |
| drawScore | 25 |
| survivalScore | 50 |

#### DefaultEconomyRules

| فیلد | مقدار |
| --- | --- |
| winCoins | 80 |
| drawCoins | 35 |
| lossCoins | 15 |
| performanceCap | 30 |
| coinsPerScore | 0.2 |
| maximumUpgradeLevel | 3 |
| hullBaseCost | 60 |
| armorBaseCost | 80 |
| engineBaseCost | 60 |
| hullPerLevel | 0.05 |
| armorPerLevel | 0.02 |
| enginePerLevel | 0.05 |

## ۵. تانک‌ها، سپرها و ارتقا

مشخصات پایه از دارایی‌ها خوانده می‌شوند؛ تغییر عدد در این جدول کد را تغییر نمی‌دهد. مرجع نهایی، فایل `.asset` در مخزن است. Scout سپر سنگین را نمی‌پذیرد چون ظرفیت قابل‌پشتیبانی آن ۳۰ است؛ Vanguard و Bastion هر سه حالت سپر را می‌پذیرند. بازیکن با Vanguard و سپر سبک/بدون سپر شروع می‌کند. حریف از تعریف ذخیره‌شدهٔ `opponentTank` در Bootstrap استفاده می‌کند.

| شناسه | جان | کاهش زره | سرعت | ضریب حرکت | ظرفیت پشتیبانی سپر | هزینه | برد لازم |
| --- | --- | --- | --- | --- | --- | --- | --- |
| bastion | 140 | 0.2 | 2.2 | 0.73333335 | 60 | 250 | 3 |
| scout | 80 | 0 | 3.8 | 1.4 | 30 | 100 | 1 |
| vanguard | 100 | 0.1 | 3 | 1 | 60 | 0 | 0 |

| شناسهٔ سپر | ظرفیت | ضریب حرکت | هزینه | برد لازم |
| --- | --- | --- | --- | --- |
| heavy | 60 | 0.75 | 100 | 2 |
| light | 30 | 1 | 0 | 0 |
| none | 0 | 1 | 0 | 0 |

بودجهٔ جابه‌جایی هر نوبت برابر `movementPerTurn × TankStats.MovementMultiplier × ShieldMovementMultiplier` است. ارتقای بدنه در هر سطح ۵٪ جان، ارتقای موتور ۵٪ سرعت و بودجهٔ حرکت و ارتقای زره ۰٫۰۲ به ضریب کاهش آسیب اضافه می‌کند. سقف فعلی سه سطح است؛ کاهش زره در نهایت حداکثر ۰٫۶ است. هزینهٔ هر سطح از `ProgressStore.UpgradeCost` و `DefaultEconomyRules` خوانده می‌شود. خرید و اعمال انتخاب فقط پس از موفقیت نوشتن فایل قطعی می‌شوند.

## ۶. سلاح‌ها و پرتابه‌ها

| شناسه | آسیب پایه | سرعت پرتاب | شعاع | مهمات | حداقل سهم آسیب لبه | نفوذ سپر |
| --- | --- | --- | --- | --- | --- | --- |
| weapon-bouncy | 34 | 12 | 1.4 | 3 | 0.35 | 0 |
| weapon-cluster | 0 | 11 | 1.2 | 2 | 0.35 | 0 |
| weapon-digger | 42 | 13 | 1.8 | 2 | 0.35 | 0 |
| weapon-floater | 40 | 8 | 2 | 2 | 0.35 | 0 |
| weapon-heavy-mortar | 48 | 11 | 2.4 | 3 | 0.35 | 0 |
| weapon-railgun | 38 | 28 | 0.65 | 3 | 0.35 | 0.5 |
| weapon-rocket | 32 | 10 | 1.5 | -1 | 0.35 | 0 |

مهمات `-1` یعنی نامحدود. مهمات ویژه در هر مسابقه بازنشانی می‌شود. موشک و خمپاره مسیر بالستیک دارند؛ ریل‌گان سریع و دارای نفوذ سپر است؛ شناور حرکت کندتری دارد؛ جهشی از `BouncyImpactHandler` استفاده می‌کند؛ خوشه‌ای با `ClusterImpactHandler` ریزگلوله‌های همان شلیک را می‌سازد؛ حفار با `DiggerImpactHandler` چند گام در خاک پیش می‌رود و سپس منفجر می‌شود. تعداد جهش، جزئیات پخش خوشه و گام‌های حفاری در Prefab تنظیم می‌شوند؛ فهرست کامل آن‌ها در پیوست آمده است.

### افزودن سلاح جدید

۱. یک `WeaponData` با شناسهٔ یکتا، تصویر و Prefab معتبر ایجاد کنید.
۲. Prefab باید `Projectile`، `Rigidbody2D` و Collider مناسب داشته باشد؛ رفتار خاص در `IProjectileImpactHandler` قرار می‌گیرد.
۳. دارایی را به `IntegratedInput.availableWeapons` در صحنه اضافه کنید. رابط انتخاب از این آرایه ساخته می‌شود.
۴. برای ظاهر و صدا، شناسه را در `FeedbackStyle` و در صورت نیاز `WeaponPresentation` تعریف کنید.
۵. ثبت ریزگلوله‌ها در `ShotContext`، تمام‌شدن شلیک، پاک‌سازی و مهمات را بررسی کنید. آزمون ترکیب‌های فاز هفتم فعلاً تعداد مورد انتظار ۱۶۸ را ثابت فرض می‌کند؛ با تغییر تعداد سلاح/کلاس/سپر، آن انتظار هم باید به‌روزرسانی شود.

## ۷. زمین و فیزیک

`DestructibleTerrain` سطح قابل‌تغییر، Sprite/بافت زمان اجرا و برخوردگر زمین را مدیریت می‌کند. انفجار از `CarveExplosion(center, blastRadius)` استفاده می‌کند؛ این متد شعاع گودی را به ۳۶٪ شعاع آسیب، حداکثر ۰٫۸۵ واحد و عمق مرکزی در برخورد سطحی را به حدود ۵۵٪ شعاع گودی محدود می‌کند. متد پایهٔ `Carve(center, radius)` برای تغییر مستقیم هندسه و حفاری باقی است. `SurfaceHeightAt(x)` ارتفاع سطح را می‌دهد و `ResetSurface()` زمین اولیه را برای مسابقهٔ بعد برمی‌گرداند. موتور تانک با سطح و شیب هماهنگ می‌شود. مدل سطح از نوع ارتفاع بر حسب x است؛ هندسهٔ تونل دلخواه سه‌بعدی یا غار مستقل فرض نشود.

آبجکت `Underground Lava` با کامپوننت `LavaHazard` در ارتفاع جهانی `-3.85` قرار دارد. جریان، موج و ذرات روشن با یک Mesh متحرک ۸۶۴ رأسی نمایش داده می‌شوند. ارجاع `MatchFlowController.lava` به همین آبجکت وصل است؛ تماس تانک با سطح مذاب شکست از نوع `Fall` همراه پاشش و فرو رفتن بدنه ایجاد می‌کند. توقف بازی و بازیابی تانک در مسابقهٔ بعد نیز بررسی شده‌اند.

سه محیط چمن، برف و سنگ با `TerrainThemeDefinition` و `TerrainThemeController` مدیریت می‌شوند. انتخاب ظاهر به‌تنهایی نباید منطق نبرد را عوض کند؛ شروع مسابقهٔ تازه سطح را بازنشانی می‌کند. تغییر تم در همان زمین، ظاهر را عوض می‌کند. دارایی‌های واقعی محیط در فهرست دارایی‌های پیوست آمده‌اند.

## ۸. رابط، افکت و صدا

صفحات `Home`، `Preparation`، `Garage`، `Settings`، `Help`، `Battle`، `Pause`، `ConfirmLeave`، `Result` و `Rotate` وجود دارند. بیشتر فرزندان رابط هنگام اجرا با کد ساخته می‌شوند؛ به همین دلیل همهٔ دکمه‌ها در Hierarchy حالت ویرایش دیده نمی‌شوند. `FitInterfacePanel` و `HudSafeArea` به اندازه و محدودهٔ امن صفحه کمک می‌کنند.

`BattleFeedback` رویدادهای واقعی پرتاب، انفجار و آسیب را مصرف می‌کند. رد هفت سلاح رنگ متفاوت و حاشیهٔ تیره دارد. سپر با اندازهٔ بدنه هماهنگ می‌شود، جذب و شکستن آن بازخورد جدا دارد و عدد جان کم‌شده از جذب سپر تفکیک می‌شود. سقف افکت‌های فعال ۶۴ و سقف نوشته‌های آسیب ۶ است. انفجار لغوشده نباید انفجار نمایشی اضافه ایجاد کند.

`FeedbackAudio` صداهای کوتاه را به‌صورت تولیدی و ذخیره‌شده در حافظه فراهم می‌کند. صدای رابط هنگام توقف نیز کار می‌کند. تنظیم صدا در پیشرفت محلی ذخیره می‌شود. متریال `Presentation/Assets/BattleEffects.mat` در صحنه ارجاع صریح دارد تا در خروجی حذف نشود. برای افزودن جلوه از رویدادهای موجود استفاده کنید تا منطق آسیب دوبار اجرا نشود.

## ۹. پیشرفت، اقتصاد و ذخیره

مسیر ذخیره `Application.persistentDataPath/progress-v1.json` است. در ویندوز با نام‌های فعلی شرکت/محصول، معمولاً زیر `%USERPROFILE%/AppData/LocalLow/DefaultCompany/tank` قرار می‌گیرد. تغییر company/product مسیر ذخیره را تغییر می‌دهد. فایل اصلی در مخزن قرار ندارد.

`PlayerProgress` شامل نسخه، سکه، برد/مساوی/باخت، امتیاز کل/بهترین، شناسهٔ تانک و سپر انتخابی، سلاح شروع، تم انتخابی، وضعیت صدا، `pendingMatchId`، مالکیت تجهیزات و سطح ارتقای هر تانک است. این مدل احراز هویت یا حساب شبکه‌ای نیست.

`ProgressStore` داده را در envelope با payload و SHA-256 ذخیره می‌کند. نوشتن ابتدا در `.tmp` انجام می‌شود، سپس فایل اصلی همراه `.bak` جایگزین می‌شود. در خرابی، نسخهٔ پشتیبان امتحان می‌شود؛ نسخهٔ پشتیبانی‌نشده یا خرابی غیرقابل‌بازیابی می‌تواند فروشگاه را فقط‌خواندنی کند و فایل‌های موجود حفظ می‌شوند. checksum مکانیزم امضای سرور یا ضدتقلب نیست.

`BeginMatch` یک شناسهٔ معلق جدید ذخیره می‌کند. `CompleteMatch` فقط شناسهٔ معلق فعلی را قبول می‌کند و آن را در همان ثبت پاداش پاک می‌کند؛ ثبت دوباره پاداش نمی‌سازد. `ProgressionController.RetrySettlement()` ثبت ناموفق نتیجه را دوباره امتحان می‌کند. نتیجهٔ لغو/تسلیم پاداش اقتصادی ندارد. پاداش طبیعی برابر پایهٔ برد/مساوی/باخت به‌علاوهٔ حداقلِ سقف عملکرد و `floor(score × coinsPerScore)` است.

## ۱۰. راهنمای اتصال بک‌اند و شبکه

**وضعیت فعلی:** بازی محلی با حریف AI است؛ سرویس حساب، دیتابیس، API، WebSocket، matchmaking و همگام‌سازی دو دستگاه در این مخزن پیاده نشده‌اند. جدول زیر نقاط اتصال پیشنهادی است، نه API شبکه‌ای موجود.

| نیاز بک‌اند | نقطهٔ مناسب در کد فعلی | پیشنهاد ادامهٔ کار |
| --- | --- | --- |
| هویت و مالکیت تجهیزات | `PlayerProgress` و `ProgressStore` | تعریف DTO و شناسهٔ حساب سرور؛ نگه‌داشتن IDهای فعلی تجهیزات |
| اعتبارسنجی خرید و ارتقا | `Unlock`، `Upgrade`، `CanUnlock` | محاسبه و ثبت اتمیک روی سرور و بازگرداندن snapshot معتبر |
| ساخت مسابقه | `BackendTestBootstrap` و `MatchFlowController.Initialize` | جایگزینی مونتاژ محلی با جلسهٔ دارای ID و ثبت کنترلرهای محلی/راه‌دور |
| ارسال فرمان | `GameplayActionController.Move/Stop/Aim/Fire` | پیام شامل matchId، playerId، turnId/sequence و پارامتر؛ بررسی نوبت در مرجع معتبر |
| نتیجه و پاداش | `MatchFlowController.MatchEnded` و `ProgressionController` | نتیجهٔ معتبر سمت سرور و کلید idempotency برای جلوگیری از ثبت تکراری |
| وضعیت نبرد | `MatchState`، زمان، بودجه، جان/سپر و `ShotContext` | طراحی snapshot/event و ترتیب/نسخهٔ پیام؛ زمان شبکه را از UI جدا نگه دارید |
| زمین تخریب‌شده | `DestructibleTerrain.Carve` و بازنشانی | تعیین مرجع تغییر زمین و ارسال تغییرات مرتب یا snapshot سطح |
| نمایش بازیکن راه‌دور | `IPlayerController` و `PlayerControllerBase` | کنترلر راه‌دور با همان قرارداد مجازبودن اعمال |

فیزیک Unity این نسخه به‌عنوان شبیه‌سازی قطعی بین دستگاه‌ها تأیید نشده است. پیش از انتخاب lockstep باید این موضوع آزمایش شود؛ یک مرجع معتبر برای نتیجه، آسیب و زمین تعریف کنید. کیف پول محلی و checksum فایل را منبع معتبر اقتصاد آنلاین نگیرید. در این مرحله endpoint، کلید API یا قرارداد پیام آماده‌ای وجود ندارد؛ این موارد باید همراه توسعه‌دهندهٔ بک‌اند تعیین شوند.

## ۱۱. آزمون‌ها، ساخت و محدودیت‌های تحویل

گزارش‌های فازهای اول تا ششم به‌ترتیب ۱۹، ۱۶، ۱۳، ۲۲، ۲۰ و ۱۸ بررسی موفق دارند: جمعاً ۱۰۸ بررسی. فاز هفتم ۱۷۶ بررسی موفق، شامل ۱۶۸ ترکیب قانونی کلاس/سپر/محیط/سلاح دارد. یک مسابقهٔ واقعی پس از ۱۰ شلیک با حذف حریف پایان یافت؛ ثبت یک‌بارهٔ پاداش و پاک‌سازی تأیید شد. نسخهٔ مستقل ویندوز نیز هفت بررسی تکمیلی را با موفقیت گذراند.

منوی `Tools > Tank Game` گزینه‌های هر فاز را دارد. آزمون فازهای پنجم، ششم و هفتم را از Edit Mode شروع کنید؛ برای تکرار مستقل آزمون‌ها بین آن‌ها از Play Mode خارج شوید. گزارش‌های تازه داخل `Library` تولید می‌شوند و نسخهٔ ثبت‌شدهٔ زمان تحویل در `Docs/Verification` قرار دارد.

برای ساخت: صحنهٔ اصلی را باز کنید، از منوی Phase Seven گزینهٔ `Prepare Delivery Scene` و سپس `Build Windows Test Version` را بزنید. خروجی `Builds/Windows/TankBattle.exe` است. کل پوشهٔ Windows باید تحویل داده شود؛ EXE به‌تنهایی کافی نیست. ساخت ثبت‌شدهٔ ۲۴ اکتبر با صفر خطا، اندازهٔ گزارش‌شدهٔ ۱۵۲٬۸۴۵٬۲۹۴ بایت و زمان حدود پنج دقیقه و ۲۵ ثانیه انجام شد. Unity تعداد ۴۸۶ هشدار ثبت کرد؛ هشدارهای شیدر و متغیر قدیمی استفاده‌نشده در کنسول مشاهده شدند. این خروجی آزمایشی پیش از تغییر گدازه ساخته شده است؛ برای فایل اجرایی حاوی تغییرات ۲۵ اکتبر، دوباره خروجی بسازید. گدازه و تنظیم جدید زمین در Unity بررسی شده‌اند.

آزمون خودکار نسخهٔ مستقل با `TankBattle.exe --tank-smoke D:/TankQualification` انجام می‌شود. پنجرهٔ برنامه را به‌صورت عادی باز کنید؛ شروعِ کاملاً مخفی در بررسی حاضر آزمون را آغاز نکرد. این حالت از پروفایل جدا استفاده می‌کند و در پایان بسته می‌شود. کد خروج صفر موفقیت و یک شکست است. اجرای عادی بدون آرگومان وارد منوی بازی می‌شود.

**اندروید هنوز تأیید نشده:** ماژول Android Build Support روی نصب آزمایش موجود نبود؛ APK و تست گوشی ساخته/انجام نشدند. پس از نصب ماژول مناسب همراه SDK/NDK/JDK، لمس چندگانه، نسبت صفحه و safe area، توقف/ادامه، ذخیره بعد از بازگشایی، صدا، نرخ فریم، حافظه و داغ‌شدن روی گوشی واقعی بررسی شوند. اعداد حافظهٔ داخل Editor جای آزمون گوشی را نمی‌گیرند.

فایل‌های اجرایی به‌علت حجم و ماهیت خروجی ساخت در Git commit نمی‌شوند. سورس و راهنما و گزارش‌های بررسی در مخزن هستند؛ بستهٔ اجرایی ویندوز جداگانه تحویل داده می‌شود.

## ۱۲. شرح هفت فاز و مستندات موجود

- [Phase01_Game_Rules.fa.md](Docs/Design/Phase01_Game_Rules.fa.md)
- [Phase02_Weapons_Shields.fa.md](Docs/Design/Phase02_Weapons_Shields.fa.md)
- [Phase03_Tanks_Garage.fa.md](Docs/Design/Phase03_Tanks_Garage.fa.md)
- [Phase04_Progression.fa.md](Docs/Design/Phase04_Progression.fa.md)
- [Phase05_Interface.fa.md](Docs/Design/Phase05_Interface.fa.md)
- [Phase06_Feedback.fa.md](Docs/Design/Phase06_Feedback.fa.md)
- [Phase07_Delivery.fa.md](Docs/Design/Phase07_Delivery.fa.md)
- [Volcanic_Update.fa.md](Docs/Design/Volcanic_Update.fa.md)

راهنماهای قدیمی سلاح نیز در ریشه قرار دارند: `PROJECTILES_SYSTEM_GUIDE.md` و `WEAPONS_VISUAL_ASSETS_GUIDE.md`. در تعارض با نسخه‌های قدیمی، کد و دارایی فعلی و گزارش این تحویل مبنا هستند.

## پیوست الف: فهرست آبجکت‌های ذخیره‌شدهٔ صحنهٔ اصلی

جدول زیر تمام GameObjectهای دارای نام و ذخیره‌شده در فایل صحنه را همراه مسیر والد و کامپوننت‌ها ثبت می‌کند. عدد fileID برای پیدا کردن ارجاع در YAML است، شناسهٔ شبکه‌ای نیست. `active=0` یعنی خود آبجکت غیرفعال است؛ وضعیت نهایی فرزند به والد هم وابسته است. `disabled` کنار کامپوننت یعنی همان کامپوننت خاموش ذخیره شده است. فرزندان ساخته‌شده در زمان اجرا در پیوست جدا توضیح داده می‌شوند. نمونه‌های Prefab اگر به‌صورت مرجع ذخیره شده باشند، در بخش Prefab نیز باید بررسی شوند.

| مسیر آبجکت | fileID | active / layer | کامپوننت‌ها |
| --- | --- | --- | --- |
| Battle Feedback | 2105976711 | 1 / 0 | Transform, BattleFeedback |
| Battlefield | 436426176 | 1 / 0 | Transform |
| Battlefield/Ground | 1493427343 | 0 / 0 | Transform, SpriteRenderer, BoxCollider2D, Rigidbody2D |
| EventSystem | 820264937 | 1 / 0 | Transform, EventSystem, InputSystemUIInputModule |
| Global Light 2D | 447045989 | 1 / 0 | Transform, Package MonoBehaviour |
| MVP Background | 468772676 | 1 / 0 | Transform |
| MVP Background/Clouds | 887116292 | 1 / 0 | Transform, SpriteRenderer |
| MVP Background/Mountains Far | 1001347397 | 1 / 0 | Transform, SpriteRenderer |
| MVP Background/Mountains Near | 1727540485 | 1 / 0 | Transform, SpriteRenderer |
| MVP Background/Sky | 662258611 | 1 / 0 | Transform, SpriteRenderer |
| MVP Battlefield | 289363242 | 1 / 0 | Transform, TerrainThemeController |
| MVP Battlefield/Grass Surface | 906547725 | 1 / 0 | Transform, SpriteRenderer |
| MVP Battlefield/Soil Ground | 2007810296 | 1 / 0 | Transform, SpriteRenderer, PolygonCollider2D, DestructibleTerrain |
| MVP HUD | 648974017 | 1 / 0 | RectTransform, Canvas, CanvasScaler, GraphicRaycaster, IntegratedHud (disabled) |
| MVP HUD/SafeArea | 121744478 | 1 / 0 | RectTransform, HudSafeArea |
| MVP HUD/SafeArea/AIM DOWN | 2120250773 | 0 / 0 | RectTransform, CanvasRenderer, Image, Button, IntegratedHoldButton |
| MVP HUD/SafeArea/AIM DOWN/Label | 1877607670 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/AIM UP | 483550077 | 0 / 0 | RectTransform, CanvasRenderer, Image, Button, IntegratedHoldButton |
| MVP HUD/SafeArea/AIM UP/Label | 745153566 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/Enemy Health Bar | 328548529 | 0 / 0 | RectTransform, CanvasRenderer, Image |
| MVP HUD/SafeArea/Enemy Health Bar/Fill | 1671649949 | 1 / 0 | RectTransform, CanvasRenderer, Image |
| MVP HUD/SafeArea/Enemy Health Bar/Label | 732649788 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/Fire Button | 1324402178 | 0 / 0 | RectTransform, CanvasRenderer, Image, Button, IntegratedHoldButton |
| MVP HUD/SafeArea/Fire Button/Disc | 1502459571 | 1 / 0 | RectTransform, CanvasRenderer, Image |
| MVP HUD/SafeArea/Fire Button/Label | 254711323 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/Game Interface | 2014528866 | 1 / 0 | RectTransform, GameInterface |
| MVP HUD/SafeArea/Garage HUD | 634985467 | 1 / 0 | RectTransform, GarageHud |
| MVP HUD/SafeArea/LEFT | 752934675 | 0 / 0 | RectTransform, CanvasRenderer, Image, Button, IntegratedHoldButton |
| MVP HUD/SafeArea/LEFT/Label | 598120849 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/Phase One HUD | 1646746948 | 0 / 0 | RectTransform, PhaseOneHud |
| MVP HUD/SafeArea/Phase One HUD/Pause | 231094241 | 1 / 0 | RectTransform, CanvasRenderer, Image, Button |
| MVP HUD/SafeArea/Phase One HUD/Pause Panel | 1618209109 | 0 / 0 | RectTransform, CanvasRenderer, Image |
| MVP HUD/SafeArea/Phase One HUD/Pause Panel/Back | 1977513166 | 1 / 0 | RectTransform, CanvasRenderer, Image, Button |
| MVP HUD/SafeArea/Phase One HUD/Pause Panel/Back/Label | 1617354282 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/Phase One HUD/Pause Panel/Confirm | 499328861 | 1 / 0 | RectTransform, CanvasRenderer, Image, Button |
| MVP HUD/SafeArea/Phase One HUD/Pause Panel/Confirm/Label | 1581271318 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/Phase One HUD/Pause Panel/Resume | 366318604 | 1 / 0 | RectTransform, CanvasRenderer, Image, Button |
| MVP HUD/SafeArea/Phase One HUD/Pause Panel/Resume/Label | 287875259 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/Phase One HUD/Pause Panel/Surrender | 1299994875 | 1 / 0 | RectTransform, CanvasRenderer, Image, Button |
| MVP HUD/SafeArea/Phase One HUD/Pause Panel/Surrender/Label | 1266826561 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/Phase One HUD/Pause Panel/Title | 1332202434 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/Phase One HUD/Pause/Label | 873115800 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/Phase One HUD/Result Panel | 258577702 | 0 / 0 | RectTransform, CanvasRenderer, Image |
| MVP HUD/SafeArea/Phase One HUD/Result Panel/Details | 624296819 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/Phase One HUD/Result Panel/Restart | 2020799051 | 1 / 0 | RectTransform, CanvasRenderer, Image, Button |
| MVP HUD/SafeArea/Phase One HUD/Result Panel/Restart/Label | 1273974461 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/Phase One HUD/Result Panel/Title | 1052946149 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/Phase One HUD/Turn Status | 63484885 | 1 / 0 | RectTransform, CanvasRenderer, Image |
| MVP HUD/SafeArea/Phase One HUD/Turn Status/Status | 2081342528 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/Phase Two HUD | 1768467671 | 0 / 0 | RectTransform, PhaseTwoHud |
| MVP HUD/SafeArea/Player Health Bar | 1055450106 | 0 / 0 | RectTransform, CanvasRenderer, Image |
| MVP HUD/SafeArea/Player Health Bar/Fill | 1206271529 | 1 / 0 | RectTransform, CanvasRenderer, Image |
| MVP HUD/SafeArea/Player Health Bar/Label | 913695586 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/RESTART | 1608383156 | 0 / 0 | RectTransform, CanvasRenderer, Image, Button |
| MVP HUD/SafeArea/RESTART/Label | 515252519 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/RIGHT | 1578179737 | 0 / 0 | RectTransform, CanvasRenderer, Image, Button, IntegratedHoldButton |
| MVP HUD/SafeArea/RIGHT/Label | 597694885 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/Terrain Selector | 1174599359 | 0 / 0 | RectTransform |
| MVP HUD/SafeArea/Terrain Selector/Theme GRASS | 1574381919 | 1 / 0 | RectTransform, CanvasRenderer, Image, Button |
| MVP HUD/SafeArea/Terrain Selector/Theme GRASS/Label | 850727174 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/Terrain Selector/Theme ROCK | 1234490172 | 1 / 0 | RectTransform, CanvasRenderer, Image, Button |
| MVP HUD/SafeArea/Terrain Selector/Theme ROCK/Label | 995205618 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/Terrain Selector/Theme SNOW | 73231950 | 1 / 0 | RectTransform, CanvasRenderer, Image, Button |
| MVP HUD/SafeArea/Terrain Selector/Theme SNOW/Label | 875043107 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/Terrain Selector/Title | 692241095 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| MVP HUD/SafeArea/Turn Indicator | 531061845 | 0 / 0 | RectTransform, CanvasRenderer, Image |
| MVP HUD/SafeArea/Turn Indicator/Label | 1682083976 | 1 / 0 | RectTransform, CanvasRenderer, Text |
| Main Camera | 262994191 | 1 / 0 | Transform, Camera, AudioListener |
| Muzzle | 1750549422 | 1 / 0 | Transform |
| Muzzle | 291084909 | 1 / 0 | Transform |
| Release Qualification | 50770197 | 1 / 0 | Transform, ReleaseQualification |
| Systems | 1091336330 | 1 / 0 | Transform, IntegratedInput |
| Systems/BackendTestBootstrap | 687904037 | 1 / 0 | Transform, BackendTestBootstrap |
| Systems/BackendTestDiagnostics | 161129496 | 1 / 0 | Transform, BackendTestDiagnostics (disabled) |
| Systems/BackendTestInput | 874101255 | 1 / 0 | Transform, BackendTestInput (disabled) |
| Systems/GameManager | 1612854977 | 1 / 0 | Transform, GameManager |
| Systems/GameplayActionController | 690841744 | 1 / 0 | Transform, GameplayActionController |
| Systems/MatchFlowController | 1867302566 | 1 / 0 | Transform, MatchFlowController |
| Systems/Progression | 667380912 | 1 / 0 | Transform, ProgressionController |
| Systems/SimpleArtilleryAI | 732972017 | 1 / 0 | Transform, SimpleArtilleryAI |
| Systems/TurnManager | 1302739949 | 1 / 0 | Transform, TurnManager |
| Underground Lava | 765233045 | 1 / 0 | Transform, LavaHazard |

## پیوست ب: تنظیمات و ارجاع‌های اسکریپت‌های صحنه

مقادیر زیر از Inspector ذخیره‌شده استخراج شده‌اند؛ بعضی مقادیر در Start/ApplyLoadout بازنویسی می‌شوند. ارجاع بیرونی به مسیر دارایی و ارجاع داخلی به fileID همان صحنه اشاره می‌کند.

### Battle Feedback → BattleFeedback

```yaml
  effectMaterial: {fileID: 2100000, guid: b334c3b629b27544397d05b5d1795d31, type: 2} # Assets/_Game/Presentation/Assets/BattleEffects.mat
```

### MVP Battlefield → TerrainThemeController

```yaml
  terrain: {fileID: 2007810300} # DestructibleTerrain
  soilRenderer: {fileID: 2007810299} # SpriteRenderer
  surfaceRenderer: {fileID: 906547727} # SpriteRenderer
  skyRenderer: {fileID: 662258613} # SpriteRenderer
  farMountains: {fileID: 1001347399} # SpriteRenderer
  nearMountains: {fileID: 1727540487} # SpriteRenderer
  themes:
  - {fileID: 11400000, guid: 544da9d0b882228438753ce6869601f5, type: 2} # Assets/_Game/Art/Terrain/Themes/Grassland.asset
  - {fileID: 11400000, guid: 47b3320179c30194a95e160053a587d7, type: 2} # Assets/_Game/Art/Terrain/Themes/Snowfield.asset
  - {fileID: 11400000, guid: f66bcc8e64169c3438aedda4bd6d95df, type: 2} # Assets/_Game/Art/Terrain/Themes/Rocklands.asset
  selectedTheme: 0
  buttons:
  - {fileID: 1574381921} # Button
  - {fileID: 73231952} # Button
  - {fileID: 1234490174} # Button
```

### MVP Battlefield/Soil Ground → DestructibleTerrain

```yaml
  grassRenderer: {fileID: 906547727} # SpriteRenderer
  scorchWidth: 0.16
  blastCraterScale: 0.36
  blastDepthFraction: 0.55
  maximumBlastCraterRadius: 0.85
```

### MVP HUD → IntegratedHud

```yaml
  playerHealth: {fileID: 1487234971} # HealthComponent
  enemyHealth: {fileID: 1823342655} # HealthComponent
  playerFill: {fileID: 1206271532} # Image
  enemyFill: {fileID: 1671649952} # Image
  playerLabel: {fileID: 913695589} # Text
  enemyLabel: {fileID: 732649791} # Text
  turnLabel: {fileID: 1682083979} # Text
  gameManager: {fileID: 1612854978} # GameManager
  turnManager: {fileID: 1302739950} # TurnManager
  matchFlow: {fileID: 1867302567} # MatchFlowController
  input: {fileID: 1091336332} # IntegratedInput
  fire: {fileID: 1324402182} # Button
  restart: {fileID: 1608383157} # Button
```

### MVP HUD/SafeArea/AIM DOWN → IntegratedHoldButton

```yaml
  input: {fileID: 1091336332} # IntegratedInput
  command: 3
```

### MVP HUD/SafeArea/AIM UP → IntegratedHoldButton

```yaml
  input: {fileID: 1091336332} # IntegratedInput
  command: 2
```

### MVP HUD/SafeArea/Fire Button → IntegratedHoldButton

```yaml
  input: {fileID: 1091336332} # IntegratedInput
  command: 4
```

### MVP HUD/SafeArea/Game Interface → GameInterface

```yaml
  bootstrap: {fileID: 687904039} # BackendTestBootstrap
  match: {fileID: 1867302567} # MatchFlowController
  input: {fileID: 1091336332} # IntegratedInput
  garage: {fileID: 634985469} # GarageHud
  themes: {fileID: 289363244} # TerrainThemeController
  playerHealth: {fileID: 1487234971} # HealthComponent
  enemyHealth: {fileID: 1823342655} # HealthComponent
  weapon: {fileID: 1487234972} # WeaponController
```

### MVP HUD/SafeArea/Garage HUD → GarageHud

```yaml
  bootstrap: {fileID: 687904039} # BackendTestBootstrap
  match: {fileID: 1867302567} # MatchFlowController
  externalNavigation: 1
```

### MVP HUD/SafeArea/LEFT → IntegratedHoldButton

```yaml
  input: {fileID: 1091336332} # IntegratedInput
  command: 0
```

### MVP HUD/SafeArea/Phase One HUD → PhaseOneHud

```yaml
  match: {fileID: 1867302567} # MatchFlowController
  bootstrap: {fileID: 687904039} # BackendTestBootstrap
  status: {fileID: 2081342530} # Text
  pausePanel: {fileID: 1618209109} # GameObject
  pauseTitle: {fileID: 1332202436} # Text
  pauseButton: {fileID: 231094243} # Button
  resumeButton: {fileID: 366318606} # Button
  surrenderButton: {fileID: 1299994877} # Button
  confirmButton: {fileID: 499328863} # Button
  backButton: {fileID: 1977513168} # Button
  resultPanel: {fileID: 258577702} # GameObject
  resultTitle: {fileID: 1052946151} # Text
  resultDetails: {fileID: 624296821} # Text
  restartButton: {fileID: 2020799053} # Button
```

### MVP HUD/SafeArea/Phase Two HUD → PhaseTwoHud

```yaml
  input: {fileID: 1091336332} # IntegratedInput
  playerWeapon: {fileID: 1487234972} # WeaponController
  playerHealth: {fileID: 1487234971} # HealthComponent
  enemyHealth: {fileID: 1823342655} # HealthComponent
  match: {fileID: 1867302567} # MatchFlowController
  bootstrap: {fileID: 687904039} # BackendTestBootstrap
```

### MVP HUD/SafeArea/RIGHT → IntegratedHoldButton

```yaml
  input: {fileID: 1091336332} # IntegratedInput
  command: 1
```

### Systems → IntegratedInput

```yaml
  actions: {fileID: 690841745} # GameplayActionController
  gameManager: {fileID: 1612854978} # GameManager
  turns: {fileID: 1302739950} # TurnManager
  player: {fileID: 223520936} # BackendTestPlayerController
  bootstrap: {fileID: 687904039} # BackendTestBootstrap
  aim: {fileID: 1487234969} # TankAim
  barrel: {fileID: 970009292} # Transform
  aimRate: 60
  holdButtons:
  - {fileID: 752934678} # Button
  - {fileID: 1578179740} # Button
  - {fileID: 483550080} # Button
  - {fileID: 2120250776} # Button
  availableWeapons:
  - {fileID: 11400000, guid: cabef70869bcebe438ef627c78fe41c1, type: 2} # Assets/_Game/Data/Weapons/Weapon_Rocket.asset
  - {fileID: 11400000, guid: 92af71366e35324419278ba3227ea985, type: 2} # Assets/_Game/Data/Weapons/Weapon_HeavyMortar.asset
  - {fileID: 11400000, guid: 89029397b93deb24084fa6930fad98b5, type: 2} # Assets/_Game/Data/Weapons/Weapon_Railgun.asset
  - {fileID: 11400000, guid: 7ec79b5db4cd056489eae1a6730faf10, type: 2} # Assets/_Game/Data/Weapons/Weapon_Floater.asset
  - {fileID: 11400000, guid: 36687fda9a76dbd4cb5e0e235927c898, type: 2} # Assets/_Game/Data/Weapons/Weapon_Bouncy.asset
  - {fileID: 11400000, guid: 7bbb12419588b2346954be30379a622a, type: 2} # Assets/_Game/Data/Weapons/Weapon_Cluster.asset
  - {fileID: 11400000, guid: 09fa4789a10d70b44860d5aec0f0b1f0, type: 2} # Assets/_Game/Data/Weapons/Weapon_Digger.asset
  weaponController: {fileID: 0}
```

### Systems/BackendTestBootstrap → BackendTestBootstrap

```yaml
  gameManager: {fileID: 1612854978} # GameManager
  turnManager: {fileID: 1302739950} # TurnManager
  matchFlow: {fileID: 1867302567} # MatchFlowController
  gameplayActions: {fileID: 690841745} # GameplayActionController
  playerZero: {fileID: 223520936} # BackendTestPlayerController
  playerOne: {fileID: 55054705} # BackendTestPlayerController
  playerZeroMuzzle: {fileID: 291084910} # Transform
  playerOneMuzzle: {fileID: 1750549423} # Transform
  playerZeroDefaultAim: 45
  playerOneDefaultAim: 45
  shieldOptions:
  - {fileID: 11400000, guid: 1631256ba207aa6498dd86f6e2e6a834, type: 2} # Assets/_Game/Data/Shield_None.asset
  - {fileID: 11400000, guid: 8b416fa7950da1945b63df0ac56bde8a, type: 2} # Assets/_Game/Data/Shield_Light.asset
  - {fileID: 11400000, guid: 9f2cff698f5a3fe4593c3d6b41484168, type: 2} # Assets/_Game/Data/Shield_Heavy.asset
  nextShieldIndex: 1
  tankOptions:
  - {fileID: 11400000, guid: 83b246fd5d5115f439a1eb54147553f4, type: 2} # Assets/_Game/Data/Tanks/Tank_Scout.asset
  - {fileID: 11400000, guid: 17747e1b27893e14bb8f41d9e7af18a8, type: 2} # Assets/_Game/Data/Tanks/Tank_Vanguard.asset
  - {fileID: 11400000, guid: 2aaf2140d23e60d499ed218bc5f0b7ea, type: 2} # Assets/_Game/Data/Tanks/Tank_Bastion.asset
  nextTankIndex: 1
  opponentTank: {fileID: 11400000, guid: 17747e1b27893e14bb8f41d9e7af18a8, type: 2} # Assets/_Game/Data/Tanks/Tank_Vanguard.asset
  startInGarage: 1
  progression: {fileID: 667380913} # ProgressionController
```

### Systems/BackendTestDiagnostics → BackendTestDiagnostics

```yaml
  gameManager: {fileID: 1612854978} # GameManager
  turnManager: {fileID: 1302739950} # TurnManager
  matchFlow: {fileID: 1867302567} # MatchFlowController
  playerZeroHealth: {fileID: 1487234971} # HealthComponent
  playerOneHealth: {fileID: 1823342655} # HealthComponent
```

### Systems/BackendTestInput → BackendTestInput

```yaml
  gameplayActions: {fileID: 690841745} # GameplayActionController
  turnManager: {fileID: 1302739950} # TurnManager
  bootstrap: {fileID: 687904039} # BackendTestBootstrap
  playerZeroAim: {fileID: 1487234969} # TankAim
  playerOneAim: {fileID: 1823342653} # TankAim
  playerZeroWeapon: {fileID: 0}
  playerOneWeapon: {fileID: 0}
  playerZeroMuzzle: {fileID: 223520935} # Transform
  playerOneMuzzle: {fileID: 55054704} # Transform
  availableWeapons: []
  aimRate: 60
  playerOneIsAI: 1
```

### Systems/MatchFlowController → MatchFlowController

```yaml
  rules: {fileID: 11400000, guid: 78839375472c2ef47957a9fd5bde7609, type: 2} # Assets/_Game/Data/DefaultMatchRules.asset
  lava: {fileID: 765233046} # LavaHazard
```

### Systems/Progression → ProgressionController

```yaml
  rules: {fileID: 11400000, guid: 21e4b8e3ad1aabf48a690bc5047965db, type: 2} # Assets/_Game/Data/DefaultEconomyRules.asset
  match: {fileID: 1867302567} # MatchFlowController
  themes: {fileID: 289363244} # TerrainThemeController
  savePathOverride:
```

### Systems/SimpleArtilleryAI → SimpleArtilleryAI

```yaml
  gameplayActions: {fileID: 690841745} # GameplayActionController
  gameManager: {fileID: 1612854978} # GameManager
  turnManager: {fileID: 1302739950} # TurnManager
  playerController: {fileID: 55054705} # BackendTestPlayerController
  muzzle: {fileID: 1750549423} # Transform
  aimReferenceFrame: {fileID: 1012447819} # Transform
  target: {fileID: 223520934} # Transform
  weaponData: {fileID: 11400000, guid: ee454cce595d0514bb30c03e11e432c5, type: 2} # Assets/_Game/Presentation/Assets/IntegratedBasicWeapon.asset
  thinkDelay: 0.5
  angleErrorRange: 2
  minAimAngle: 10
  maxAimAngle: 90
```

### Underground Lava → LavaHazard

```yaml
  lavaMaterial: {fileID: 2100000, guid: b334c3b629b27544397d05b5d1795d31, type: 2} # Assets/_Game/Presentation/Assets/BattleEffects.mat
  match: {fileID: 1867302567} # MatchFlowController
  surfaceY: -3.85
  left: -10
  right: 10
  bottom: -5.8
```

## پیوست پ: نمونه‌های Prefab

این فهرست ساختار و کامپوننت تمام Prefabهای پوشهٔ `_Game/Prefabs` را ثبت می‌کند؛ تنظیمات رفتارهای اختصاصی نیز آورده شده‌اند.

### `Assets/_Game/Art/FX/BasicExplosion/BasicExplosion_Visual.prefab`

| مسیر آبجکت | کامپوننت‌ها |
| --- | --- |
| BasicExplosion_Visual | Transform, SpriteRenderer, Animator |

### `Assets/_Game/Art/Projectiles/BasicShell_Visual.prefab`

| مسیر آبجکت | کامپوننت‌ها |
| --- | --- |
| BasicShell_Visual | Transform, SpriteRenderer, Rigidbody2D, CircleCollider2D, Projectile |

`Projectile`

```yaml
  maximumLifetime: 10
```

### `Assets/_Game/Art/Tanks/TankVisual.prefab`

| مسیر آبجکت | کامپوننت‌ها |
| --- | --- |
| TankVisual | Transform, Rigidbody2D, CapsuleCollider2D, TankMotor, TankAim |
| TankVisual/Body | Transform, SpriteRenderer |
| TankVisual/Tracks | Transform, SpriteRenderer |
| TankVisual/TurretPivot | Transform |
| TankVisual/TurretPivot/BarrelPivot | Transform |
| TankVisual/TurretPivot/BarrelPivot/Barrel | Transform, SpriteRenderer |
| TankVisual/TurretPivot/Turret | Transform, SpriteRenderer |

`TankMotor`

```yaml
  movementSpeed: 3
  acceleration: 7
  braking: 12
  airControl: 0.15
  maxSlopeAngle: 50
  groundProbeDistance: 0.12
  alignmentSpeed: 100
  groundAdhesion: 0.8
  groundLayers:
    serializedVersion: 2
    m_Bits: 4294967291
```

`TankAim`

```yaml
  aimPivot: {fileID: 2760319534709595669}
  minAimAngle: 0
  maxAimAngle: 90
```

### `Assets/_Game/Prefabs/Mock/MockProjectile.prefab`

| مسیر آبجکت | کامپوننت‌ها |
| --- | --- |
| MockProjectile | Transform, SpriteRenderer, Rigidbody2D, CircleCollider2D, Projectile, ProjectileFlight |

`Projectile`

```yaml
  maximumLifetime: 10
```

`ProjectileFlight`

```yaml
  alignToVelocity: 1
  rotationOffset: 0
  turnSpeed: 0
  minSpeedThreshold: 0.1
```

### `Assets/_Game/Prefabs/Mock/MockTank.prefab`

| مسیر آبجکت | کامپوننت‌ها |
| --- | --- |
| MockTank | Transform, Rigidbody2D, BoxCollider2D, HealthComponent, TankMotor, TankAim, TankController, WeaponController, BackendTestPlayerController |
| MockTank/Body | Transform, SpriteRenderer |
| MockTank/MockDamageProbe | Transform, CircleCollider2D |
| MockTank/TurretPivot | Transform |
| MockTank/TurretPivot/Barrel | Transform, SpriteRenderer |
| MockTank/TurretPivot/Barrel/Muzzle | Transform |

`HealthComponent`

```yaml
  maxHealth: 100
```

`TankMotor`

```yaml
  movementSpeed: 3
```

`TankAim`

```yaml
  aimPivot: {fileID: 6302394252426415527}
  minAimAngle: 10
  maxAimAngle: 170
```

`TankController`

```yaml
  motor: {fileID: 7758006458663067181}
  aim: {fileID: 527646480115679999}
```

`WeaponController`

```yaml
  currentWeapon: {fileID: 11400000, guid: 764a8b59b6b5b084cb596fc3b3a12c5c, type: 2}
```

`BackendTestPlayerController`

```yaml
  playerIndex: 0
```

### `Assets/_Game/Prefabs/Projectiles/Projectile_Bouncy.prefab`

| مسیر آبجکت | کامپوننت‌ها |
| --- | --- |
| Projectile_Bouncy | Transform, SpriteRenderer, Rigidbody2D, CircleCollider2D, Projectile, ProjectileFlight, BouncyImpactHandler |

`Projectile`

```yaml
  maximumLifetime: 10
```

`ProjectileFlight`

```yaml
  alignToVelocity: 1
  rotationOffset: 0
  turnSpeed: 0
  minSpeedThreshold: 0.1
  overrideGravity: 0
  customGravityScale: 1
  overrideDrag: 0
  customLinearDamping: 0
  enableRocketThrust: 0
  thrustDelay: 0.2
  thrustAcceleration: 15
  maxSpeed: 30
  enableAirburst: 0
  airburstTime: 1.5
  triggerOnApex: 0
```

`BouncyImpactHandler`

```yaml
  maxBounces: 3
  bounciness: 0.75
  minBounceSpeed: 1
  explodeOnTankHit: 1
```

### `Assets/_Game/Prefabs/Projectiles/Projectile_Cluster.prefab`

| مسیر آبجکت | کامپوننت‌ها |
| --- | --- |
| Projectile_Cluster | Transform, SpriteRenderer, Rigidbody2D, CircleCollider2D, Projectile, ProjectileFlight, ClusterImpactHandler |

`Projectile`

```yaml
  maximumLifetime: 10
```

`ProjectileFlight`

```yaml
  alignToVelocity: 1
  rotationOffset: 0
  turnSpeed: 0
  minSpeedThreshold: 0.1
  overrideGravity: 0
  customGravityScale: 1
  overrideDrag: 0
  customLinearDamping: 0
  enableRocketThrust: 0
  thrustDelay: 0.2
  thrustAcceleration: 15
  maxSpeed: 30
  enableAirburst: 1
  airburstTime: 1.5
  triggerOnApex: 1
```

`ClusterImpactHandler`

```yaml
  submunitionPrefab: {fileID: 1567743675706068424, guid: 092304ac9fb5c09488fb6639a1ce27d2, type: 3}
  clusterCount: 4
  spreadAngle: 80
  ejectionSpeed: 7
  velocityInheritance: 0.25
  triggerOnAirburst: 1
  submunitionDamage: 14
  submunitionExplosionRadius: 0.9
```

### `Assets/_Game/Prefabs/Projectiles/Projectile_Digger.prefab`

| مسیر آبجکت | کامپوننت‌ها |
| --- | --- |
| Projectile_Digger | Transform, SpriteRenderer, Rigidbody2D, CircleCollider2D, Projectile, ProjectileFlight, DiggerImpactHandler |

`Projectile`

```yaml
  maximumLifetime: 10
```

`ProjectileFlight`

```yaml
  alignToVelocity: 1
  rotationOffset: 0
  turnSpeed: 0
  minSpeedThreshold: 0.1
  overrideGravity: 0
  customGravityScale: 1
  overrideDrag: 0
  customLinearDamping: 0
  enableRocketThrust: 0
  thrustDelay: 0.2
  thrustAcceleration: 15
  maxSpeed: 30
  enableAirburst: 0
  airburstTime: 1.5
  triggerOnApex: 0
```

`DiggerImpactHandler`

```yaml
  digSteps: 4
  digStepDistance: 0.25
  digCarveRadius: 0.34
  digStepInterval: 0.05
  alignAlongVelocity: 1
  explodeOnDirectTankHit: 1
```

### `Assets/_Game/Prefabs/Projectiles/Projectile_Floater.prefab`

| مسیر آبجکت | کامپوننت‌ها |
| --- | --- |
| Projectile_Floater | Transform, SpriteRenderer, Rigidbody2D, CircleCollider2D, Projectile, ProjectileFlight |

`Projectile`

```yaml
  maximumLifetime: 10
```

`ProjectileFlight`

```yaml
  alignToVelocity: 1
  rotationOffset: 0
  turnSpeed: 0
  minSpeedThreshold: 0.1
  overrideGravity: 1
  customGravityScale: 0.25
  overrideDrag: 1
  customLinearDamping: 2.5
  enableRocketThrust: 0
  thrustDelay: 0.2
  thrustAcceleration: 15
  maxSpeed: 30
  enableAirburst: 0
  airburstTime: 1.5
  triggerOnApex: 0
```

### `Assets/_Game/Prefabs/Projectiles/Projectile_HeavyMortar.prefab`

| مسیر آبجکت | کامپوننت‌ها |
| --- | --- |
| Projectile_HeavyMortar | Transform, SpriteRenderer, Rigidbody2D, CircleCollider2D, Projectile, ProjectileFlight |

`Projectile`

```yaml
  maximumLifetime: 10
```

`ProjectileFlight`

```yaml
  alignToVelocity: 1
  rotationOffset: 0
  turnSpeed: 0
  minSpeedThreshold: 0.1
  overrideGravity: 1
  customGravityScale: 2
  overrideDrag: 0
  customLinearDamping: 0
  enableRocketThrust: 0
  thrustDelay: 0.2
  thrustAcceleration: 15
  maxSpeed: 30
  enableAirburst: 0
  airburstTime: 1.5
  triggerOnApex: 0
```

### `Assets/_Game/Prefabs/Projectiles/Projectile_Railgun.prefab`

| مسیر آبجکت | کامپوننت‌ها |
| --- | --- |
| Projectile_Railgun | Transform, SpriteRenderer, Rigidbody2D, CircleCollider2D, Projectile, ProjectileFlight |

`Projectile`

```yaml
  maximumLifetime: 10
```

`ProjectileFlight`

```yaml
  alignToVelocity: 1
  rotationOffset: 0
  turnSpeed: 0
  minSpeedThreshold: 0.1
  overrideGravity: 1
  customGravityScale: 0
  overrideDrag: 1
  customLinearDamping: 0
  enableRocketThrust: 0
  thrustDelay: 0.2
  thrustAcceleration: 15
  maxSpeed: 30
  enableAirburst: 0
  airburstTime: 1.5
  triggerOnApex: 0
```

### `Assets/_Game/Prefabs/Projectiles/Projectile_Rocket.prefab`

| مسیر آبجکت | کامپوننت‌ها |
| --- | --- |
| Projectile_Rocket | Transform, SpriteRenderer, Rigidbody2D, CircleCollider2D, Projectile, ProjectileFlight |

`Projectile`

```yaml
  maximumLifetime: 10
```

`ProjectileFlight`

```yaml
  alignToVelocity: 1
  rotationOffset: 0
  turnSpeed: 0
  minSpeedThreshold: 0.1
  overrideGravity: 1
  customGravityScale: 0.3
  overrideDrag: 0
  customLinearDamping: 0
  enableRocketThrust: 1
  thrustDelay: 0.25
  thrustAcceleration: 25
  maxSpeed: 35
  enableAirburst: 0
  airburstTime: 1.5
  triggerOnApex: 0
```

### `Assets/_Game/Presentation/Assets/IntegratedProjectile.prefab`

| مسیر آبجکت | کامپوننت‌ها |
| --- | --- |
| IntegratedProjectile | Transform, SpriteRenderer (disabled), Rigidbody2D, CircleCollider2D, Projectile, ProjectilePresentation, ProjectileFlight |

`Projectile`

```yaml
  maximumLifetime: 10
```

`ProjectilePresentation`

```yaml
  visual: {fileID: 7730273682082506397}
  explosionVisual: {fileID: 8023549953916221471, guid: 0e693835244cccd42a514438d5b40c35, type: 3}
```

`ProjectileFlight`

```yaml
  alignToVelocity: 1
  rotationOffset: 0
  turnSpeed: 0
  minSpeedThreshold: 0.1
  overrideGravity: 0
  customGravityScale: 1
  overrideDrag: 0
  customLinearDamping: 0
  enableRocketThrust: 0
  thrustDelay: 0.2
  thrustAcceleration: 15
  maxSpeed: 30
  enableAirburst: 0
  airburstTime: 1.5
  triggerOnApex: 0
```

## پیوست ت: آبجکت‌های زمان اجرا

| سازنده | آبجکت/نقش |
| --- | --- |
| `GameInterface.Start` | صفحه‌های منو، آماده‌سازی، تنظیمات، راهنما، توقف، تأیید خروج، نتیجه و چرخش صفحه |
| `BattleInterface.Build` | اطلاعات نبرد، دکمه‌های نگه‌داشتنی، شلیک و فهرست سلاح |
| `GarageHud` | کارت‌ها، انتخاب تجهیزات، خرید و ارتقا |
| `WeaponController.Fire` | نمونهٔ Prefab پرتابه با مالک، آسیب، سرعت و ShotContext |
| `ClusterImpactHandler` | ریزگلوله‌ها با همان ShotContext والد |
| `ProjectileFeedback` / `BattleFeedback` | رد، حلقه، خطوط و ذرات، عدد آسیب و نمایش سپر |
| `FeedbackAudio` | منابع پخش صدا و کلیپ‌های تولیدی |
| `DestructibleTerrain` | بافت/نمایش سطح تغییرکرده و تغییرات برخوردگر |

آبجکت‌های ایجادشده هنگام اجرا را برای اصلاح پایدار، در اسکریپت سازنده یا دارایی مربوط تغییر دهید؛ تغییر Inspector در Play Mode معمولاً بعد از خروج باقی نمی‌ماند.

## پیوست ث: فهرست کامل اسکریپت‌های پروژه

| فایل | نوع استفاده |
| --- | --- |
| Assets/_Game/AI/SimpleArtilleryAI.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Art/Background/Editor/BuildMvpBackground.cs | ابزار Editor |
| Assets/_Game/Art/HUD/Editor/BuildMvpHudAndExplosion.cs | ابزار Editor |
| Assets/_Game/Art/Projectiles/Editor/PrepareBasicShellProjectile.cs | ابزار Editor |
| Assets/_Game/Art/Tanks/Editor/BuildTankArt.cs | ابزار Editor |
| Assets/_Game/Art/Terrain/Editor/BuildRollingTerrain.cs | ابزار Editor |
| Assets/_Game/Art/Weapons/Editor/ImportBasicShellIcon.cs | ابزار Editor |
| Assets/_Game/Core/Editor/PhaseFiveSetup.cs | ابزار Editor |
| Assets/_Game/Core/Editor/PhaseFourSetup.cs | ابزار Editor |
| Assets/_Game/Core/Editor/PhaseOneSetup.cs | ابزار Editor |
| Assets/_Game/Core/Editor/PhaseSevenSetup.cs | ابزار Editor |
| Assets/_Game/Core/Editor/PhaseSixSetup.cs | ابزار Editor |
| Assets/_Game/Core/Editor/PhaseThreeSetup.cs | ابزار Editor |
| Assets/_Game/Core/Editor/PhaseTwoSetup.cs | ابزار Editor |
| Assets/_Game/Core/Editor/VolcanicSetup.cs | ابزار Editor |
| Assets/_Game/Core/GameManager.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Core/GameplayActionController.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Core/HealthComponent.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Core/IDamageable.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Core/MatchFlowController.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Core/MatchResult.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Core/MatchRules.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Core/MatchState.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Core/PlayerProgress.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Core/ProgressionController.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Core/ProgressStore.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Core/TurnManager.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Data/EconomyRules.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Data/ShieldData.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Data/TankDefinition.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Data/WeaponData.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Player/IPlayerController.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Player/PlayerControllerBase.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Presentation/BattleFeedback.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Presentation/BattleInterface.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Presentation/FeedbackAudio.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Presentation/FeedbackStyle.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Presentation/FitInterfacePanel.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Presentation/GameInterface.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Presentation/GarageHud.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Presentation/HudSafeArea.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Presentation/IntegratedHoldButton.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Presentation/IntegratedHud.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Presentation/IntegratedInput.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Presentation/PhaseOneHud.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Presentation/PhaseTwoHud.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Presentation/ProjectileFeedback.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Presentation/ProjectilePresentation.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Presentation/WeaponPresentation.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Projectiles/BouncyImpactHandler.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Projectiles/ClusterImpactHandler.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Projectiles/DiggerImpactHandler.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Projectiles/ExplosionDamage.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Projectiles/IProjectileImpactHandler.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Projectiles/Projectile.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Projectiles/ProjectileFlight.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Projectiles/ShotContext.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Tank/Editor/TankSlopePhysicsSetup.cs | ابزار Editor |
| Assets/_Game/Tank/TankAim.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Tank/TankController.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Tank/TankLoadout.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Tank/TankMotor.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Tank/TankStats.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Terrain/DestructibleTerrain.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Terrain/Editor/CraterAndAimSetup.cs | ابزار Editor |
| Assets/_Game/Terrain/Editor/OpenMVPSceneHelper.cs | ابزار Editor |
| Assets/_Game/Terrain/Editor/TerrainThemeSetup.cs | ابزار Editor |
| Assets/_Game/Terrain/LavaHazard.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Terrain/TerrainThemeController.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Terrain/TerrainThemeDefinition.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Testing/BackendTestBootstrap.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Testing/BackendTestDiagnostics.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Testing/BackendTestInput.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Testing/BackendTestPlayerController.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Testing/ProjectileShowcaseAutomator.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Testing/ReleaseQualification.cs | زمان اجرا / مدل / تست |
| Assets/_Game/Weapons/WeaponController.cs | زمان اجرا / مدل / تست |

## پیوست ج: فهرست دارایی‌های تنظیمی

| دارایی | نوع اسکریپت |
| --- | --- |
| Assets/_Game/Art/Terrain/Themes/Grassland.asset | TerrainThemeDefinition |
| Assets/_Game/Art/Terrain/Themes/Rocklands.asset | TerrainThemeDefinition |
| Assets/_Game/Art/Terrain/Themes/Snowfield.asset | TerrainThemeDefinition |
| Assets/_Game/Data/DefaultEconomyRules.asset | EconomyRules |
| Assets/_Game/Data/DefaultMatchRules.asset | MatchRules |
| Assets/_Game/Data/MockBasicWeapon.asset | WeaponData |
| Assets/_Game/Data/Shield_Heavy.asset | ShieldData |
| Assets/_Game/Data/Shield_Light.asset | ShieldData |
| Assets/_Game/Data/Shield_None.asset | ShieldData |
| Assets/_Game/Data/Tanks/Tank_Bastion.asset | TankDefinition |
| Assets/_Game/Data/Tanks/Tank_Scout.asset | TankDefinition |
| Assets/_Game/Data/Tanks/Tank_Vanguard.asset | TankDefinition |
| Assets/_Game/Data/Weapons/Weapon_Bouncy.asset | WeaponData |
| Assets/_Game/Data/Weapons/Weapon_Cluster.asset | WeaponData |
| Assets/_Game/Data/Weapons/Weapon_Digger.asset | WeaponData |
| Assets/_Game/Data/Weapons/Weapon_Floater.asset | WeaponData |
| Assets/_Game/Data/Weapons/Weapon_HeavyMortar.asset | WeaponData |
| Assets/_Game/Data/Weapons/Weapon_Railgun.asset | WeaponData |
| Assets/_Game/Data/Weapons/Weapon_Rocket.asset | WeaponData |
| Assets/_Game/Presentation/Assets/IntegratedBasicWeapon.asset | WeaponData |

## پیوست چ: نمونه‌های Prefab داخل صحنه و Overrideها

تانک‌های واقعی صحنه نمونهٔ Prefab هستند و جزئیات آن‌ها در YAML به‌صورت مرجع و Override ذخیره می‌شود؛ بنابراین نام و تنظیمات آن‌ها باید علاوه بر جدول آبجکت‌های مستقل بررسی شوند. جدول هر نمونه، تفاوت آن با فایل Prefab پایه را نشان می‌دهد.

### Player1 — instance `55054702`

منبع: `Assets/_Game/Prefabs/Mock/MockTank.prefab`؛ والد Transform در صحنه: `436426177`.

| هدف در Prefab | فیلد Override | مقدار | ارجاع آبجکت |
| --- | --- | --- | --- |
| TankAim #527646480115679999 | aimPivot |  | {fileID: 1012447820} |
| TankAim #527646480115679999 | maxAimAngle | 90 | {fileID: 0} |
| MockTank #1956523670024084646 | m_Name | Player1 | {fileID: 0} |
| HealthComponent #2923494590816955901 | shield |  | {fileID: 11400000, guid: 8b416fa7950da1945b63df0ac56bde8a, type: 2} |
| Body #3857218022733174877 | m_IsActive | 0 | {fileID: 0} |
| SpriteRenderer #4169992478233334086 | m_Color.b | 0.3 | {fileID: 0} |
| SpriteRenderer #4169992478233334086 | m_Color.g | 0.4 | {fileID: 0} |
| SpriteRenderer #4169992478233334086 | m_Color.r | 1 | {fileID: 0} |
| Transform #8268840141615725777 | m_LocalPosition.x | 5 | {fileID: 0} |
| Transform #8268840141615725777 | m_LocalPosition.y | -1.4 | {fileID: 0} |
| Transform #8268840141615725777 | m_LocalPosition.z | 0 | {fileID: 0} |
| Transform #8268840141615725777 | m_LocalRotation.w | 1 | {fileID: 0} |
| Transform #8268840141615725777 | m_LocalRotation.x | 0 | {fileID: 0} |
| Transform #8268840141615725777 | m_LocalRotation.y | 0 | {fileID: 0} |
| Transform #8268840141615725777 | m_LocalRotation.z | 0 | {fileID: 0} |
| Transform #8268840141615725777 | m_LocalEulerAnglesHint.x | 0 | {fileID: 0} |
| Transform #8268840141615725777 | m_LocalEulerAnglesHint.y | 0 | {fileID: 0} |
| Transform #8268840141615725777 | m_LocalEulerAnglesHint.z | 0 | {fileID: 0} |
| TurretPivot #9045689744892579170 | m_IsActive | 0 | {fileID: 0} |
| WeaponController #9095673119330416512 | currentWeapon |  | {fileID: 11400000, guid: ee454cce595d0514bb30c03e11e432c5, type: 2} |

- آبجکت/کامپوننت افزوده‌شده در صحنه: `Transform` با fileID `1012447819`.

### Player0 — instance `223520933`

منبع: `Assets/_Game/Prefabs/Mock/MockTank.prefab`؛ والد Transform در صحنه: `436426177`.

| هدف در Prefab | فیلد Override | مقدار | ارجاع آبجکت |
| --- | --- | --- | --- |
| TankAim #527646480115679999 | aimPivot |  | {fileID: 970009293} |
| TankAim #527646480115679999 | maxAimAngle | 90 | {fileID: 0} |
| MockTank #1956523670024084646 | m_Name | Player0 | {fileID: 0} |
| HealthComponent #2923494590816955901 | shield |  | {fileID: 11400000, guid: 8b416fa7950da1945b63df0ac56bde8a, type: 2} |
| Body #3857218022733174877 | m_IsActive | 0 | {fileID: 0} |
| Transform #8268840141615725777 | m_LocalPosition.x | -5 | {fileID: 0} |
| Transform #8268840141615725777 | m_LocalPosition.y | -1.4 | {fileID: 0} |
| Transform #8268840141615725777 | m_LocalPosition.z | 0 | {fileID: 0} |
| Transform #8268840141615725777 | m_LocalRotation.w | 1 | {fileID: 0} |
| Transform #8268840141615725777 | m_LocalRotation.x | 0 | {fileID: 0} |
| Transform #8268840141615725777 | m_LocalRotation.y | 0 | {fileID: 0} |
| Transform #8268840141615725777 | m_LocalRotation.z | 0 | {fileID: 0} |
| Transform #8268840141615725777 | m_LocalEulerAnglesHint.x | 0 | {fileID: 0} |
| Transform #8268840141615725777 | m_LocalEulerAnglesHint.y | 0 | {fileID: 0} |
| Transform #8268840141615725777 | m_LocalEulerAnglesHint.z | 0 | {fileID: 0} |
| TurretPivot #9045689744892579170 | m_IsActive | 0 | {fileID: 0} |
| WeaponController #9095673119330416512 | currentWeapon |  | {fileID: 11400000, guid: ee454cce595d0514bb30c03e11e432c5, type: 2} |

- آبجکت/کامپوننت افزوده‌شده در صحنه: `Transform` با fileID `970009294`.

### TankVisual — instance `970009291`

منبع: `Assets/_Game/Art/Tanks/TankVisual.prefab`؛ والد Transform در صحنه: `223520934`.

| هدف در Prefab | فیلد Override | مقدار | ارجاع آبجکت |
| --- | --- | --- | --- |
| SpriteRenderer #1790361182222655111 | m_Color.b | 0.35 | {fileID: 0} |
| SpriteRenderer #1790361182222655111 | m_Color.g | 0.8 | {fileID: 0} |
| SpriteRenderer #1790361182222655111 | m_Color.r | 0.4 | {fileID: 0} |
| TankVisual #4775263564855255643 | m_Name | TankVisual | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalPosition.x | 0 | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalPosition.y | -0.5 | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalPosition.z | 0 | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalRotation.w | 1 | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalRotation.x | 0 | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalRotation.y | 0 | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalRotation.z | 0 | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalEulerAnglesHint.x | 0 | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalEulerAnglesHint.y | 0 | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalEulerAnglesHint.z | 0 | {fileID: 0} |
| SpriteRenderer #5518053566682395532 | m_Color.b | 0.35 | {fileID: 0} |
| SpriteRenderer #5518053566682395532 | m_Color.g | 0.8 | {fileID: 0} |
| SpriteRenderer #5518053566682395532 | m_Color.r | 0.4 | {fileID: 0} |
| SpriteRenderer #6886422968338585961 | m_Color.b | 0.35 | {fileID: 0} |
| SpriteRenderer #6886422968338585961 | m_Color.g | 0.8 | {fileID: 0} |
| SpriteRenderer #6886422968338585961 | m_Color.r | 0.4 | {fileID: 0} |
| SpriteRenderer #7447849395859950022 | m_Color.b | 0.35 | {fileID: 0} |
| SpriteRenderer #7447849395859950022 | m_Color.g | 0.8 | {fileID: 0} |
| SpriteRenderer #7447849395859950022 | m_Color.r | 0.4 | {fileID: 0} |

- آبجکت/کامپوننت افزوده‌شده در صحنه: `Transform` با fileID `291084910`.

### TankVisual — instance `1012447818`

منبع: `Assets/_Game/Art/Tanks/TankVisual.prefab`؛ والد Transform در صحنه: `55054703`.

| هدف در Prefab | فیلد Override | مقدار | ارجاع آبجکت |
| --- | --- | --- | --- |
| SpriteRenderer #1790361182222655111 | m_Color.b | 0.3 | {fileID: 0} |
| SpriteRenderer #1790361182222655111 | m_Color.g | 0.35 | {fileID: 0} |
| SpriteRenderer #1790361182222655111 | m_Color.r | 0.9 | {fileID: 0} |
| TankVisual #4775263564855255643 | m_Name | TankVisual | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalScale.x | -1 | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalPosition.x | 0 | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalPosition.y | -0.5 | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalPosition.z | 0 | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalRotation.w | 1 | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalRotation.x | 0 | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalRotation.y | 0 | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalRotation.z | 0 | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalEulerAnglesHint.x | 0 | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalEulerAnglesHint.y | 0 | {fileID: 0} |
| Transform #5471736183284426174 | m_LocalEulerAnglesHint.z | 0 | {fileID: 0} |
| SpriteRenderer #5518053566682395532 | m_Color.b | 0.3 | {fileID: 0} |
| SpriteRenderer #5518053566682395532 | m_Color.g | 0.35 | {fileID: 0} |
| SpriteRenderer #5518053566682395532 | m_Color.r | 0.9 | {fileID: 0} |
| SpriteRenderer #6886422968338585961 | m_Color.b | 0.3 | {fileID: 0} |
| SpriteRenderer #6886422968338585961 | m_Color.g | 0.35 | {fileID: 0} |
| SpriteRenderer #6886422968338585961 | m_Color.r | 0.9 | {fileID: 0} |
| SpriteRenderer #7447849395859950022 | m_Color.b | 0.3 | {fileID: 0} |
| SpriteRenderer #7447849395859950022 | m_Color.g | 0.35 | {fileID: 0} |
| SpriteRenderer #7447849395859950022 | m_Color.r | 0.9 | {fileID: 0} |

- آبجکت/کامپوننت افزوده‌شده در صحنه: `Transform` با fileID `1750549423`.

### تنظیمات اسکریپت‌های متصل به نمونه‌های Prefab

- `BackendTestPlayerController` با fileID صحنهٔ `55054705` مرجع stripped به کامپوننت Prefab است؛ مقادیر پایه در Prefab و تغییرات در جدول Override همان نمونه آمده‌اند.
- `BackendTestPlayerController` با fileID صحنهٔ `223520936` مرجع stripped به کامپوننت Prefab است؛ مقادیر پایه در Prefab و تغییرات در جدول Override همان نمونه آمده‌اند.
- `TankAim` با fileID صحنهٔ `1487234969` مرجع stripped به کامپوننت Prefab است؛ مقادیر پایه در Prefab و تغییرات در جدول Override همان نمونه آمده‌اند.
- `HealthComponent` با fileID صحنهٔ `1487234971` مرجع stripped به کامپوننت Prefab است؛ مقادیر پایه در Prefab و تغییرات در جدول Override همان نمونه آمده‌اند.
- `WeaponController` با fileID صحنهٔ `1487234972` مرجع stripped به کامپوننت Prefab است؛ مقادیر پایه در Prefab و تغییرات در جدول Override همان نمونه آمده‌اند.
- `TankAim` با fileID صحنهٔ `1823342653` مرجع stripped به کامپوننت Prefab است؛ مقادیر پایه در Prefab و تغییرات در جدول Override همان نمونه آمده‌اند.
- `HealthComponent` با fileID صحنهٔ `1823342655` مرجع stripped به کامپوننت Prefab است؛ مقادیر پایه در Prefab و تغییرات در جدول Override همان نمونه آمده‌اند.

## پیوست ح: جزئیات کامل به‌روزرسانی گدازه و تخریب زمین

تاریخ: ۲۵ اکتبر ۲۰۲۶.

زیر مسیر نبرد، آبجکت `Underground Lava` با کامپوننت `LavaHazard` اضافه شد. سطح گدازه در ارتفاع جهانی `-3.85` قرار دارد؛ موج‌های کوچک، جریان رنگ و ذرات روشن با یک Mesh محدود به ۸۶۴ رأس متحرک ساخته می‌شوند. بافت جدید یا آبجکت ذره در هر فریم ساخته نمی‌شود. متریال افکت موجود صریحاً در صحنه ارجاع داده شده است.

تماس پایین برخوردگر تانک با سطح گدازه، از مسیر `MatchFlowController` سقوط ثبت می‌کند. سپر و زره جلوی این شکست را نمی‌گیرند. پاشش نارنجی و فرو رفتن تصویر تانک نمایش داده می‌شود. تصویر اصلی برای شروع مسابقهٔ بعد برمی‌گردد. توقف بازی، برخورد و انیمیشن را متوقف می‌کند. قانون سقوط قدیمی زیر ارتفاع `killY` همچنان برای خروج از میدان باقی است.

تخریب انفجاری از آسیب به تانک جدا شد. `ExplosionDamage` برای زمین از `DestructibleTerrain.CarveExplosion` استفاده می‌کند؛ شعاع گودی برابر ۳۶٪ شعاع انفجار، حداکثر ۰٫۸۵ واحد و عمق مرکزی در برخورد سطحی حدود ۵۵٪ شعاع گودی است. بنابراین موشک با شعاع آسیب ۱٫۵، گودی تقریباً ۱٫۰۸ واحد پهنا و ۰٫۳۰ واحد عمق می‌سازد؛ قبلاً شعاع و عمق زمین از همان انفجار ۱٫۵ واحدی گرفته می‌شد. تیرهای قوی‌تر هنوز گودی بزرگ‌تری دارند و ضربه‌های تکراری می‌توانند زمین را تا گدازه پایین ببرند. مرز گودی و تیرگی خاک حفظ شده‌اند.

متد پایهٔ `Carve` برای تغییر مستقیم هندسه و حفاری باقی است. Prefab تیر حفار از ۶ گام ۰٫۳۵ واحدی با شعاع ۰٫۵، به ۴ گام ۰٫۲۵ واحدی با شعاع ۰٫۳۴ تغییر کرد. آسیب، شعاع آسیب به تانک، نفوذ سپر و مهمات سلاح‌ها تغییر نکرده‌اند. پنل پایین نبرد کمی شفاف‌تر شده تا حرکت گدازه پشت کنترل‌ها دیده شود.

هفت بررسی اختصاصی موفق شدند: بودجهٔ Mesh، عمق گودی موشک و حفظ زمین دور، سقف گودی انفجار بسیار بزرگ، بازنشانی و ورودی نامعتبر، حفظ آسیب ۳۲ واحدی، توقف/شکست سقوط/پاشش یکتا و بازیابی تانک در مسابقهٔ بعد. ۱۹ بررسی قوانین، ۱۶ بررسی آسیب و سپر و ۱۷۶ بررسی فاز هفتم شامل تمام ۱۶۸ ترکیب قانونی نیز با کد جدید موفق بودند. خروجی ویندوز قبلی شامل این تغییر تازه نیست و APK نیز هنوز ساخته نشده است؛ در این مرحله اجرای داخل Unity بررسی شده است.

گزینه‌های ویرایشگر در `Tools > Tank Game > Volcanic Update` قرار دارند. نصب از Edit Mode با `Install Lava and Balanced Craters` و آزمون جداگانه با `Verify Lava and Craters` انجام می‌شود. آزمون از ذخیرهٔ جدا در `Library/VolcanicTests` استفاده می‌کند. گزارش اصلی در `Library/VolcanicVerification.txt` و نسخهٔ ثبت‌شده در `Docs/Verification/VolcanicVerification.txt` قرار دارد.
