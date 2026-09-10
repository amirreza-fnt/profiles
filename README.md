# Profile Service — سامانه اطلاعات کاربران

دامنه: https://apiweb-profilesystem.sabzevar.ir/  
پورت: **5027**  
ریپو: https://github.com/amirreza-fnt/profiles.git

تنظیمات DB / SSO / ApiKey از قبل داخل ریپو پر شده؛ روی سرور فقط clone + deploy کافی است.

## آپلود و اجرا روی AlmaLinux 10

```bash
cd /opt
sudo git clone https://github.com/amirreza-fnt/profiles.git profileservice-repo
cd /opt/profileservice-repo

# اگر دیتابیس هنوز ساخته نشده، یک‌بار در SSMS:
# deploy/create-database.sql

sed -i 's/\r$//' deploy/deploy.sh
chmod +x deploy/deploy.sh
sudo bash deploy/deploy.sh
```

بررسی:

```bash
curl http://127.0.0.1:5027/health
curl http://SERVER_IP:5027/swagger/index.html
sudo systemctl status profileservice
```

به‌روزرسانی بعدی:

```bash
cd /opt/profileservice-repo
sudo git pull
sudo bash deploy/deploy.sh
```

## اتصال دیتابیس (از قبل تنظیم‌شده)

| پارامتر | مقدار |
|---------|--------|
| Server | `185.255.91.242,2019` |
| Database | `apiweb-profilesystem` |
| User | `apiwebprofilesystemuser` |
| Password | در `deploy/profileservice.env.example` |

> در فایل env مقدار `$$` برای systemd است تا `$` پسورد درست برسد.

## ApiKey مشترک با سایر سرویس‌ها

- `dev-internal-key-137` — سامانه‌های داخلی (مثل coding / car-referral)
- `dev-internal-key-profile` — مخصوص SSO / پروفایل

SSO: `http://127.0.0.1:5001`  
فایل‌سرویس: `https://storage.sabzevar.ir`

## تست سریع Ensure

```bash
curl -X POST http://127.0.0.1:5027/api/v1/profiles/ensure \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: dev-internal-key-137" \
  -d '{
    "nationalCode": "0795032307",
    "firstName": "علی",
    "lastName": "محمدی",
    "mobile": "09151234567"
  }'
```

## API

| Method | Path | Auth |
|--------|------|------|
| POST | `/api/v1/profiles/ensure` | ApiKey |
| GET | `/api/v1/profiles/{nationalCode}` | Bearer / ApiKey |
| GET/PATCH | `/api/v1/profiles/me` | Bearer |
| PUT | `/api/v1/profiles/me/avatar` | Bearer |
| PUT | `/api/v1/profiles/me/photo` | Bearer |
| POST | `/api/v1/profiles/me/contacts` | Bearer |
| POST | `/api/v1/profiles/me/contacts/{id}/verify/send` | Bearer |
| POST | `/api/v1/profiles/me/contacts/{id}/verify/confirm` | Bearer |
| GET | `/health` | — |
