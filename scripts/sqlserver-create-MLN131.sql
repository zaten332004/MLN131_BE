-- Create database MLN131 (SQL Server)
IF DB_ID(N'MLN131') IS NULL
BEGIN
    CREATE DATABASE [MLN131];
END
GO

USE [MLN131];
GO

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

CREATE TABLE [AspNetRoles] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetUsers] (
    [Id] uniqueidentifier NOT NULL,
    [FullName] nvarchar(200) NULL,
    [Age] int NULL,
    [AvatarUrl] nvarchar(500) NULL,
    [IsDisabled] bit NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ChatMessages] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Role] nvarchar(32) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ContentPages] (
    [Id] uniqueidentifier NOT NULL,
    [Slug] nvarchar(128) NOT NULL,
    [Title] nvarchar(256) NOT NULL,
    [BodyMarkdown] nvarchar(max) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    CONSTRAINT [PK_ContentPages] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [UserResponses] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [QuestionKey] nvarchar(128) NOT NULL,
    [AnswerText] nvarchar(max) NOT NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_UserResponses] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [VisitSessions] (
    [Id] uniqueidentifier NOT NULL,
    [VisitorId] nvarchar(450) NOT NULL,
    [UserId] uniqueidentifier NULL,
    [StartedAt] datetimeoffset NOT NULL,
    [LastSeenAt] datetimeoffset NOT NULL,
    [EndedAt] datetimeoffset NULL,
    [IpAddress] nvarchar(64) NULL,
    [UserAgent] nvarchar(512) NULL,
    [PathFirst] nvarchar(256) NULL,
    CONSTRAINT [PK_VisitSessions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserRoles] (
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserTokens] (
    [UserId] uniqueidentifier NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [PageViewEvents] (
    [Id] uniqueidentifier NOT NULL,
    [VisitSessionId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NULL,
    [Path] nvarchar(256) NOT NULL,
    [Referrer] nvarchar(512) NULL,
    [OccurredAt] datetimeoffset NOT NULL,
    CONSTRAINT [PK_PageViewEvents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PageViewEvents_VisitSessions_VisitSessionId] FOREIGN KEY ([VisitSessionId]) REFERENCES [VisitSessions] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO

CREATE INDEX [IX_AspNetUsers_CreatedAt] ON [AspNetUsers] ([CreatedAt]);
GO

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO

CREATE INDEX [IX_ChatMessages_UserId_CreatedAt] ON [ChatMessages] ([UserId], [CreatedAt]);
GO

CREATE UNIQUE INDEX [IX_ContentPages_Slug] ON [ContentPages] ([Slug]);
GO

CREATE INDEX [IX_PageViewEvents_OccurredAt] ON [PageViewEvents] ([OccurredAt]);
GO

CREATE INDEX [IX_PageViewEvents_VisitSessionId] ON [PageViewEvents] ([VisitSessionId]);
GO

CREATE INDEX [IX_UserResponses_UserId_CreatedAt] ON [UserResponses] ([UserId], [CreatedAt]);
GO

CREATE INDEX [IX_VisitSessions_LastSeenAt] ON [VisitSessions] ([LastSeenAt]);
GO

CREATE INDEX [IX_VisitSessions_StartedAt] ON [VisitSessions] ([StartedAt]);
GO

CREATE INDEX [IX_VisitSessions_VisitorId] ON [VisitSessions] ([VisitorId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260306075553_InitialCreate', N'8.0.23');
GO

COMMIT;
GO

