# SERVICE CATALOG — Profile Service

| Item | Value |
|------|--------|
| Name | profileservice |
| Repo | https://github.com/amirreza-fnt/profiles.git |
| Public domain | https://apiweb-profilesystem.sabzevar.ir/ |
| Kestrel port | **5027** |
| Stack | .NET 8, EF Core, SQL Server |
| Auth | SSO Bearer (`/api/auth/me`) + `X-Api-Key` |
| Depends on | sso-login-service (:5001), File Storage (keys only), SQL Server |
| Deploy path | `/opt/profileservice` |
| Env file | `/etc/profileservice.env` |
| Logs | `journalctl -u profileservice` / `/var/log/profileservice/` |
| Health | `GET /health` |
| API prefix | `/api/v1/profiles` |
