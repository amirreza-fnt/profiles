IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910070308_InitialCreate'
)
BEGIN
    CREATE TABLE [Profiles] (
        [NationalCode] nvarchar(10) NOT NULL,
        [SsoFirstName] nvarchar(100) NULL,
        [SsoLastName] nvarchar(100) NULL,
        [SsoFatherName] nvarchar(100) NULL,
        [SsoGender] nvarchar(20) NULL,
        [SsoBirthDate] nvarchar(32) NULL,
        [SsoEmail] nvarchar(256) NULL,
        [SsoProvince] nvarchar(100) NULL,
        [SsoCity] nvarchar(100) NULL,
        [SsoPostalCode] nvarchar(20) NULL,
        [SsoSyncedAtUtc] datetime2 NULL,
        [UserBirthDate] nvarchar(32) NULL,
        [AvatarFileId] uniqueidentifier NULL,
        [AvatarShortCode] nvarchar(32) NULL,
        [AvatarUrl] nvarchar(512) NULL,
        [PhotoFileId] uniqueidentifier NULL,
        [PhotoShortCode] nvarchar(32) NULL,
        [PhotoUrl] nvarchar(512) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Profiles] PRIMARY KEY ([NationalCode])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910070308_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] uniqueidentifier NOT NULL,
        [NationalCode] nvarchar(10) NOT NULL,
        [Action] nvarchar(40) NOT NULL,
        [ActorType] nvarchar(20) NOT NULL,
        [ActorId] nvarchar(100) NULL,
        [Description] nvarchar(2000) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_Profiles_NationalCode] FOREIGN KEY ([NationalCode]) REFERENCES [Profiles] ([NationalCode]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910070308_InitialCreate'
)
BEGIN
    CREATE TABLE [Contacts] (
        [Id] uniqueidentifier NOT NULL,
        [NationalCode] nvarchar(10) NOT NULL,
        [Type] nvarchar(20) NOT NULL,
        [Number] nvarchar(20) NOT NULL,
        [IsVerified] bit NOT NULL,
        [VerifiedAtUtc] datetime2 NULL,
        [Source] nvarchar(20) NOT NULL,
        [IsPrimary] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Contacts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Contacts_Profiles_NationalCode] FOREIGN KEY ([NationalCode]) REFERENCES [Profiles] ([NationalCode]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910070308_InitialCreate'
)
BEGIN
    CREATE TABLE [VerificationChallenges] (
        [Id] uniqueidentifier NOT NULL,
        [ContactId] uniqueidentifier NOT NULL,
        [CodeHash] nvarchar(128) NOT NULL,
        [Channel] nvarchar(20) NOT NULL,
        [ExpiresAtUtc] datetime2 NOT NULL,
        [Attempts] int NOT NULL,
        [MaxAttempts] int NOT NULL,
        [ConsumedAtUtc] datetime2 NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_VerificationChallenges] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VerificationChallenges_Contacts_ContactId] FOREIGN KEY ([ContactId]) REFERENCES [Contacts] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910070308_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_NationalCode_CreatedAtUtc] ON [AuditLogs] ([NationalCode], [CreatedAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910070308_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Contacts_NationalCode_Type_Number] ON [Contacts] ([NationalCode], [Type], [Number]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910070308_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Contacts_Number] ON [Contacts] ([Number]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910070308_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_VerificationChallenges_ContactId_ConsumedAtUtc_ExpiresAtUtc] ON [VerificationChallenges] ([ContactId], [ConsumedAtUtc], [ExpiresAtUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910070308_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260910070308_InitialCreate', N'8.0.11');
END;
GO

COMMIT;
GO

