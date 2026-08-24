USE [master];
GO

IF DB_ID(N'ProjectDb') IS NULL
BEGIN
	CREATE DATABASE [ProjectDb];
END
GO

USE [ProjectDb];
GO

IF OBJECT_ID(N'dbo.Projects', N'U') IS NULL
BEGIN
	CREATE TABLE [dbo].[Projects]
	(
		[Id] INT IDENTITY(1,1) NOT NULL,
		[Name] NVARCHAR(200) NOT NULL,
		[IsActive] BIT NOT NULL CONSTRAINT [DF_Projects_IsActive] DEFAULT (1),
		[CreatedAt] DATETIME2 NOT NULL,
		[UpdatedAt] DATETIME2 NOT NULL,
		CONSTRAINT [PK_Projects] PRIMARY KEY ([Id])
	);
END
GO

IF COL_LENGTH('dbo.Projects', 'Description') IS NOT NULL
BEGIN
	ALTER TABLE [dbo].[Projects] DROP CONSTRAINT [DF_Projects_Description];
	ALTER TABLE [dbo].[Projects] DROP COLUMN [Description];
END
GO

IF COL_LENGTH('dbo.Projects', 'Status') IS NOT NULL
BEGIN
	ALTER TABLE [dbo].[Projects] DROP COLUMN [Status];
END
GO

IF COL_LENGTH('dbo.Projects', 'IsActive') IS NULL
BEGIN
	ALTER TABLE [dbo].[Projects] ADD [IsActive] BIT NOT NULL CONSTRAINT [DF_Projects_IsActive] DEFAULT (1);
END
GO

IF OBJECT_ID(N'dbo.Tasks', N'U') IS NULL
BEGIN
	CREATE TABLE [dbo].[Tasks]
	(
		[Id] INT IDENTITY(1,1) NOT NULL,
		[ProjectId] INT NOT NULL,
		[Title] NVARCHAR(200) NOT NULL,
		[IsActive] BIT NOT NULL CONSTRAINT [DF_Tasks_IsActive] DEFAULT (1),
		[CreatedAt] DATETIME2 NOT NULL,
		[UpdatedAt] DATETIME2 NOT NULL,
		CONSTRAINT [PK_Tasks] PRIMARY KEY ([Id]),
		CONSTRAINT [FK_Tasks_Projects_ProjectId]
			FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Projects]([Id]) ON DELETE CASCADE
	);
END
GO

IF COL_LENGTH('dbo.Tasks', 'Description') IS NOT NULL
BEGIN
	ALTER TABLE [dbo].[Tasks] DROP CONSTRAINT [DF_Tasks_Description];
	ALTER TABLE [dbo].[Tasks] DROP COLUMN [Description];
END
GO

IF COL_LENGTH('dbo.Tasks', 'Status') IS NOT NULL
BEGIN
	ALTER TABLE [dbo].[Tasks] DROP COLUMN [Status];
END
GO

IF COL_LENGTH('dbo.Tasks', 'Priority') IS NOT NULL
BEGIN
	ALTER TABLE [dbo].[Tasks] DROP COLUMN [Priority];
END
GO

IF COL_LENGTH('dbo.Tasks', 'DueDate') IS NOT NULL
BEGIN
	ALTER TABLE [dbo].[Tasks] DROP COLUMN [DueDate];
END
GO

IF COL_LENGTH('dbo.Tasks', 'IsActive') IS NULL
BEGIN
	ALTER TABLE [dbo].[Tasks] ADD [IsActive] BIT NOT NULL CONSTRAINT [DF_Tasks_IsActive] DEFAULT (1);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Projects_Name' AND object_id = OBJECT_ID(N'dbo.Projects'))
BEGIN
	CREATE INDEX [IX_Projects_Name] ON [dbo].[Projects]([Name]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tasks_ProjectId' AND object_id = OBJECT_ID(N'dbo.Tasks'))
BEGIN
	CREATE INDEX [IX_Tasks_ProjectId] ON [dbo].[Tasks]([ProjectId]);
END
GO

IF OBJECT_ID(N'dbo.TimeEntries', N'U') IS NULL
BEGIN
	CREATE TABLE [dbo].[TimeEntries]
	(
		[Id] INT IDENTITY(1,1) NOT NULL,
		[UserId] INT NOT NULL,
		[ProjectId] INT NOT NULL,
		[TaskId] INT NOT NULL,
		[EntryDate] DATE NOT NULL,
		[Hours] DECIMAL(5,2) NOT NULL,
		[Notes] NVARCHAR(2000) NOT NULL CONSTRAINT [DF_TimeEntries_Notes] DEFAULT (N''),
		[IsActive] BIT NOT NULL CONSTRAINT [DF_TimeEntries_IsActive] DEFAULT (1),
		[CreatedAt] DATETIME2 NOT NULL,
		[UpdatedAt] DATETIME2 NOT NULL,
		CONSTRAINT [PK_TimeEntries] PRIMARY KEY ([Id]),
		CONSTRAINT [FK_TimeEntries_Projects_ProjectId]
			FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Projects]([Id]),
		CONSTRAINT [FK_TimeEntries_Tasks_TaskId]
			FOREIGN KEY ([TaskId]) REFERENCES [dbo].[Tasks]([Id])
	);
END
GO

IF COL_LENGTH('dbo.TimeEntries', 'EntryDate') IS NOT NULL
BEGIN
	DECLARE @EntryDateType NVARCHAR(128);
	SELECT @EntryDateType = TYPE_NAME(c.user_type_id)
	FROM sys.columns c
	WHERE c.object_id = OBJECT_ID(N'dbo.TimeEntries')
	  AND c.name = 'EntryDate';

	IF @EntryDateType <> 'date'
	BEGIN
		IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TimeEntries_EntryDate' AND object_id = OBJECT_ID(N'dbo.TimeEntries'))
		BEGIN
			DROP INDEX [IX_TimeEntries_EntryDate] ON [dbo].[TimeEntries];
		END

		ALTER TABLE [dbo].[TimeEntries] ALTER COLUMN [EntryDate] DATE NOT NULL;
	END
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TimeEntries_UserId' AND object_id = OBJECT_ID(N'dbo.TimeEntries'))
BEGIN
	CREATE INDEX [IX_TimeEntries_UserId] ON [dbo].[TimeEntries]([UserId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TimeEntries_EntryDate' AND object_id = OBJECT_ID(N'dbo.TimeEntries'))
BEGIN
	CREATE INDEX [IX_TimeEntries_EntryDate] ON [dbo].[TimeEntries]([EntryDate]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TimeEntries_ProjectId' AND object_id = OBJECT_ID(N'dbo.TimeEntries'))
BEGIN
	CREATE INDEX [IX_TimeEntries_ProjectId] ON [dbo].[TimeEntries]([ProjectId]);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TimeEntries_TaskId' AND object_id = OBJECT_ID(N'dbo.TimeEntries'))
BEGIN
	CREATE INDEX [IX_TimeEntries_TaskId] ON [dbo].[TimeEntries]([TaskId]);
END
GO
