rem TODO: make this a real script
cargo clean -p trampoline
cargo clean -p byondapitest
del ..\meowtonin\crates\sys\link\windows\byondcore.dll.lib
del ..\bin\Content.Server\byondcore.dll
del ..\bin\Content.Tests\byondcore.dll

cargo build -p trampoline

copy .\target\debug\byondcore.dll.lib ..\meowtonin\crates\sys\link\windows\byondcore.dll.lib
copy .\target\debug\byondcore.dll  ..\bin\Content.Server\byondcore.dll
copy .\target\debug\byondcore.dll  ..\bin\Content.Tests\byondcore.dll
cargo build -p byondapitest
copy .\target\debug\byondapitest.dll ..\bin\Content.Server\byondapitest.dll
copy .\target\debug\byondapitest.dll ..\bin\Content.Tests\byondapitest.dll