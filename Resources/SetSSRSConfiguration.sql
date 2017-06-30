-- ,@ReportServerMaxSizeGB FLOAT = 20
-- ,@ReportServerTempDBMaxSizeGB FLOAT = 20;

DECLARE  @ReportServerFileGrowthConfig NVARCHAR(MAX)
		,@ReportServerRecoveryModelConfig NVARCHAR(MAX)
		,@ReportServerTempDBRecoveryModelConfig NVARCHAR(MAX)
		,@ReportServerMaxSizeMB INT
		,@ReportServerTempDBMaxSizeMB INT
		
/***********************************************************************************************************************************************
-- Set ReportServer database recovery model
***********************************************************************************************************************************************/

SET @ReportServerRecoveryModelConfig = '
	USE ReportServer
	ALTER DATABASE ReportServer SET RECOVERY SIMPLE
'
EXECUTE SP_EXECUTESQL @ReportServerRecoveryModelConfig

SET @ReportServerTempDBRecoveryModelConfig = '
	USE ReportServerTempDB
	ALTER DATABASE ReportServerTempDB SET RECOVERY SIMPLE
'
EXECUTE SP_EXECUTESQL @ReportServerTempDBRecoveryModelConfig

/***********************************************************************************************************************************************
-- Set ReportServer and ReportServerTempDB database files growth
***********************************************************************************************************************************************/

SELECT
@ReportServerMaxSizeMB = FLOOR(@ReportServerMaxSizeGB * 1024),
@ReportServerTempDBMaxSizeMB = FLOOR(@ReportServerTempDBMaxSizeGB * 1024)

SET @ReportServerFileGrowthConfig = '
	USE ReportServer
	ALTER DATABASE [ReportServer] MODIFY FILE (
		NAME = ReportServer,
		FILEGROWTH = 100MB,
		MAXSIZE = '+ CONVERT(NVARCHAR(10), @ReportServerMaxSizeMB) +'MB
	)
	ALTER DATABASE [ReportServer] MODIFY FILE (
		NAME = ReportServer_log,
		FILEGROWTH = 100MB,
		MAXSIZE = '+ CONVERT(NVARCHAR(10), @ReportServerMaxSizeMB) +'MB
	)	
	
	USE ReportServerTempDB
	ALTER DATABASE [ReportServerTempDB] MODIFY FILE (
		NAME = ReportServerTempDB,
		FILEGROWTH = 100MB,
		MAXSIZE = '+ CONVERT(NVARCHAR(10), @ReportServerTempDBMaxSizeMB) +'MB
	)
	ALTER DATABASE [ReportServerTempDB] MODIFY FILE (
		NAME = ReportServerTempDB_log,
		FILEGROWTH = 100MB,
		MAXSIZE = '+ CONVERT(NVARCHAR(10), @ReportServerTempDBMaxSizeMB) +'MB
	)	
'
EXECUTE SP_EXECUTESQL @ReportServerFileGrowthConfig