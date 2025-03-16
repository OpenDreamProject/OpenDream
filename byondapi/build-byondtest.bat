rem convenience script for building byondapi & test in windows targeting byond
rem run this after intiializing git submodules
rem Useful for parity testing

git -C "%~dp0\..\meowtonin" reset --hard
cargo build --manifest-path "%~dp0\Cargo.toml" --target=i686-pc-windows-msvc
copy "%~dp0\target\i686-pc-windows-msvc\debug\byondapitest.dll" "%~dp0\..\TestGame\byondapitest.dll"
