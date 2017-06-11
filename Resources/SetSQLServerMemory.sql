DECLARE @MaxMem VARCHAR(6), @MinMem VARCHAR(6)

-- Configure SQL memory
EXEC sys.sp_configure N'show advanced options', N'1'
RECONFIGURE WITH OVERRIDE
EXEC sys.sp_configure N'min server memory (MB)', @MinMem
EXEC sys.sp_configure N'max server memory (MB)', @MaxMem
RECONFIGURE WITH OVERRIDE
EXEC sys.sp_configure N'show advanced options', N'0'
RECONFIGURE WITH OVERRIDE