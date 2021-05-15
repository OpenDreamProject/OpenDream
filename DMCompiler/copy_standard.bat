@echo off
if not exist bin\Debug\net5.0\DMStandard mkdir bin\Debug\net5.0\DMStandard
xcopy DMStandard bin\Debug\net5.0\DMStandard /y /s /e
if not exist bin\Release\net5.0\DMStandard mkdir bin\Release\net5.0\DMStandard
xcopy DMStandard bin\Release\net5.0\DMStandard /y /s /e
