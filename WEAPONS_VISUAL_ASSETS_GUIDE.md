# راهنمای آرت، اسپریت‌ها و آیکون‌های سلاح‌ها (Weapons Visual Assets Guide)
> این مستند برای همگام‌سازی و راهنمایی اعضای تیم توسعه بازی تانک تهیه شده است و نحوه پیاده‌سازی، ابعاد، استانداردهای فنی و اتصال گرافیک ۷ سلاح بازی را شرح می‌دهد.

---

## 📌 فهرست مطالب
1. [خلاصه تغییرات گرافیکی (Overview)](#۱-خلاصه-تغییرات-گرافیکی)
2. [مشخصات و مسیر فایل‌های ۷ سلاح (Assets Manifest)](#۲-مشخصات-و-مسیر-فایلهای-۷-سلاح)
3. [جزئیات بصری هر پرتابه و رفتار آن (Detailed Breakdown)](#۳-جزئیات-بصری-هر-پرتابه-و-رفتار-آن)
4. [استاندارد تنظیمات ایمپورت در یونیتی (Texture Import Settings)](#۴-استاندارد-تنظیمات-ایمپورت-در-یونیتی)
5. [یکپارچه‌سازی با کد و رابط کاربری HUD (Code Integration)](#۵-یکپارچهسازی-با-کد-و-رابط-کاربری-hud)
6. [راهنمای سریع تست برای هم‌تیمی (Testing & Verification)](#۶-راهنمای-سریع-تست-برای-همتیمی)

---

## ۱. خلاصه تغییرات گرافیکی

در این فاز، تمامی پرتابه‌های هندسی آزمایشی با **آرت دوبعدی باکیفیت و اختصاصی (Stylized 2D Arcade Art)** جایگزین شدند:
* **۷ اسپریت پرواز (In-Flight Sprites):** دارای پس‌زمینه کاملاً شفاف (Transparent Alpha)، بدون هاله تیره، هم‌راستا با جهت حرکت افقی رو به راست.
* **۷ آیکون رابط کاربری منو (HUD Icons):** ابعاد استاندارد مربعی `256x256` پیکسل با گوشه‌های گرد و پس‌زمینه اختصاصی سلاح.
* **لوگوی اختصاصی بازی (Game Logo Badge):** آیکون نمادین تانک نظامی برای منوها و بخش‌های معرفی بازی.
* **ارتقای HUD بازی:** اضافه شدن تصویر زنده آیکون به باکس راهنمای بالای صفحه هنگام انتخاب سلاح.

---

## ۲. مشخصات و مسیر فایل‌های ۷ سلاح

| ردیف | نام سلاح | کلید شورتکات | مسیر فایل اسپریت پرواز (Sprite) | مسیر فایل آیکون منو (HUD Icon) | پری‌فب مربوطه (Prefab) | دیتای سلاح (WeaponData) |
| :---: | :--- | :---: | :--- | :--- | :--- | :--- |
| **۱** | **Rocket** (موشک شتاب‌گیرنده) | <kbd>1</kbd> | `Assets/_Game/Art/Projectiles/Projectile_Rocket.png` | `Assets/_Game/Art/Weapons/Weapon_Rocket_Icon.png` | `Projectile_Rocket.prefab` | `Weapon_Rocket.asset` |
| **۲** | **Heavy Mortar** (خمپاره سنگین) | <kbd>2</kbd> | `Assets/_Game/Art/Projectiles/Projectile_HeavyMortar.png` | `Assets/_Game/Art/Weapons/Weapon_HeavyMortar_Icon.png` | `Projectile_HeavyMortar.prefab` | `Weapon_HeavyMortar.asset` |
| **۳** | **Railgun** (ریل‌گان پلاسما) | <kbd>3</kbd> | `Assets/_Game/Art/Projectiles/Projectile_Railgun.png` | `Assets/_Game/Art/Weapons/Weapon_Railgun_Icon.png` | `Projectile_Railgun.prefab` | `Weapon_Railgun.asset` |
| **۴** | **Floater** (ضدجاذبه شناور) | <kbd>4</kbd> | `Assets/_Game/Art/Projectiles/Projectile_Floater.png` | `Assets/_Game/Art/Weapons/Weapon_Floater_Icon.png` | `Projectile_Floater.prefab` | `Weapon_Floater.asset` |
| **۵** | **Bouncy** (گوی کمانه‌ای) | <kbd>5</kbd> | `Assets/_Game/Art/Projectiles/Projectile_Bouncy.png` | `Assets/_Game/Art/Weapons/Weapon_Bouncy_Icon.png` | `Projectile_Bouncy.prefab` | `Weapon_Bouncy.asset` |
| **۶** | **Cluster** (بمب خوشه‌ای) | <kbd>6</kbd> | `Assets/_Game/Art/Projectiles/Projectile_Cluster.png` | `Assets/_Game/Art/Weapons/Weapon_Cluster_Icon.png` | `Projectile_Cluster.prefab` | `Weapon_Cluster.asset` |
| **۷** | **Digger** (مته‌ای زمین‌کَن) | <kbd>7</kbd> | `Assets/_Game/Art/Projectiles/Projectile_Digger.png` | `Assets/_Game/Art/Weapons/Weapon_Digger_Icon.png` | `Projectile_Digger.prefab` | `Weapon_Digger.asset` |
| **-** | **Tank Game Logo** (لوگو پروژه) | - | - | `Assets/_Game/Art/HUD/TankGame_Logo.png` | - | - |

---

## ۳. جزئیات بصری هر پرتابه و رفتار آن

### ۱. موشک شتاب‌گیرنده (Rocket Missile)
* **هویت بصری:** بدنه آیرودینامیک سفید و خاکستری با خطوط هشداردهنده نارنجی و شعله پیشران آتشین زرد/نارنجی در انتها.
* **رفتار فیزیکی:** پرتاب اولیه بالستیک، روشن شدن موتور راکت در هوا پس از تاخیر کوتاه و شتاب‌گیری مستقیم به سمت هدف.
* **تنظیمات پری‌فب:** اسکیل `(0.22, 0.22, 1)`، رنگ `White`، لایه نمایشی `Sorting Order = 20`.

### ۲. خمپاره سنگین (Heavy Mortar)
* **هویت بصری:** گلوله بمب توپخانه‌ای چاق با رنگ سبز ارتشی زیتونی (Olive Drab)، نوارهای هشدار زرد/مشکی و دود سیاه متراکم خروجی.
* **رفتار فیزیکی:** جاذبه بالا (1.8G)، قوس پرتابی شیب‌دار، نفوذ عمودی شدید و ایجاد گودال تخریبی بزرگ در خاک.

### ۳. ریل‌گان الکترومغناطیسی (Railgun)
* **هویت بصری:** دارت جنبشی نقره‌ای بسیار باریک با سیم‌پیچ‌های آبی برقی، صاعقه‌های نورانی و هاله پلاسمای نئون فیروزه‌ای (Cyan).
* **رفتار فیزیکی:** سرعت مافوق صوت، جاذبه صفر (0G)، مسیر کاملاً خطی و مستقیم مثل اشعه لیزر.

### ۴. کپسول ضدجاذبه شناور (Floater)
* **هویت بصری:** محفظه کریستالی با هسته بنفش درخشان، حلقه‌های دافعه گرانشی متالیک و رد ذرات معلق اثیری در آسمان.
* **رفتار فیزیکی:** جاذبه بسیار ضعیف (0.25G)، شناوری طولانی در هوا و حرکت بادکنکی و نرم در آسمان بازی.

### ۵. گوی کمانه‌ای ضربه‌گیر (Bouncy Ball)
* **هویت بصری:** کره زره‌پوش پلیمری با پترن شش‌ضلعی سبز لیمویی فسفری (Lime) و موج انرژی سینوسی فنری در پشت سر.
* **رفتار فیزیکی:** بازتاب الاستیک تا ۳ بار در برخورد با سطوح و دیواره‌ها (`BouncyImpactHandler`) و انفجار در برخورد مستقیم با تانک دشمن.

### ۶. بمب خوشه‌ای (Cluster Dispenser)
* **هویت بصری:** قوطی زره‌پوش با سر مخروطی قرمز و درزهای مکانیکی که نشان‌دهنده پرتاب ریزبمب‌های کوچک از بالا و پایین است.
* **رفتار فیزیکی:** حرکت تا بالاترین نقطه پرتاب (Apex)، انفجار مخزن و پخش شدن ۴ ریزبمب با الگوی بادبزنی برای بمباران محوطه.

### ۷. مته زمین‌کَن (Digger / Mole)
* **هویت بصری:** موشک زرد صنعتی با مته مارپیچی تیتانیومی در نوک، دریچه‌های خروج خاک و تراشه‌های سنگ در اطراف.
* **رفتار فیزیکی:** حفر تونل و عبور از داخل خاک و تپه‌ها با تخریب مسیر زمین (`DestructibleTerrain.Carve`) و انفجار در عمق یا زیر پای تانک حریف.

---

## ۴. استاندارد تنظیمات ایمپورت در یونیتی (Texture Import Settings)

تمامی تصاویر با دقت پیکسلی بالا و شفافیت کامل آلفا خروجی گرفته شده و تنظیمات ایمپورت آن‌ها در ادیتور یکدست شده است:

```yaml
Texture Type: Sprite (2D and UI)
Sprite Mode: Single
Pixels Per Unit: 100
Alpha Source: Input Texture Alpha
Alpha is Transparency: Checked (True)
Generate Mip Maps: Unchecked (False)
Wrap Mode: Clamp
Filter Mode: Bilinear
Max Size: 512 (اسپریت‌های پرواز) / 256 (آیکون‌های منو)
Sprite Pivot: Center (0.5, 0.5)
```

> [!NOTE]
> با توجه به اینکه نقطه محور (`Pivot`) در مرکز قرار داده شده است، چرخش پرتابه‌ها حول محور سرعت توسط `ProjectileFlight.cs` کاملاً متقارن و بدون لرزش (Jitter) انجام می‌شود.

---

## ۵. یکپارچه‌سازی با کد و رابط کاربری HUD

### ۱. ارتقای کلاس `WeaponData.cs`
یک فیلد سرشاخه‌ای با متغیر نمایشی برای آیکون سلاح به ScriptableObject افزوده شد:
```csharp
[SerializeField] private Sprite icon = null;
public Sprite Icon => icon;
```
تمامی فایل‌های `.asset` سلاح‌ها اکنون به آیکون مربوط به خود در `Assets/_Game/Art/Weapons/` متصل هستند.

### ۲. نمایش تصویر در HUD درون بازی (`IntegratedInput.cs` و `BackendTestInput.cs`)
باکس HUD در بالای تصویر گسترش یافته و اکنون تصویر باکیفیت سلاح فعال را به‌صورت زنده با `GUI.DrawTexture` رسم می‌کند:
```csharp
if (weaponIcon != null && weaponIcon.texture != null)
{
    GUI.color = Color.white;
    GUI.DrawTexture(new Rect(25, 22, 70, 70), weaponIcon.texture, ScaleMode.ScaleToFit);
}
```

---

## ۶. راهنمای سریع تست برای هم‌تیمی

دوست شما پس از دریافت تغییرات می‌تواند بدون هیچ تنظیم دستی مراحل زیر را انجام دهد:

1. **دریافت آخرین تغییرات از گیت:**
   ```bash
   git pull origin main
   ```
2. **باز کردن صحنه گرافیکی اصلی بازی:**
   * در محیط ادیتور کلید <kbd>F6</kbd> را بزنید.
   * یا از منوی بالای یونیتی گزینه زیر را انتخاب کنید:
     `Tools > Tank Game > Open Main Graphical Scene (MVP)`
3. **اجرای بازی:**
   * دکمه **Play** را در یونیتی بزنید.
   * با کلیدهای عددی <kbd>1</kbd> تا <kbd>7</kbd> بین ۷ سلاح سوئیچ کنید (تصویر هر سلاح بلافاصله در باکس بالای صفحه تغییر می‌کند).
   * با کلیدهای <kbd>W</kbd> و <kbd>S</kbd> لوله تانک را زاویه‌دار کنید.
   * با کلید <kbd>Space</kbd> شلیک کنید و پرواز و برخورد پرتابه جدید را مشاهده کنید.
   * با کلید <kbd>R</kbd> می‌توانید مسابقه را در هر لحظه ری‌استارت کنید.

---

## ۷. مانیفست فایل‌های تغییریافته و جدید در گیت

```text
Assets/
└── _Game/
    ├── Art/
    │   ├── HUD/
    │   │   ├── TankGame_Logo.png
    │   │   └── TankGame_Logo.png.meta
    │   ├── Projectiles/
    │   │   ├── Projectile_Rocket.png (+ .meta)
    │   │   ├── Projectile_HeavyMortar.png (+ .meta)
    │   │   ├── Projectile_Railgun.png (+ .meta)
    │   │   ├── Projectile_Floater.png (+ .meta)
    │   │   ├── Projectile_Bouncy.png (+ .meta)
    │   │   ├── Projectile_Cluster.png (+ .meta)
    │   │   └── Projectile_Digger.png (+ .meta)
    │   └── Weapons/
    │       ├── Weapon_Rocket_Icon.png (+ .meta)
    │       ├── Weapon_HeavyMortar_Icon.png (+ .meta)
    │       ├── Weapon_Railgun_Icon.png (+ .meta)
    │       ├── Weapon_Floater_Icon.png (+ .meta)
    │       ├── Weapon_Bouncy_Icon.png (+ .meta)
    │       ├── Weapon_Cluster_Icon.png (+ .meta)
    │       └── Weapon_Digger_Icon.png (+ .meta)
    ├── Data/
    │   ├── WeaponData.cs
    │   └── Weapons/
    │       └── Weapon_*.asset (7 assets updated)
    ├── Prefabs/
    │   └── Projectiles/
    │       └── Projectile_*.prefab (7 prefabs updated)
    ├── Presentation/
    │   ├── Assets/IntegratedProjectile.prefab
    │   └── IntegratedInput.cs
    └── Testing/
        └── BackendTestInput.cs
```
