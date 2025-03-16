rem convenience script for building byondapi in windows targeting opendream
rem run this after initializing git submodules and building with dotnet/visual studio
rem TODO: use release instead of debug

cargo clean --manifest-path "%~dp0\Cargo.toml" -p trampoline
cargo clean --manifest-path "%~dp0\Cargo.toml" -p byondapitest
del "%~dp0\..\meowtonin\crates\sys\link\windows\byondapi.lib"
del "%~dp0\..\bin\Content.Server\byondcore.dll"
del "%~dp0\..\bin\Content.Tests\byondcore.dll"
del "%~dp0\..\bin\Content.IntegrationTests\byondcore.dll"
del "%~dp0\..\bin\Content.Server\byondapitest.dll"
del "%~dp0\..\bin\Content.Tests\byondapitest.dll"
del "%~dp0\..\bin\Content.IntegrationTests\byondapitest.dll"

cargo build --manifest-path "%~dp0\Cargo.toml" -p trampoline
copy "%~dp0\target\debug\byondcore.dll.lib" "%~dp0\..\meowtonin\crates\sys\link\windows\byondapi.lib"
copy "%~dp0\target\debug\byondcore.dll"  "%~dp0\..\bin\Content.Server\byondcore.dll"
copy "%~dp0\target\debug\byondcore.dll"  "%~dp0\..\bin\Content.Tests\byondcore.dll"
copy "%~dp0\target\debug\byondcore.dll"  "%~dp0\..\bin\Content.IntegrationTests\byondcore.dll"

cargo build --manifest-path "%~dp0\Cargo.toml" -p byondapitest
copy "%~dp0\target\debug\byondapitest.dll" "%~dp0\..\bin\Content.Server\byondapitest.dll"
copy "%~dp0\target\debug\byondapitest.dll" "%~dp0\..\bin\Content.Tests\byondapitest.dll"
copy "%~dp0\target\debug\byondapitest.dll" "%~dp0\..\bin\Content.IntegrationTests\byondapitest.dll"
