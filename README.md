# Profile Service — سامانه اطلاعات کاربران (SSO وزارت کشور)

میکروسرویس مستقل برای نگهداری اطلاعات هویتی و تکمیلی کاربران.  
**کلید اصلی:** کد ملی ۱۰ رقمی.  
**احراز هویت:** توکن JWT سرویس SSO (`/api/auth/me`) یا `X-Api-Key` سامانه‌های مجاز.  
**دامنه خارجی:** https://apiweb-profilesystem.sabzevar.ir/  
**پورت اجرا:** `5027`

## معماری

```
SSO وزارت کشور → sso-login-service → POST /api/v1/profiles/ensure (ApiKey)
کاربر / سامانه‌ها → Profile Service API (Bearer یا ApiKey)
تصویر/آواتار → File Storage (فقط FileId + ShortCode اینجا ذخیره می‌شود)
```

لایه‌ها: `Domain` / `Application` / `Infrastructure` / `Api` (.NET 8 + EF Core + SQL Server)

## تفکیک منبع داده

| بلوک | منبع | نمونه |
|------|------|--------|
| `identity` | `Sso` | نام، نام‌خانوادگی، موبایل SSO |
| `userCompleted` | `User` | تاریخ تولد کاربر، آواتار، تصویر |
| `contacts` | `Sso` یا `User` + `isVerified` | موبایل / تلفن ثابت |

- موبایل آمده از SSO → **Verified خودکار**
- موبایل/تلفن ثبت دستی → باید Verification شود (SMS / تماس)
- استفاده از شماره بدون `isVerified=true` در منطق کسب‌وکار مجاز نیست

## API خلاصه

| Method | Path | Auth | توضیح |
|--------|------|------|--------|
| POST | `/api/v1/profiles/ensure` | ApiKey | ایجاد/همگام‌سازی بعد از لاگین SSO |
| GET | `/api/v1/profiles/{nationalCode}` | Bearer/ApiKey | دریافت پروفایل |
| GET | `/api/v1/profiles/me` | Bearer | پروفایل کاربر جاری |
| PATCH | `/api/v1/profiles/me` | Bearer | تکمیل اطلاعات مجاز |
| PUT | `/api/v1/profiles/me/avatar` | Bearer | مرجع آواتار فایل‌سرویس |
| PUT | `/api/v1/profiles/me/photo` | Bearer | مرجع تصویر شخص |
| POST | `/api/v1/profiles/me/contacts` | Bearer | افزودن موبایل/ثابت |
| POST | `/api/v1/profiles/me/contacts/{id}/verify/send` | Bearer | ارسال کد |
| POST | `/api/v1/profiles/me/contacts/{id}/verify/confirm` | Bearer | تأیید کد |
| GET | `/health` | — | سلامت |

تغییرات مهم (موبایل، ثابت، آواتار، تصویر) در `AuditLogs` ثبت می‌شوند.

## نمونه Ensure بعد از SSO

```bash
curl -X POST https://apiweb-profilesystem.sabzevar.ir/api/v1/profiles/ensure \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: YOUR_SERVICE_KEY" \
  -d '{
    "nationalCode": "0795032307",
    "firstName": "علی",
    "lastName": "محمدی",
    "fatherName": "حسین",
    "mobile": "09151234567",
    "birthDate": "1370/01/01"
  }'
```

## استقرار آفلاین روی AlmaLinux 10 (پورت 5027)

سرور اینترنت برای `dotnet publish` ندارد؛ پوشه **`publish/`** از قبل در ریپو هست.

### ۱) پیش‌نیاز

- .NET 8 **Runtime** (`dotnet --list-runtimes`)
- SQL Server در دسترس
- nginx (برای دامنه HTTPS)
- systemd

### ۲) Clone

```bash
cd /opt
sudo git clone https://github.com/amirreza-fnt/profiles.git profileservice-repo
cd profileservice-repo
```

### ۳) تنظیم env

```bash
sudo cp deploy/profileservice.env.example /etc/profileservice.env
sudo nano /etc/profileservice.env
sudo chmod 640 /etc/profileservice.env
```

حداقل این‌ها را درست کنید: `ConnectionStrings__Profile`، کلیدهای `InternalAuth__ApiKeys__*`، `Sso__BaseUrl`.

دیتابیس نمونه: `apiweb-profilesystem`

### ۴) Deploy

```bash
sed -i 's/\r$//' deploy/deploy.sh
chmod +x deploy/deploy.sh
sudo bash deploy/deploy.sh
```

اسکریپت `publish/` را به `/opt/profileservice` کپی می‌کند، systemd و nginx را تنظیم می‌کند و سرویس را روی **5027** بالا می‌آورد.

### ۵) SSL دامنه

در `/etc/nginx/conf.d/apiweb-profilesystem.conf` خطوط `ssl_certificate` را فعال و مسیر گواهی را بگذارید، سپس:

```bash
sudo nginx -t && sudo systemctl reload nginx
```

دامنه: **https://apiweb-profilesystem.sabzevar.ir/**

### ۶) بررسی

```bash
curl http://127.0.0.1:5027/health
curl http://SERVER_IP:5027/health
curl https://apiweb-profilesystem.sabzevar.ir/health

sudo systemctl status profileservice
sudo journalctl -u profileservice -f
```

### دستورات سریع بعد از به‌روزرسانی

```bash
cd /opt/profileservice-repo
sudo git pull
sudo bash deploy/deploy.sh
sudo systemctl restart profileservice
```

## توسعه محلی

```bash
dotnet restore
dotnet run --project src/ProfileService.Api --urls http://localhost:5027
```

Swagger: http://localhost:5027/swagger

## امنیت (پیاده‌سازی‌شده)

1. تغییر پروفایل فقط با توکن کاربر یا ApiKey سرویس — نه فقط با کد ملی در body  
2. کد ملی شناسه است، نه مدرک احراز هویت  
3. Authorization بر اساس Actor (User/Service)  
4. کد Verification: کوتاه‌مدت، یکبارمصرف، سقف تلاش، هش‌شده در DB  
5. تصویر فقط به‌صورت کلید فایل‌سرویس  
6. Audit Log برای تغییرات مهم  
7. HTTPS از طریق nginx دامنه  
8. بلوک `identity` و `userCompleted` با `source` جدا
