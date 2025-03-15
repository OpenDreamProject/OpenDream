git -C ../meowtonin reset --hard
cargo build --target=i686-pc-windows-msvc
copy .\target\i686-pc-windows-msvc\debug\byondapitest.dll ..\TestGame\byondapitest.dll
