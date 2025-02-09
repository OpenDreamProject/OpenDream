@echo off
if not exist bin\Debug\net9.0\DMStandard mkdir bin\Debug\net9.0\DMStandard
xcopy DMStandard bin\Debug\net9.0\DMStandard /y /s /e
if not exist bin\Release\net9.0\DMStandard mkdir bin\Release\net9.0\DMStandard
xcopy DMStandard bin\Release\net9.0\DMStandard /y /s /e
