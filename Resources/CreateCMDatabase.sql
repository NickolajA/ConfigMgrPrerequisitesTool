SET NOCOUNT ON;
GO
USE [master];
GO
/************************************************************************************************************************
    REQUIRED VARIABLES TO UPDATE:
    Make sure to update the following variables!
************************************************************************************************************************/
DECLARE  @SecondDataDrive     nchar(1) = N''
        ,@InitialDataFileSize nvarchar(50) = N'2621440KB'
        ,@InitialLogFileSize  nvarchar(50) = N'5242880KB'
        ,@PriDataFileGrowth   nvarchar(50) = N'1024MB'
        ,@LogFileGrowth       nvarchar(50) = N'1024MB';

/************************************************************************************************************************
    INTERNAL VARIABLES:
    These are NOT to be changed or updated!
************************************************************************************************************************/
DECLARE  @DefLog       nvarchar(512)
        ,@DefMdf       nvarchar(512)
        ,@SecNdf       nvarchar(512)
        ,@CreateDB     nvarchar(max)
        ,@AddtlFiles   nvarchar(max) = N''
        ,@LogScript    nvarchar(max)
        ,@AddtlFileNum tinyint = 1
        ,@TwoDrives    bit = 1
        ,@Mdfi         tinyint
        ,@Ldfi         tinyint
        ,@Arg          nvarchar(10)
        ,@MdlDtaFlSze  int
        ,@MdlLogFlSze  int;

/************************************************************************************************************************
    Make sure the database doesn't already exist before continuing.
************************************************************************************************************************/
SET @CMSiteCode = UPPER(@CMSiteCode);
IF DATABASEPROPERTYEX(N'CM_'+@CMSiteCode, 'status') IS NOT NULL
GOTO DBExists;

/************************************************************************************************************************
    Initialize/Check Working Variables
************************************************************************************************************************/
-- Remove any spaces from the size and growth definitions:
SET @PriDataFileGrowth = UPPER(REPLACE(@PriDataFileGrowth,N' ',N''));
SET @LogFileGrowth = UPPER(REPLACE(@LogFileGrowth,N' ',N''));
SET @InitialDataFileSize = UPPER(REPLACE(@InitialDataFileSize,N' ',N''));
SET @InitialLogFileSize = UPPER(REPLACE(@InitialLogFileSize,N' ',N''));
SET @SecondDataDrive = UPPER(@SecondDataDrive);

-- Ensure the initial file sizes aren't smaller than the model database size; if so update the size:
SELECT  @MdlDtaFlSze = size*8 FROM model.sys.database_files WHERE type = 0;
SELECT  @MdlLogFlSze = size*8 FROM model.sys.database_files WHERE type = 1;
IF CAST(LEFT(@InitialDataFileSize,LEN(@InitialDataFileSize)-2) AS int) < @MdlDtaFlSze
BEGIN
    SET @InitialDataFileSize = CAST(@MdlDtaFlSze AS nvarchar(48))+N'KB';
END;

IF (CAST(LEFT(@InitialLogFileSize,LEN(@InitialLogFileSize)-2) AS int)) < @MdlLogFlSze
BEGIN
    SET @InitialLogFileSize = CAST(@MdlLogFlSze AS nvarchar(48))+N'KB';
END;

-- Get the Default MDF location (from the registry):
EXECUTE master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE', N'Software\Microsoft\MSSQLServer\MSSQLServer', N'DefaultData', @DefMdf OUTPUT, 'no_output';
IF @DefMdf IS NULL -- if we couldn't get the key from this location for some reason then look at the startup parameters:
BEGIN
    SET @Mdfi = 0;
    WHILE @Mdfi < 100
    BEGIN
        SELECT @Arg = N'SQLArg' + CAST(@Mdfi AS nvarchar(4));
        EXECUTE master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE', N'Software\Microsoft\MSSQLServer\MSSQLServer\Parameters', @Arg, @DefMdf OUTPUT, 'no_output';
        IF LOWER(LEFT(REVERSE(@DefMdf),10)) = N'fdm.retsam'
        BEGIN
            -- If we found the parameter for the master data file then set the variable and stop processing this loop:
            SELECT @DefMdf = SUBSTRING(@DefMdf,3,CHARINDEX(N'\master.mdf',@DefMdf)-3);
            BREAK;
        END;
        ELSE
        SET @DefMdf = NULL;

        SELECT @Mdfi += 1;
    END;
END;

-- Get the Default LDF location (from the registry):
EXECUTE master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE', N'Software\Microsoft\MSSQLServer\MSSQLServer', N'DefaultLog', @DefLog OUTPUT, 'no_output';
IF @DefLog IS NULL -- if we couldn't get the key from this location for some reason then look at the startup parameters:
BEGIN
    SET @Ldfi = 0;
    WHILE @Ldfi < 100
    BEGIN
        SELECT @Arg = N'SQLArg' + CAST(@Ldfi AS nvarchar(4));
        EXECUTE master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE', N'Software\Microsoft\MSSQLServer\MSSQLServer\Parameters', @Arg, @DefLog OUTPUT, 'no_output';
        IF LOWER(LEFT(REVERSE(@DefLog),11)) = N'fdl.goltsam'
        BEGIN
            -- If we found the parameter for the master log file then set the variable and stop processing this loop:
            SELECT @DefLog = SUBSTRING(@DefLog,3,CHARINDEX(N'\mastlog.ldf',@DefLog)-3);
            BREAK;
        END;
        ELSE
        SET @DefLog = NULL;

        SELECT @Ldfi += 1;
    END;
END;

-- Determine whether data files will be stored on two drives or on the same drive:
IF (ISNULL(@SecondDataDrive, N'') = N'') OR (LEFT(@DefMdf,1) = @SecondDataDrive)
BEGIN
    SET @TwoDrives = 0;
END;
ELSE
BEGIN
    SET @SecNdf = @SecondDataDrive + SUBSTRING(@DefMdf,2,LEN(@DefMdf)-1);
END;

/************************************************************************************************************************
    Ensure the Secondary data file location exists; if not create it:
************************************************************************************************************************/
IF @TwoDrives = 1
BEGIN
    DECLARE  @FileExists TABLE ( isFile       int NOT NULL
                                ,isDirectory  int NOT NULL
                                ,ParentExists int NOT NULL
                                );
    INSERT @FileExists
    EXECUTE master..xp_fileexist @SecNdf;

    IF (SELECT isDirectory FROM @FileExists) = 0
    BEGIN
        EXECUTE master..xp_create_subdir @SecNdf;
    END
END;

/************************************************************************************************************************
    Create the statement that will create the database with all user input and defaults.
************************************************************************************************************************/
-- First, create the 'additional' data files portion of the statement:
WHILE @AddtlFileNum < @NumTotalDataFiles
BEGIN
IF @TwoDrives = 0
-- If we are storing all the files on the same drive then we only need to use this logic:
SELECT @AddtlFiles += N'              ,( NAME = N''CM_'+@CMSiteCode+N'_'+CAST(@AddtlFileNum AS nvarchar(3))+N'''
                ,FILENAME = N'''+@DefMdf+N'\CM_'+@CMSiteCode+N'_'+CAST(@AddtlFileNum AS nvarchar(3))+N'.ndf''
                ,SIZE = '+@InitialDataFileSize+N'
                ,FILEGROWTH = '+@PriDataFileGrowth+N'
                )
';
ELSE -- else, we'll be storing data files in two locations
    BEGIN
    IF @AddtlFileNum % 2 = 1
    -- The "odd" number data files will be stored on the second drive:
    SELECT @AddtlFiles += N'              ,( NAME = N''CM_'+@CMSiteCode+N'_'+CAST(@AddtlFileNum AS nvarchar(3))+N'''
                    ,FILENAME = N'''+@SecNdf+N'\CM_'+@CMSiteCode+N'_'+CAST(@AddtlFileNum AS nvarchar(3))+N'.ndf''
                    ,SIZE = '+@InitialDataFileSize+N'
                    ,FILEGROWTH = '+@PriDataFileGrowth+N'
                    )
    ';
    ELSE
    -- The even numbered files will be stored on the default drive:
    SELECT @AddtlFiles += N'              ,( NAME = N''CM_'+@CMSiteCode+N'_'+CAST(@AddtlFileNum AS nvarchar(3))+N'''
                    ,FILENAME = N'''+@DefMdf+N'\CM_'+@CMSiteCode+N'_'+CAST(@AddtlFileNum AS nvarchar(3))+N'.ndf''
                    ,SIZE = '+@InitialDataFileSize+N'
                    ,FILEGROWTH = '+@PriDataFileGrowth+N'
                    )
    ';
    END;
SELECT @AddtlFileNum += 1;
END;

-- Second, create the log file portion of the statement:
SET @LogScript = N'LOG ON ( NAME = N''CM_'+@CMSiteCode+N'_Log''
        ,FILENAME = N'''+@DefLog+N'\CM_'+@CMSiteCode+N'_Log.LDF''
        ,SIZE = '+@InitialLogFileSize+N'
        ,FILEGROWTH = '+@LogFileGrowth+N'
        );';

-- Third, create the beginning portion of the statement:
SET @CreateDB = N'CREATE DATABASE [CM_'+@CMSiteCode+N']
    ON PRIMARY ( NAME = N''CM_'+@CMSiteCode+N'''
                ,FILENAME = N'''+@DefMdf+N'\CM_'+@CMSiteCode+N'.mdf''
                ,SIZE = '+@InitialDataFileSize+N'
                ,FILEGROWTH = '+@PriDataFileGrowth+N'
                )
';

-- Finally, put all the statements together in one final statement:
SELECT @CreateDB += @AddtlFiles;
SELECT @CreateDB += @LogScript;

/************************************************************************************************************************
    Create the database using the statement built:
************************************************************************************************************************/
BEGIN TRY
    EXECUTE sp_executesql @CreateDB;
    EXECUTE (N'ALTER AUTHORIZATION ON DATABASE::[CM_'+@CMSiteCode+N'] TO sa;');
    GOTO ScriptSuccess;
END TRY
BEGIN CATCH
    GOTO ExitError;
END CATCH;

/************************************************************************************************************************
    Exits and End of Script:
************************************************************************************************************************/
DBExists:
PRINT N'Database exist';
GOTO EndScript;

ExitError:
PRINT N'Error';
GOTO EndScript;

ScriptSuccess:
PRINT N'Success';
GOTO EndScript;

EndScript:
GO