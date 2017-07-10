/***********************************************************************************************************************************************
-- Configure SQL memory
***********************************************************************************************************************************************/

BEGIN TRY
	EXEC sys.sp_configure N'show advanced options', N'1'
	RECONFIGURE WITH OVERRIDE
	EXEC sys.sp_configure N'min server memory (MB)', @MinMem
	EXEC sys.sp_configure N'max server memory (MB)', @MaxMem
	RECONFIGURE WITH OVERRIDE
	EXEC sys.sp_configure N'show advanced options', N'0'
	RECONFIGURE WITH OVERRIDE
	GOTO ScriptSuccess;
END TRY
BEGIN CATCH
	GOTO ExitError;
END CATCH
		
/************************************************************************************************************************
    Exits and End of Script:
************************************************************************************************************************/

ExitError:
PRINT N'Error';
SET @ReturnValue = 1
GOTO EndScript;

ScriptSuccess:
PRINT N'Success';
SET @ReturnValue = 0
GOTO EndScript;

EndScript:
PRINT @ReturnValue