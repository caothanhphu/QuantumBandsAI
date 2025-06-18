-- =============================================
-- SCRUM-85: Create Affiliate Hierarchy Management Triggers and Functions
-- Description: Implement database triggers and stored procedures to automatically 
--              manage affiliate hierarchy and commission calculations
-- Author: System
-- Date: 2024-06-15
-- Database: MSSQL Server
-- Dependencies: SCRUM-84 must be executed first
-- =============================================

USE [QuantumBandsAI]
GO

-- =============================================
-- 1. Create Hierarchy Management Function
-- =============================================
IF OBJECT_ID('dbo.BuildAffiliateHierarchy', 'TF') IS NOT NULL
    DROP FUNCTION dbo.BuildAffiliateHierarchy;
GO

CREATE FUNCTION dbo.BuildAffiliateHierarchy(
    @NewUserId INT,
    @ReferrerUserId INT
)
RETURNS @HierarchyTable TABLE (
    ChildUserId INT,
    ParentUserId INT,
    Level INT,
    HierarchyPath NVARCHAR(1000)
)
AS
BEGIN
    -- Build complete hierarchy path for new user
    DECLARE @MaxLevel INT = 5;
    DECLARE @CurrentLevel INT = 1;
    DECLARE @CurrentParent INT = @ReferrerUserId;
    DECLARE @HierarchyPath NVARCHAR(1000) = '/' + CAST(@NewUserId AS NVARCHAR(10));
    
    -- Insert direct parent relationship
    INSERT INTO @HierarchyTable VALUES (@NewUserId, @CurrentParent, @CurrentLevel, @HierarchyPath);
    
    -- Build upward hierarchy
    WHILE @CurrentLevel < @MaxLevel AND @CurrentParent IS NOT NULL
    BEGIN
        SET @CurrentLevel = @CurrentLevel + 1;
        
        SELECT @CurrentParent = ReferredByUserId
        FROM Users 
        WHERE UserId = @CurrentParent;
        
        IF @CurrentParent IS NOT NULL
        BEGIN
            SET @HierarchyPath = '/' + CAST(@CurrentParent AS NVARCHAR(10)) + @HierarchyPath;
            INSERT INTO @HierarchyTable VALUES (@NewUserId, @CurrentParent, @CurrentLevel, @HierarchyPath);
        END
    END
    
    RETURN;
END
GO

-- =============================================
-- 2. Create Commission Calculation Function
-- =============================================
IF OBJECT_ID('dbo.CalculateAffiliateCommission', 'FN') IS NOT NULL
    DROP FUNCTION dbo.CalculateAffiliateCommission;
GO

CREATE FUNCTION dbo.CalculateAffiliateCommission(
    @OriginalAmount DECIMAL(18,2),
    @CommissionType NVARCHAR(50),
    @AffiliateLevel INT,
    @HierarchyLevel INT
)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @BaseCommissionRate DECIMAL(5,4);
    DECLARE @LevelCommissionRate DECIMAL(5,4);
    DECLARE @TierCommissionRate DECIMAL(5,4);
    DECLARE @FinalCommissionAmount DECIMAL(18,2);
    
    -- Get tier commission rate
    SELECT @TierCommissionRate = CommissionRate 
    FROM AffiliateCommissionTiers 
    WHERE AffiliateLevel = @AffiliateLevel AND IsActive = 1;
    
    -- Get level distribution percentage
    SELECT @LevelCommissionRate = CommissionPercentage 
    FROM AffiliateCommissionStructure 
    WHERE Level = @HierarchyLevel AND IsActive = 1;
    
    -- Base commission rate by transaction type
    SET @BaseCommissionRate = CASE @CommissionType
        WHEN 'TRADING_FEE' THEN 1.0000  -- 100% of trading fees eligible
        WHEN 'DEPOSIT_FEE' THEN 0.5000  -- 50% of deposit fees eligible
        WHEN 'WITHDRAWAL_FEE' THEN 0.5000  -- 50% of withdrawal fees eligible
        ELSE 0.0000
    END;
    
    -- Calculate final commission
    SET @FinalCommissionAmount = @OriginalAmount * @BaseCommissionRate * ISNULL(@TierCommissionRate, 0) * ISNULL(@LevelCommissionRate, 0);
    
    RETURN ISNULL(@FinalCommissionAmount, 0.00);
END
GO

-- =============================================
-- 3. Create Affiliate Tier Upgrade Check Procedure (Create this first)
-- =============================================
IF OBJECT_ID('dbo.CheckAffiliateHierarchyUpgrade', 'P') IS NOT NULL
    DROP PROCEDURE dbo.CheckAffiliateHierarchyUpgrade;
GO

CREATE PROCEDURE dbo.CheckAffiliateHierarchyUpgrade
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CurrentTierId INT;
    DECLARE @NewTierId INT;
    DECLARE @DirectReferrals INT;
    DECLARE @TotalCommission DECIMAL(18,2);
    
    -- Get current performance metrics
    SELECT 
        @CurrentTierId = CurrentTierId,
        @DirectReferrals = DirectReferrals,
        @TotalCommission = TotalCommissionEarned
    FROM AffiliatePerformance 
    WHERE UserId = @UserId 
      AND PeriodStartDate = CAST(GETDATE() AS DATE);
    
    -- Determine new tier based on performance
    SELECT TOP 1 @NewTierId = TierId
    FROM AffiliateCommissionTiers
    WHERE MinimumReferrals <= ISNULL(@DirectReferrals, 0)
      AND MinimumVolume <= ISNULL(@TotalCommission, 0)
      AND IsActive = 1
    ORDER BY TierId DESC;
    
    -- Update tier if changed
    IF @NewTierId > ISNULL(@CurrentTierId, 0)
    BEGIN
        UPDATE AffiliatePerformance
        SET CurrentTierId = @NewTierId,
            UpdatedAt = GETUTCDATE()
        WHERE UserId = @UserId 
          AND PeriodStartDate = CAST(GETDATE() AS DATE);
        
        UPDATE Users
        SET AffiliateLevel = @NewTierId,
            UpdatedAt = GETUTCDATE()
        WHERE UserID = @UserId;
    END
END
GO

-- =============================================
-- 4. Create Performance Update Stored Procedure
-- =============================================
IF OBJECT_ID('dbo.UpdateAffiliatePerformance', 'P') IS NOT NULL
    DROP PROCEDURE dbo.UpdateAffiliatePerformance;
GO

CREATE PROCEDURE dbo.UpdateAffiliatePerformance
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CurrentPeriodStart DATE = CAST(GETDATE() AS DATE);
    DECLARE @CurrentPeriodEnd DATE = DATEADD(MONTH, 1, @CurrentPeriodStart);
    
    -- Update current period performance
    MERGE AffiliatePerformance AS target
    USING (
        SELECT 
            @UserId AS UserId,
            @CurrentPeriodStart AS PeriodStartDate,
            @CurrentPeriodEnd AS PeriodEndDate,
            COUNT(DISTINCT ah.ChildUserId) AS DirectReferrals,
            COUNT(DISTINCT ah2.ChildUserId) AS TotalReferrals,
            ISNULL(SUM(acl.CommissionAmount), 0) AS TotalCommissionEarned,
            COUNT(DISTINCT CASE WHEN u.IsActive = 1 THEN ah.ChildUserId END) AS ActiveReferrals
        FROM Users u
        LEFT JOIN AffiliateHierarchy ah ON u.UserID = ah.ParentUserId AND ah.Level = 1
        LEFT JOIN AffiliateHierarchy ah2 ON u.UserID = ah2.ParentUserId
        LEFT JOIN AffiliateCommissionLog acl ON u.UserID = acl.AffiliateUserId 
            AND acl.CreatedAt >= @CurrentPeriodStart
        WHERE u.UserID = @UserId
        GROUP BY u.UserID
    ) AS source ON target.UserId = source.UserId 
        AND target.PeriodStartDate = source.PeriodStartDate
    
    WHEN MATCHED THEN
        UPDATE SET
            DirectReferrals = source.DirectReferrals,
            TotalReferrals = source.TotalReferrals,
            TotalCommissionEarned = source.TotalCommissionEarned,
            ActiveReferrals = source.ActiveReferrals,
            UpdatedAt = GETUTCDATE()
    
    WHEN NOT MATCHED THEN
        INSERT (UserId, PeriodStartDate, PeriodEndDate, DirectReferrals, TotalReferrals, 
                TotalCommissionEarned, ActiveReferrals, CurrentTierId, CreatedAt)
        VALUES (source.UserId, source.PeriodStartDate, source.PeriodEndDate, 
                source.DirectReferrals, source.TotalReferrals, source.TotalCommissionEarned, 
                source.ActiveReferrals, 1, GETUTCDATE());
                
    -- Check for tier upgrades
    EXEC dbo.CheckAffiliateHierarchyUpgrade @UserId;
END
GO

-- =============================================
-- 5. Create Commission Processing Stored Procedure
-- =============================================
IF OBJECT_ID('dbo.ProcessAffiliateCommissions', 'P') IS NOT NULL
    DROP PROCEDURE dbo.ProcessAffiliateCommissions;
GO

CREATE PROCEDURE dbo.ProcessAffiliateCommissions
    @ReferredUserId INT,
    @SourceTransactionId BIGINT,
    @OriginalAmount DECIMAL(18,2),
    @CommissionType NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        -- Get all affiliates in hierarchy for this user
        DECLARE affiliate_cursor CURSOR FOR
        SELECT 
            ah.ParentUserId,
            ah.Level,
            u.AffiliateLevel
        FROM AffiliateHierarchy ah
        INNER JOIN Users u ON ah.ParentUserId = u.UserID
        WHERE ah.ChildUserId = @ReferredUserId
          AND u.IsAffiliateActive = 1
        ORDER BY ah.Level;
        
        DECLARE @AffiliateUserId INT, @HierarchyLevel INT, @AffiliateLevel INT;
        DECLARE @CommissionAmount DECIMAL(18,2);
        
        OPEN affiliate_cursor;
        FETCH NEXT FROM affiliate_cursor INTO @AffiliateUserId, @HierarchyLevel, @AffiliateLevel;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Calculate commission amount
            SET @CommissionAmount = dbo.CalculateAffiliateCommission(
                @OriginalAmount, 
                @CommissionType, 
                @AffiliateLevel, 
                @HierarchyLevel
            );
            
            -- Only create commission log if amount > 0
            IF @CommissionAmount > 0.01
            BEGIN
                INSERT INTO AffiliateCommissionLog (
                    AffiliateUserId,
                    ReferredUserId,
                    SourceWalletTransactionId,
                    CommissionLevel,
                    OriginalAmount,
                    CommissionRate,
                    CommissionAmount,
                    CommissionType,
                    Status,
                    Description,
                    CreatedAt
                )
                VALUES (
                    @AffiliateUserId,
                    @ReferredUserId,
                    @SourceTransactionId,
                    @HierarchyLevel,
                    @OriginalAmount,
                    @CommissionAmount / @OriginalAmount,
                    @CommissionAmount,
                    @CommissionType,
                    'PENDING',
                    'Level ' + CAST(@HierarchyLevel AS NVARCHAR(2)) + ' commission from ' + @CommissionType,
                    GETUTCDATE()
                );
            END
            
            FETCH NEXT FROM affiliate_cursor INTO @AffiliateUserId, @HierarchyLevel, @AffiliateLevel;
        END
        
        CLOSE affiliate_cursor;
        DEALLOCATE affiliate_cursor;
        
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS('global','affiliate_cursor') >= 0
        BEGIN
            CLOSE affiliate_cursor;
            DEALLOCATE affiliate_cursor;
        END
        
        -- Log error instead of throwing to prevent transaction failure
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        -- Insert error log (assuming SystemLogs table exists)
        IF OBJECT_ID('SystemLogs', 'U') IS NOT NULL
        BEGIN
            INSERT INTO SystemLogs (LogLevel, Message, Exception, CreatedAt)
            VALUES ('ERROR', 'ProcessAffiliateCommissions failed for UserId: ' + CAST(@ReferredUserId AS NVARCHAR(10)), @ErrorMessage, GETUTCDATE());
        END
        
        -- Re-raise the error
        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO



-- =============================================
-- 6. Create User Registration Trigger
-- =============================================
IF OBJECT_ID('TR_Users_AfterInsert', 'TR') IS NOT NULL
    DROP TRIGGER TR_Users_AfterInsert;
GO

CREATE TRIGGER TR_Users_AfterInsert
ON Users
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @NewUserId INT, @ReferrerUserId INT, @Username NVARCHAR(255);
        
        SELECT @NewUserId = UserID, @ReferrerUserId = ReferredByUserId, @Username = Username
        FROM inserted;
        
        -- Generate referral code if not provided
        IF NOT EXISTS (SELECT 1 FROM inserted WHERE ReferralCode IS NOT NULL AND ReferralCode != '')
        BEGIN
            UPDATE Users 
            SET ReferralCode = UPPER(@Username + '_' + CAST(@NewUserId AS NVARCHAR(10)))
            WHERE UserID = @NewUserId;
        END
        
        -- Build affiliate hierarchy if user was referred
        IF @ReferrerUserId IS NOT NULL
        BEGIN
            INSERT INTO AffiliateHierarchy (ChildUserId, ParentUserId, Level, HierarchyPath, CreatedAt)
            SELECT ChildUserId, ParentUserId, Level, HierarchyPath, GETUTCDATE()
            FROM dbo.BuildAffiliateHierarchy(@NewUserId, @ReferrerUserId);
            
            -- Update referrer's performance metrics
            EXEC dbo.UpdateAffiliatePerformance @ReferrerUserId;
        END
        
        -- Create initial performance record
        INSERT INTO AffiliatePerformance (
            UserId, PeriodStartDate, PeriodEndDate, CurrentTierId, 
            DirectReferrals, TotalReferrals, TotalCommissionEarned, ActiveReferrals,
            CreatedAt
        )
        VALUES (
            @NewUserId, CAST(GETDATE() AS DATE), DATEADD(MONTH, 1, CAST(GETDATE() AS DATE)), 1,
            0, 0, 0.00, 0,
            GETUTCDATE()
        );
        
    END TRY
    BEGIN CATCH
        -- Log error but don't fail user creation (only if SystemLogs exists)
        IF OBJECT_ID('SystemLogs', 'U') IS NOT NULL
        BEGIN
            INSERT INTO SystemLogs (LogLevel, Message, Exception, CreatedAt)
            VALUES (
                'ERROR', 
                'Error in TR_Users_AfterInsert for UserId: ' + CAST(@NewUserId AS NVARCHAR(10)),
                ERROR_MESSAGE(),
                GETUTCDATE()
            );
        END
    END CATCH
END
GO

-- =============================================
-- 7. Create Transaction Processing Trigger
-- =============================================
IF OBJECT_ID('TR_WalletTransactions_AfterInsert', 'TR') IS NOT NULL
    DROP TRIGGER TR_WalletTransactions_AfterInsert;
GO

CREATE TRIGGER TR_WalletTransactions_AfterInsert
ON WalletTransactions
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @TransactionId BIGINT, @WalletId INT, @UserId INT, @Amount DECIMAL(18,2);
        DECLARE @TransactionType NVARCHAR(50), @CommissionType NVARCHAR(50);
        
        SELECT 
            @TransactionId = i.TransactionID,
            @WalletId = i.WalletID,
            @Amount = ABS(i.Amount),
            @TransactionType = ISNULL(tt.TypeName, 'UNKNOWN')
        FROM inserted i
        LEFT JOIN WalletTransactionTypes tt ON i.TransactionTypeID = tt.TransactionTypeID;
        
        -- Get user ID from wallet (assuming UserID column exists in Wallets table)
        SELECT @UserId = UserID FROM Wallets WHERE WalletID = @WalletId;
        
        -- Determine if this transaction generates commission
        SET @CommissionType = CASE @TransactionType
            WHEN 'TRADING_FEE' THEN 'TRADING_FEE'
            WHEN 'DEPOSIT_FEE' THEN 'DEPOSIT_FEE'
            WHEN 'WITHDRAWAL_FEE' THEN 'WITHDRAWAL_FEE'
            ELSE NULL
        END;
        
        -- Process commissions if applicable
        IF @CommissionType IS NOT NULL AND @Amount > 0 AND @UserId IS NOT NULL
        BEGIN
            EXEC dbo.ProcessAffiliateCommissions @UserId, @TransactionId, @Amount, @CommissionType;
        END
        
    END TRY
    BEGIN CATCH
        -- Log error but don't fail transaction (only if SystemLogs exists)
        IF OBJECT_ID('SystemLogs', 'U') IS NOT NULL
        BEGIN
            INSERT INTO SystemLogs (LogLevel, Message, Exception, CreatedAt)
            VALUES (
                'ERROR', 
                'Error in TR_WalletTransactions_AfterInsert for TransactionId: ' + CAST(@TransactionId AS NVARCHAR(20)),
                ERROR_MESSAGE(),
                GETUTCDATE()
            );
        END
    END CATCH
END
GO

-- =============================================
-- 8. Create Commission Processing Job (Optional)
-- =============================================
IF OBJECT_ID('dbo.ProcessPendingCommissions', 'P') IS NOT NULL
    DROP PROCEDURE dbo.ProcessPendingCommissions;
GO

CREATE PROCEDURE dbo.ProcessPendingCommissions
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        DECLARE @ProcessedCount INT = 0;
        
        -- Process pending commissions to wallet
        UPDATE acl
        SET Status = 'PROCESSED',
            ProcessedAt = GETUTCDATE()
        FROM AffiliateCommissionLog acl
        INNER JOIN Users u ON acl.AffiliateUserId = u.UserId
        WHERE acl.Status = 'PENDING'
          AND u.IsActive = 1
          AND u.IsAffiliateActive = 1
          AND acl.CreatedAt <= DATEADD(HOUR, -1, GETUTCDATE()); -- Process after 1 hour
        
        SET @ProcessedCount = @@ROWCOUNT;
        
        -- Add commission amounts to affiliate wallets
        INSERT INTO WalletTransactions (
            WalletID, TransactionTypeID, Amount, Description, ReferenceID, CreatedAt
        )
        SELECT 
            w.WalletID,
            (SELECT TransactionTypeID FROM WalletTransactionTypes WHERE TypeName = 'AFFILIATE_COMMISSION'),
            acl.CommissionAmount,
            'Affiliate commission: ' + acl.Description,
            acl.CommissionLogId,
            GETUTCDATE()
        FROM AffiliateCommissionLog acl
        INNER JOIN Users u ON acl.AffiliateUserId = u.UserID
        INNER JOIN Wallets w ON u.UserID = w.UserID
        WHERE acl.Status = 'PROCESSED'
          AND acl.ProcessedAt >= DATEADD(MINUTE, -5, GETUTCDATE()); -- Just processed
        
        -- Log processing summary (only if SystemLogs table exists)
        IF OBJECT_ID('SystemLogs', 'U') IS NOT NULL
        BEGIN
            INSERT INTO SystemLogs (LogLevel, Message, CreatedAt)
            VALUES (
                'INFO',
                'Processed ' + CAST(@ProcessedCount AS NVARCHAR(10)) + ' affiliate commissions',
                GETUTCDATE()
            );
        END
        
    END TRY
    BEGIN CATCH
        -- Log error (only if SystemLogs table exists)
        IF OBJECT_ID('SystemLogs', 'U') IS NOT NULL
        BEGIN
            INSERT INTO SystemLogs (LogLevel, Message, Exception, CreatedAt)
            VALUES (
                'ERROR', 
                'Error in ProcessPendingCommissions',
                ERROR_MESSAGE(),
                GETUTCDATE()
            );
        END
    END CATCH
END
GO

-- =============================================
-- 9. Create Indexes for Performance
-- =============================================

-- Index on AffiliateHierarchy for quick lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AffiliateHierarchy_ChildUserId')
    CREATE NONCLUSTERED INDEX IX_AffiliateHierarchy_ChildUserId 
    ON AffiliateHierarchy (ChildUserId) 
    INCLUDE (ParentUserId, Level);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AffiliateHierarchy_ParentUserId')
    CREATE NONCLUSTERED INDEX IX_AffiliateHierarchy_ParentUserId 
    ON AffiliateHierarchy (ParentUserId) 
    INCLUDE (ChildUserId, Level);

-- Index on AffiliateCommissionLog for processing
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AffiliateCommissionLog_Status_CreatedAt')
    CREATE NONCLUSTERED INDEX IX_AffiliateCommissionLog_Status_CreatedAt 
    ON AffiliateCommissionLog (Status, CreatedAt) 
    INCLUDE (AffiliateUserId, CommissionAmount);

-- Index on Users for affiliate lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_ReferredByUserId')
    CREATE NONCLUSTERED INDEX IX_Users_ReferredByUserId 
    ON Users (ReferredByUserId) 
    WHERE ReferredByUserId IS NOT NULL;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_IsAffiliateActive')
    CREATE NONCLUSTERED INDEX IX_Users_IsAffiliateActive 
    ON Users (IsAffiliateActive, AffiliateLevel) 
    WHERE IsAffiliateActive = 1;

PRINT 'SCRUM-85: Affiliate Hierarchy Management Triggers and Functions created successfully!'
PRINT 'Created Components:'
PRINT '- BuildAffiliateHierarchy function'
PRINT '- CalculateAffiliateCommission function'
PRINT '- UpdateAffiliatePerformance procedure'
PRINT '- ProcessAffiliateCommissions procedure'
PRINT '- CheckAffiliateHierarchyUpgrade procedure'
PRINT '- TR_Users_AfterInsert trigger'
PRINT '- TR_WalletTransactions_AfterInsert trigger'
PRINT '- ProcessPendingCommissions procedure'
PRINT '- Performance indexes'
PRINT ''
PRINT 'Next Steps:'
PRINT '1. Test all triggers with sample data'
PRINT '2. Set up scheduled job for ProcessPendingCommissions'
PRINT '3. Monitor performance and adjust indexes as needed'
PRINT '4. Verify commission calculations are accurate'