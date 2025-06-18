-- =====================================================================
-- SCRUM-84: Affiliate/Referral System Database Schema
-- Description: Complete SQL Server script for affiliate/referral system
-- Version: 1.0
-- Created: December 1, 2024
-- =====================================================================

USE [QuantumBandsAI]
GO

PRINT 'Starting SCRUM-84 - Affiliate/Referral System Implementation...'
GO

-- =====================================================================
-- Section 1: Modify Users Table to Support Affiliate Tracking
-- =====================================================================

PRINT 'Step 1: Adding affiliate columns to Users table...'
GO

-- Check if columns exist before adding them
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'ReferralCode')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [ReferralCode] NVARCHAR(50) NULL;
    PRINT '  ✓ Added ReferralCode column'
END
ELSE 
BEGIN
    PRINT '  ⚠ ReferralCode column already exists'
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'ReferredByUserId')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [ReferredByUserId] INT NULL;
    PRINT '  ✓ Added ReferredByUserId column'
END
ELSE 
BEGIN
    PRINT '  ⚠ ReferredByUserId column already exists'
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'ReferralDate')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [ReferralDate] DATETIME2 NULL;
    PRINT '  ✓ Added ReferralDate column'
END
ELSE 
BEGIN
    PRINT '  ⚠ ReferralDate column already exists'
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'AffiliateLevel')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [AffiliateLevel] INT NOT NULL DEFAULT 1;
    PRINT '  ✓ Added AffiliateLevel column'
END
ELSE 
BEGIN
    PRINT '  ⚠ AffiliateLevel column already exists'
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'IsAffiliateActive')
BEGIN
    ALTER TABLE [dbo].[Users] ADD [IsAffiliateActive] BIT NOT NULL DEFAULT 1;
    PRINT '  ✓ Added IsAffiliateActive column'
END
ELSE 
BEGIN
    PRINT '  ⚠ IsAffiliateActive column already exists'
END

-- Add unique constraint for ReferralCode if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Users_ReferralCode')
BEGIN
    ALTER TABLE [dbo].[Users] ADD CONSTRAINT [UQ_Users_ReferralCode] UNIQUE NONCLUSTERED ([ReferralCode]);
    PRINT '  ✓ Added unique constraint for ReferralCode'
END
ELSE 
BEGIN
    PRINT '  ⚠ Unique constraint for ReferralCode already exists'
END

-- Add foreign key constraint for ReferredByUserId if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Users_ReferredByUserId')
BEGIN
    ALTER TABLE [dbo].[Users] ADD CONSTRAINT [FK_Users_ReferredByUserId] 
        FOREIGN KEY ([ReferredByUserId]) REFERENCES [dbo].[Users]([UserID]);
    PRINT '  ✓ Added foreign key constraint for ReferredByUserId'
END
ELSE 
BEGIN
    PRINT '  ⚠ Foreign key constraint for ReferredByUserId already exists'
END

-- Add indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_ReferralCode')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Users_ReferralCode] ON [dbo].[Users] ([ReferralCode]);
    PRINT '  ✓ Created index IX_Users_ReferralCode'
END
ELSE 
BEGIN
    PRINT '  ⚠ Index IX_Users_ReferralCode already exists'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_ReferredByUserId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Users_ReferredByUserId] ON [dbo].[Users] ([ReferredByUserId]);
    PRINT '  ✓ Created index IX_Users_ReferredByUserId'
END
ELSE 
BEGIN
    PRINT '  ⚠ Index IX_Users_ReferredByUserId already exists'
END

-- =====================================================================
-- Section 2: Create AffiliateCommissionTiers Table
-- =====================================================================

PRINT 'Step 2: Creating AffiliateCommissionTiers table...'
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AffiliateCommissionTiers')
BEGIN
    CREATE TABLE [dbo].[AffiliateCommissionTiers] (
        [TierId] INT IDENTITY(1,1) PRIMARY KEY,
        [TierName] NVARCHAR(100) NOT NULL,
        [AffiliateLevel] INT NOT NULL,
        [CommissionRate] DECIMAL(5,4) NOT NULL, -- e.g., 0.0500 for 5%
        [MinimumReferrals] INT NOT NULL DEFAULT 0,
        [MinimumVolume] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        [Description] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [UQ_AffiliateCommissionTiers_TierName] UNIQUE ([TierName]),
        CONSTRAINT [UQ_AffiliateCommissionTiers_AffiliateLevel] UNIQUE ([AffiliateLevel]),
        CONSTRAINT [CK_AffiliateCommissionTiers_CommissionRate] CHECK ([CommissionRate] >= 0 AND [CommissionRate] <= 1),
        CONSTRAINT [CK_AffiliateCommissionTiers_MinimumReferrals] CHECK ([MinimumReferrals] >= 0),
        CONSTRAINT [CK_AffiliateCommissionTiers_MinimumVolume] CHECK ([MinimumVolume] >= 0)
    );
    
    -- Insert default tier data
    INSERT INTO [dbo].[AffiliateCommissionTiers] ([TierName], [AffiliateLevel], [CommissionRate], [MinimumReferrals], [MinimumVolume], [Description]) VALUES
    ('Bronze', 1, 0.0100, 0, 0, 'Entry level - 1% commission on direct referrals'),
    ('Silver', 2, 0.0200, 10, 1000, 'Silver level - 2% commission, requires 10 referrals and $1000 volume'),
    ('Gold', 3, 0.0350, 25, 5000, 'Gold level - 3.5% commission, requires 25 referrals and $5000 volume'),
    ('Platinum', 4, 0.0500, 50, 10000, 'Platinum level - 5% commission, requires 50 referrals and $10000 volume');
    
    PRINT '  ✓ Created AffiliateCommissionTiers table with default data'
END
ELSE 
BEGIN
    PRINT '  ⚠ AffiliateCommissionTiers table already exists'
END

-- =====================================================================
-- Section 3: Create AffiliateCommissionStructure Table
-- =====================================================================

PRINT 'Step 3: Creating AffiliateCommissionStructure table...'
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AffiliateCommissionStructure')
BEGIN
    CREATE TABLE [dbo].[AffiliateCommissionStructure] (
        [StructureId] INT IDENTITY(1,1) PRIMARY KEY,
        [Level] INT NOT NULL, -- 1 = direct referral, 2 = second level, etc.
        [CommissionPercentage] DECIMAL(5,4) NOT NULL,
        [MaxLevel] INT NOT NULL DEFAULT 5,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [UQ_AffiliateCommissionStructure_Level] UNIQUE ([Level]),
        CONSTRAINT [CK_AffiliateCommissionStructure_Level] CHECK ([Level] > 0),
        CONSTRAINT [CK_AffiliateCommissionStructure_CommissionPercentage] CHECK ([CommissionPercentage] >= 0 AND [CommissionPercentage] <= 1),
        CONSTRAINT [CK_AffiliateCommissionStructure_MaxLevel] CHECK ([MaxLevel] > 0)
    );
    
    -- Insert default commission structure
    INSERT INTO [dbo].[AffiliateCommissionStructure] ([Level], [CommissionPercentage]) VALUES
    (1, 0.5000), -- 50% to direct referrer
    (2, 0.2000), -- 20% to second level
    (3, 0.1500), -- 15% to third level
    (4, 0.1000), -- 10% to fourth level
    (5, 0.0500); -- 5% to fifth level
    
    PRINT '  ✓ Created AffiliateCommissionStructure table with default data'
END
ELSE 
BEGIN
    PRINT '  ⚠ AffiliateCommissionStructure table already exists'
END

-- =====================================================================
-- Section 4: Create AffiliatePerformance Table
-- =====================================================================

PRINT 'Step 4: Creating AffiliatePerformance table...'
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AffiliatePerformance')
BEGIN
    CREATE TABLE [dbo].[AffiliatePerformance] (
        [PerformanceId] INT IDENTITY(1,1) PRIMARY KEY,
        [UserId] INT NOT NULL,
        [PeriodStartDate] DATE NOT NULL,
        [PeriodEndDate] DATE NOT NULL,
        [DirectReferrals] INT NOT NULL DEFAULT 0,
        [TotalReferrals] INT NOT NULL DEFAULT 0, -- Including sub-levels
        [TotalTradingVolume] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        [TotalCommissionEarned] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
        [ActiveReferrals] INT NOT NULL DEFAULT 0,
        [CurrentTierId] INT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [FK_AffiliatePerformance_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserID]),
        CONSTRAINT [FK_AffiliatePerformance_CurrentTierId] FOREIGN KEY ([CurrentTierId]) REFERENCES [dbo].[AffiliateCommissionTiers]([TierId]),
        CONSTRAINT [UQ_AffiliatePerformance_User_Period] UNIQUE ([UserId], [PeriodStartDate], [PeriodEndDate]),
        CONSTRAINT [CK_AffiliatePerformance_PeriodDates] CHECK ([PeriodEndDate] >= [PeriodStartDate]),
        CONSTRAINT [CK_AffiliatePerformance_DirectReferrals] CHECK ([DirectReferrals] >= 0),
        CONSTRAINT [CK_AffiliatePerformance_TotalReferrals] CHECK ([TotalReferrals] >= [DirectReferrals]),
        CONSTRAINT [CK_AffiliatePerformance_TotalTradingVolume] CHECK ([TotalTradingVolume] >= 0),
        CONSTRAINT [CK_AffiliatePerformance_TotalCommissionEarned] CHECK ([TotalCommissionEarned] >= 0),
        CONSTRAINT [CK_AffiliatePerformance_ActiveReferrals] CHECK ([ActiveReferrals] >= 0)
    );
    
    -- Create indexes for performance
    CREATE NONCLUSTERED INDEX [IX_AffiliatePerformance_UserId] ON [dbo].[AffiliatePerformance] ([UserId]);
    CREATE NONCLUSTERED INDEX [IX_AffiliatePerformance_Period] ON [dbo].[AffiliatePerformance] ([PeriodStartDate], [PeriodEndDate]);
    CREATE NONCLUSTERED INDEX [IX_AffiliatePerformance_CurrentTierId] ON [dbo].[AffiliatePerformance] ([CurrentTierId]);
    
    PRINT '  ✓ Created AffiliatePerformance table with indexes'
END
ELSE 
BEGIN
    PRINT '  ⚠ AffiliatePerformance table already exists'
END

-- =====================================================================
-- Section 5: Create AffiliateCommissionLog Table
-- =====================================================================

PRINT 'Step 5: Creating AffiliateCommissionLog table...'
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AffiliateCommissionLog')
BEGIN
    CREATE TABLE [dbo].[AffiliateCommissionLog] (
        [CommissionLogId] INT IDENTITY(1,1) PRIMARY KEY,
        [AffiliateUserId] INT NOT NULL, -- Who receives the commission
        [ReferredUserId] INT NOT NULL,  -- Who generated the activity
        [SourceWalletTransactionId] BIGINT NULL, -- Original transaction that generated commission
        [CommissionLevel] INT NOT NULL, -- 1 = direct, 2 = second level, etc.
        [OriginalAmount] DECIMAL(18,2) NOT NULL,
        [CommissionRate] DECIMAL(5,4) NOT NULL,
        [CommissionAmount] DECIMAL(18,2) NOT NULL,
        [CommissionType] NVARCHAR(50) NOT NULL, -- 'TRADING_FEE', 'DEPOSIT', 'WITHDRAWAL', etc.
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'PENDING', -- PENDING, PAID, CANCELLED
        [WalletTransactionId] BIGINT NULL, -- Commission payment transaction
        [ProcessedAt] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [Description] NVARCHAR(500) NULL,
        
        CONSTRAINT [FK_AffiliateCommissionLog_AffiliateUserId] FOREIGN KEY ([AffiliateUserId]) REFERENCES [dbo].[Users]([UserID]),
        CONSTRAINT [FK_AffiliateCommissionLog_ReferredUserId] FOREIGN KEY ([ReferredUserId]) REFERENCES [dbo].[Users]([UserID]),
        CONSTRAINT [FK_AffiliateCommissionLog_SourceWalletTransactionId] FOREIGN KEY ([SourceWalletTransactionId]) REFERENCES [dbo].[WalletTransactions]([TransactionID]),
        CONSTRAINT [FK_AffiliateCommissionLog_WalletTransactionId] FOREIGN KEY ([WalletTransactionId]) REFERENCES [dbo].[WalletTransactions]([TransactionID]),
        CONSTRAINT [CK_AffiliateCommissionLog_CommissionLevel] CHECK ([CommissionLevel] > 0),
        CONSTRAINT [CK_AffiliateCommissionLog_OriginalAmount] CHECK ([OriginalAmount] > 0),
        CONSTRAINT [CK_AffiliateCommissionLog_CommissionRate] CHECK ([CommissionRate] >= 0 AND [CommissionRate] <= 1),
        CONSTRAINT [CK_AffiliateCommissionLog_CommissionAmount] CHECK ([CommissionAmount] >= 0),
        CONSTRAINT [CK_AffiliateCommissionLog_Status] CHECK ([Status] IN ('PENDING', 'PAID', 'CANCELLED'))
    );
    
    -- Create indexes for performance
    CREATE NONCLUSTERED INDEX [IX_AffiliateCommissionLog_AffiliateUserId] ON [dbo].[AffiliateCommissionLog] ([AffiliateUserId]);
    CREATE NONCLUSTERED INDEX [IX_AffiliateCommissionLog_ReferredUserId] ON [dbo].[AffiliateCommissionLog] ([ReferredUserId]);
    CREATE NONCLUSTERED INDEX [IX_AffiliateCommissionLog_Status] ON [dbo].[AffiliateCommissionLog] ([Status]);
    CREATE NONCLUSTERED INDEX [IX_AffiliateCommissionLog_CreatedAt] ON [dbo].[AffiliateCommissionLog] ([CreatedAt]);
    CREATE NONCLUSTERED INDEX [IX_AffiliateCommissionLog_CommissionType] ON [dbo].[AffiliateCommissionLog] ([CommissionType]);
    
    PRINT '  ✓ Created AffiliateCommissionLog table with indexes'
END
ELSE 
BEGIN
    PRINT '  ⚠ AffiliateCommissionLog table already exists'
END

-- =====================================================================
-- Section 6: Create AffiliateHierarchy Table (Materialized Path)
-- =====================================================================

PRINT 'Step 6: Creating AffiliateHierarchy table...'
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AffiliateHierarchy')
BEGIN
    CREATE TABLE [dbo].[AffiliateHierarchy] (
        [HierarchyId] INT IDENTITY(1,1) PRIMARY KEY,
        [ChildUserId] INT NOT NULL,
        [ParentUserId] INT NOT NULL,
        [Level] INT NOT NULL, -- 1 = direct child, 2 = grandchild, etc.
        [HierarchyPath] NVARCHAR(1000) NOT NULL, -- e.g., "/1/15/23/45" for user path
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [FK_AffiliateHierarchy_ChildUserId] FOREIGN KEY ([ChildUserId]) REFERENCES [dbo].[Users]([UserID]),
        CONSTRAINT [FK_AffiliateHierarchy_ParentUserId] FOREIGN KEY ([ParentUserId]) REFERENCES [dbo].[Users]([UserID]),
        CONSTRAINT [UQ_AffiliateHierarchy_Child_Parent] UNIQUE ([ChildUserId], [ParentUserId]),
        CONSTRAINT [CK_AffiliateHierarchy_Level] CHECK ([Level] > 0),
        CONSTRAINT [CK_AffiliateHierarchy_DifferentUsers] CHECK ([ChildUserId] != [ParentUserId])
    );
    
    -- Create indexes for performance
    CREATE NONCLUSTERED INDEX [IX_AffiliateHierarchy_ChildUserId] ON [dbo].[AffiliateHierarchy] ([ChildUserId]);
    CREATE NONCLUSTERED INDEX [IX_AffiliateHierarchy_ParentUserId] ON [dbo].[AffiliateHierarchy] ([ParentUserId]);
    CREATE NONCLUSTERED INDEX [IX_AffiliateHierarchy_Level] ON [dbo].[AffiliateHierarchy] ([Level]);
    CREATE NONCLUSTERED INDEX [IX_AffiliateHierarchy_HierarchyPath] ON [dbo].[AffiliateHierarchy] ([HierarchyPath]);
    
    PRINT '  ✓ Created AffiliateHierarchy table with indexes'
END
ELSE 
BEGIN
    PRINT '  ⚠ AffiliateHierarchy table already exists'
END

-- =====================================================================
-- Section 7: Data Migration for Existing Users
-- =====================================================================

PRINT 'Step 7: Running data migration for existing users...'
GO

-- Generate referral codes for existing users who don't have them
UPDATE [dbo].[Users] 
SET [ReferralCode] = UPPER([Username] + '_' + CAST([UserID] AS NVARCHAR(10)))
WHERE [ReferralCode] IS NULL AND [Username] IS NOT NULL;

DECLARE @UpdatedUsersCount INT = @@ROWCOUNT;
PRINT '  ✓ Generated referral codes for ' + CAST(@UpdatedUsersCount AS NVARCHAR(10)) + ' existing users'

-- Create initial performance records for existing active users
INSERT INTO [dbo].[AffiliatePerformance] ([UserId], [PeriodStartDate], [PeriodEndDate], [CurrentTierId])
SELECT 
    [UserID], 
    CAST(GETDATE() AS DATE) AS [PeriodStartDate], 
    DATEADD(MONTH, 1, CAST(GETDATE() AS DATE)) AS [PeriodEndDate], 
    1 AS [CurrentTierId] -- Bronze tier by default
FROM [dbo].[Users]
WHERE [IsActive] = 1 
    AND [UserID] NOT IN (SELECT DISTINCT [UserId] FROM [dbo].[AffiliatePerformance]);

DECLARE @InitialPerformanceCount INT = @@ROWCOUNT;
PRINT '  ✓ Created initial performance records for ' + CAST(@InitialPerformanceCount AS NVARCHAR(10)) + ' active users'

-- =====================================================================
-- Section 8: Create Views for Common Queries
-- =====================================================================

PRINT 'Step 8: Creating useful views...'
GO

-- View for affiliate statistics
IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'vw_AffiliateStatistics')
BEGIN
    EXEC('
    CREATE VIEW [dbo].[vw_AffiliateStatistics] AS
    SELECT 
        u.[UserID],
        u.[Username],
        u.[Email],
        u.[ReferralCode],
        u.[AffiliateLevel],
        u.[IsAffiliateActive],
        t.[TierName],
        t.[CommissionRate],
        COUNT(r.[UserID]) AS [DirectReferralsCount],
        COALESCE(ap.[TotalCommissionEarned], 0) AS [TotalCommissionEarned],
        COALESCE(ap.[TotalTradingVolume], 0) AS [TotalTradingVolume],
        COALESCE(ap.[ActiveReferrals], 0) AS [ActiveReferrals]
    FROM [dbo].[Users] u
    LEFT JOIN [dbo].[AffiliateCommissionTiers] t ON u.[AffiliateLevel] = t.[AffiliateLevel]
    LEFT JOIN [dbo].[Users] r ON u.[UserID] = r.[ReferredByUserId]
    LEFT JOIN [dbo].[AffiliatePerformance] ap ON u.[UserID] = ap.[UserId] 
        AND ap.[PeriodStartDate] <= GETDATE() 
        AND ap.[PeriodEndDate] >= GETDATE()
    WHERE u.[ReferralCode] IS NOT NULL
    GROUP BY u.[UserID], u.[Username], u.[Email], u.[ReferralCode], u.[AffiliateLevel], 
             u.[IsAffiliateActive], t.[TierName], t.[CommissionRate], ap.[TotalCommissionEarned], 
             ap.[TotalTradingVolume], ap.[ActiveReferrals]
    ');
    PRINT '  ✓ Created vw_AffiliateStatistics view'
END
ELSE 
BEGIN
    PRINT '  ⚠ vw_AffiliateStatistics view already exists'
END

-- View for commission summary
IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'vw_CommissionSummary')
BEGIN
    EXEC('
    CREATE VIEW [dbo].[vw_CommissionSummary] AS
    SELECT 
        acl.[AffiliateUserId],
        u.[Username] AS [AffiliateUsername],
        COUNT(*) AS [TotalCommissions],
        SUM(acl.[CommissionAmount]) AS [TotalCommissionAmount],
        SUM(CASE WHEN acl.[Status] = ''PAID'' THEN acl.[CommissionAmount] ELSE 0 END) AS [PaidCommissionAmount],
        SUM(CASE WHEN acl.[Status] = ''PENDING'' THEN acl.[CommissionAmount] ELSE 0 END) AS [PendingCommissionAmount],
        MIN(acl.[CreatedAt]) AS [FirstCommissionDate],
        MAX(acl.[CreatedAt]) AS [LastCommissionDate]
    FROM [dbo].[AffiliateCommissionLog] acl
    INNER JOIN [dbo].[Users] u ON acl.[AffiliateUserId] = u.[UserID]
    GROUP BY acl.[AffiliateUserId], u.[Username]
    ');
    PRINT '  ✓ Created vw_CommissionSummary view'
END
ELSE 
BEGIN
    PRINT '  ⚠ vw_CommissionSummary view already exists'
END

-- =====================================================================
-- Section 9: Create Stored Procedures for Common Operations
-- =====================================================================

PRINT 'Step 9: Creating stored procedures...'
GO

-- Procedure to calculate and distribute commissions
IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_CalculateAffiliateCommission')
BEGIN
    EXEC('
    CREATE PROCEDURE [dbo].[sp_CalculateAffiliateCommission]
        @SourceTransactionId BIGINT,
        @ReferredUserId INT,
        @CommissionAmount DECIMAL(18,2),
        @CommissionType NVARCHAR(50)
    AS
    BEGIN
        SET NOCOUNT ON;
        
        DECLARE @CurrentUserId INT = @ReferredUserId;
        DECLARE @Level INT = 1;
        DECLARE @MaxLevel INT = 5;
        DECLARE @RemainingCommission DECIMAL(18,2) = @CommissionAmount;
        
        -- Get the referrer chain and calculate commissions
        WHILE @CurrentUserId IS NOT NULL AND @Level <= @MaxLevel AND @RemainingCommission > 0
        BEGIN
            -- Get the referrer of current user
            SELECT @CurrentUserId = [ReferredByUserId] 
            FROM [dbo].[Users] 
            WHERE [UserID] = @CurrentUserId AND [IsAffiliateActive] = 1;
            
            IF @CurrentUserId IS NOT NULL
            BEGIN
                -- Get commission percentage for this level
                DECLARE @CommissionPercentage DECIMAL(5,4);
                SELECT @CommissionPercentage = [CommissionPercentage]
                FROM [dbo].[AffiliateCommissionStructure]
                WHERE [Level] = @Level AND [IsActive] = 1;
                
                IF @CommissionPercentage IS NOT NULL AND @CommissionPercentage > 0
                BEGIN
                    -- Calculate commission for this level
                    DECLARE @LevelCommission DECIMAL(18,2) = @RemainingCommission * @CommissionPercentage;
                    
                    -- Get affiliate tier commission rate
                    DECLARE @TierCommissionRate DECIMAL(5,4);
                    SELECT @TierCommissionRate = t.[CommissionRate]
                    FROM [dbo].[Users] u
                    INNER JOIN [dbo].[AffiliateCommissionTiers] t ON u.[AffiliateLevel] = t.[AffiliateLevel]
                    WHERE u.[UserID] = @CurrentUserId;
                    
                    -- Apply tier multiplier
                    SET @LevelCommission = @LevelCommission * ISNULL(@TierCommissionRate, 0.01);
                    
                    -- Record the commission
                    INSERT INTO [dbo].[AffiliateCommissionLog] 
                    ([AffiliateUserId], [ReferredUserId], [SourceWalletTransactionId], [CommissionLevel], 
                     [OriginalAmount], [CommissionRate], [CommissionAmount], [CommissionType], [Status], [Description])
                    VALUES 
                    (@CurrentUserId, @ReferredUserId, @SourceTransactionId, @Level, 
                     @CommissionAmount, @CommissionPercentage * ISNULL(@TierCommissionRate, 0.01), @LevelCommission, @CommissionType, ''PENDING'', 
                     ''Level '' + CAST(@Level AS NVARCHAR(10)) + '' commission for '' + @CommissionType);
                     
                    SET @RemainingCommission = @RemainingCommission - (@RemainingCommission * @CommissionPercentage);
                END
                
                SET @Level = @Level + 1;
            END
        END
    END
    ');
    PRINT '  ✓ Created sp_CalculateAffiliateCommission stored procedure'
END
ELSE 
BEGIN
    PRINT '  ⚠ sp_CalculateAffiliateCommission stored procedure already exists'
END

-- Procedure to update affiliate hierarchy
IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_UpdateAffiliateHierarchy')
BEGIN
    EXEC('
    CREATE PROCEDURE [dbo].[sp_UpdateAffiliateHierarchy]
        @ChildUserId INT,
        @ParentUserId INT
    AS
    BEGIN
        SET NOCOUNT ON;
        
        -- Remove existing hierarchy for this child
        DELETE FROM [dbo].[AffiliateHierarchy] WHERE [ChildUserId] = @ChildUserId;
        
        -- Build new hierarchy path
        DECLARE @CurrentParent INT = @ParentUserId;
        DECLARE @Level INT = 1;
        DECLARE @MaxLevel INT = 5;
        DECLARE @HierarchyPath NVARCHAR(1000) = '''';
        
        -- Build the hierarchy path by walking up the parent chain
        WHILE @CurrentParent IS NOT NULL AND @Level <= @MaxLevel
        BEGIN
            SET @HierarchyPath = ''/'' + CAST(@CurrentParent AS NVARCHAR(10)) + @HierarchyPath;
            
            -- Insert hierarchy record
            INSERT INTO [dbo].[AffiliateHierarchy] ([ChildUserId], [ParentUserId], [Level], [HierarchyPath])
            VALUES (@ChildUserId, @CurrentParent, @Level, @HierarchyPath);
            
            -- Get the next parent up the chain
            SELECT @CurrentParent = [ReferredByUserId] FROM [dbo].[Users] WHERE [UserID] = @CurrentParent;
            SET @Level = @Level + 1;
        END
    END
    ');
    PRINT '  ✓ Created sp_UpdateAffiliateHierarchy stored procedure'
END
ELSE 
BEGIN
    PRINT '  ⚠ sp_UpdateAffiliateHierarchy stored procedure already exists'
END

-- =====================================================================
-- Section 10: Create Triggers for Automatic Updates
-- =====================================================================

PRINT 'Step 10: Creating triggers...'
GO

-- Trigger to automatically update affiliate hierarchy when user referral changes
IF NOT EXISTS (SELECT * FROM sys.triggers WHERE name = 'tr_Users_AffiliateHierarchy_Update')
BEGIN
    EXEC('
    CREATE TRIGGER [dbo].[tr_Users_AffiliateHierarchy_Update]
    ON [dbo].[Users]
    AFTER UPDATE
    AS
    BEGIN
        SET NOCOUNT ON;
        
        -- Check if ReferredByUserId was updated
        IF UPDATE([ReferredByUserId])
        BEGIN
            DECLARE @ChildUserId INT, @ParentUserId INT;
            
            DECLARE user_cursor CURSOR FOR
            SELECT [UserID], [ReferredByUserId]
            FROM inserted
            WHERE [ReferredByUserId] IS NOT NULL;
            
            OPEN user_cursor;
            FETCH NEXT FROM user_cursor INTO @ChildUserId, @ParentUserId;
            
            WHILE @@FETCH_STATUS = 0
            BEGIN
                -- Update affiliate hierarchy
                EXEC [dbo].[sp_UpdateAffiliateHierarchy] @ChildUserId, @ParentUserId;
                
                FETCH NEXT FROM user_cursor INTO @ChildUserId, @ParentUserId;
            END
            
            CLOSE user_cursor;
            DEALLOCATE user_cursor;
        END
    END
    ');
    PRINT '  ✓ Created tr_Users_AffiliateHierarchy_Update trigger'
END
ELSE 
BEGIN
    PRINT '  ⚠ tr_Users_AffiliateHierarchy_Update trigger already exists'
END

-- =====================================================================
-- Section 11: Grant Permissions
-- =====================================================================

PRINT 'Step 11: Setting up permissions...'
GO

-- Grant permissions to application roles (assuming standard roles exist)
IF EXISTS (SELECT * FROM sys.database_principals WHERE name = 'db_application')
BEGIN
    GRANT SELECT, INSERT, UPDATE ON [dbo].[AffiliateCommissionTiers] TO [db_application];
    GRANT SELECT, INSERT, UPDATE ON [dbo].[AffiliateCommissionStructure] TO [db_application];
    GRANT SELECT, INSERT, UPDATE ON [dbo].[AffiliatePerformance] TO [db_application];
    GRANT SELECT, INSERT, UPDATE ON [dbo].[AffiliateCommissionLog] TO [db_application];
    GRANT SELECT, INSERT, UPDATE ON [dbo].[AffiliateHierarchy] TO [db_application];
    GRANT SELECT ON [dbo].[vw_AffiliateStatistics] TO [db_application];
    GRANT SELECT ON [dbo].[vw_CommissionSummary] TO [db_application];
    GRANT EXECUTE ON [dbo].[sp_CalculateAffiliateCommission] TO [db_application];
    GRANT EXECUTE ON [dbo].[sp_UpdateAffiliateHierarchy] TO [db_application];
    
    PRINT '  ✓ Granted permissions to db_application role'
END
ELSE 
BEGIN
    PRINT '  ⚠ db_application role not found, skipping permission grants'
END

-- =====================================================================
-- Final Summary
-- =====================================================================

PRINT ''
PRINT '=========================================='
PRINT 'SCRUM-84 Implementation Complete!'
PRINT '=========================================='
PRINT ''
PRINT 'Summary of changes:'
PRINT '✓ Modified Users table with affiliate tracking columns'
PRINT '✓ Created AffiliateCommissionTiers table with 4 default tiers'
PRINT '✓ Created AffiliateCommissionStructure table with 5-level structure'
PRINT '✓ Created AffiliatePerformance table for tracking metrics'
PRINT '✓ Created AffiliateCommissionLog table for commission tracking'
PRINT '✓ Created AffiliateHierarchy table for efficient hierarchy queries'
PRINT '✓ Generated referral codes for existing users'
PRINT '✓ Created initial performance records'
PRINT '✓ Created useful views for reporting'
PRINT '✓ Created stored procedures for commission calculation'
PRINT '✓ Created triggers for automatic hierarchy updates'
PRINT '✓ Set up appropriate permissions'
PRINT ''
PRINT 'Next steps:'
PRINT '1. Update Entity Framework models'
PRINT '2. Implement application layer services'
PRINT '3. Create API endpoints for affiliate management'
PRINT '4. Add affiliate dashboard UI components'
PRINT ''
PRINT 'Database schema is ready for affiliate/referral system!'
GO