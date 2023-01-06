@echo off
if not exist bin\Debug\net7.0\DMStandard mkdir bin\Debug\net7.0\DMStandard
xcopy DMStandard bin\Debug\net7.0\DMStandard /y /s /e
if not exist bin\Release\net7.0\DMStandard mkdir bin\Release\net7.0\DMStandard
xcopy DMStandard bin\Release\net7.0\DMStandard /y /s /e
