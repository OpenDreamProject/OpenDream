[![OpenDream](.github/assets/OpenDream.png)](#)

**OpenDream** is a C# project that aims to compile games made in the [DM language], and run them.

**This project is currently in early stages.** Expect bugs, missing features, and a lack of quality-of-life enhancements. Set your expectations accordingly.

All parts of OpenDream should work fine on Windows and Linux, though the latter is not used as much and therefore isn't as thoroughly tested.

For more information or if you'd like to contribute, join our [Discord server](https://discord.gg/qreryhZxxs).

A detailed description of differences with BYOND can be found [here](https://github.com/wixoaGit/OpenDream/wiki/Differences-Between-OpenDream-and-BYOND). **Note that OpenDream cannot connect to BYOND servers, and BYOND's client cannot connect to OpenDream servers.** There is no cross-project compatibility other than being able to migrate from BYOND to OpenDream with minimal effort.

## Building

The first step to building OpenDream is initializing the submodule for the game engine, [Robust Toolbox](https://github.com/space-wizards/RobustToolbox). 

To do this, simply run `git submodule update --init --recursive` in git bash and let it finish.

**OpenDream requires .NET 8.** You can check your version by running `dotnet --version`. It should be at least `8.0.0`.

To build, one can use a C# compiler (such as MSBuild) to compile the various projects described in the solution.

There's 3 main parts: Compiler, Server, and Client

## Running

**Compiler:** Run `DMCompiler.exe`, and pass any number of .dm or .dme files to compile as arguments. Optional arguments can be found [here](https://github.com/wixoaGit/OpenDream/wiki/Compiler-Options).

**Server:** Run `OpenDreamServer.exe` and pass the compiled JSON file you got as a result of running the compiler above as an argument like this: `OpenDreamServer.exe C:/path/to/compiler/output.json`

**Client:** Run `OpenDreamClient.exe`. You will be prompted to choose a server address, port, and username. The defaults should work for a locally hosted server.

## Screenshots
The following screenshots are taken from a version of Paradise Station with a recompiled 64-bit rustg DLL. This branch of Paradise is available [here](https://github.com/ike709/Paradise/tree/rustg_64).

![](./.github/assets/screenshot.png?raw=true)
![](./.github/assets/screenshot2.png?raw=true)

[DM Language]: http://secure.byond.com/
