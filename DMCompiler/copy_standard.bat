@echo off
if not exist bin\Debug\net6.0\DMStandard mkdir bin\Debug\net6.0\DMStandard
xcopy DMStandard bin\Debug\net6.0\DMStandard /y /s /e
if not exist bin\Release\net6.0\DMStandard mkdir bin\Release\net6.0\DMStandard
xcopy DMStandard bin\Release\net6.0\DMStandard /y /s /e
