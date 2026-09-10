-- Profile Service DB bootstrap (run once in SSMS if DB/login not created yet)
-- Server: 185.255.91.242,2019

IF DB_ID(N'apiweb-profilesystem') IS NULL
BEGIN
    CREATE DATABASE [apiweb-profilesystem];
END
GO

USE [master];
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'apiwebprofilesystemuser')
BEGIN
    CREATE LOGIN [apiwebprofilesystemuser]
    WITH PASSWORD = N'Ad$g84&hQ!',
         CHECK_POLICY = ON,
         CHECK_EXPIRATION = OFF;
END
GO

USE [apiweb-profilesystem];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'apiwebprofilesystemuser')
BEGIN
    CREATE USER [apiwebprofilesystemuser] FOR LOGIN [apiwebprofilesystemuser];
END
GO

ALTER ROLE [db_owner] ADD MEMBER [apiwebprofilesystemuser];
GO

-- Tables are created automatically by EF Migrate() on first service start.
-- Optional manual schema: deploy/initial-schema.sql
