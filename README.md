[![OpenDream](.github/assets/OpenDream.png)](#)

**OpenDream** is a C# project that aims to compile games made in the [DM language], and run them.

This project is currently in early stages, and is not feature-complete.

All parts of OpenDream should work fine on Windows and Linux, though the latter is not used as much and therefore isn't as thoroughly tested.

For more information or if you'd like to contribute, join our [Discord server](https://discord.gg/qreryhZxxs).

A detailed description of differences with BYOND can be found [here](https://github.com/wixoaGit/OpenDream/wiki/Differences-Between-OpenDream-and-BYOND).

## Building

The first step to building OpenDream is initializing the submodule for the game engine, [Robust Toolbox](https://github.com/space-wizards/RobustToolbox). 

To do this, simply run `git submodule update --init --recursive` in git bash and let it finish.

OpenDream requires .NET 6. To build, one can use a C# compiler (such as MSBuild) to compile the various projects described in the solution.

There's 3 main parts: Compiler, Server, and Client

## Running

**Compiler:** Run `DMCompiler.exe`, and pass any number of .dm or .dme files to compile as arguments.

**Server:** Run `OpenDreamServer.exe` and pass the compiled JSON file you got as a result of running the compiler above as an argument like this: `--cvar opendream.json_path=C:/path/to/compiler/output.json`

**Client:** Run `OpenDreamClient.exe`. You will be prompted to choose a server address, port, and username. The defaults should work for a locally hosted server.

## Screenshots
The following screenshots are taken from a stripped-down version of /tg/station available [here](https://github.com/wixoaGit/tgstation).

![](https://github.com/wixoaGit/OpenDream/blob/master/.github/assets/screenshot.png?raw=true)
![](https://github.com/wixoaGit/OpenDream/blob/master/.github/assets/screenshot2.png?raw=true)

[DM Language]: http://secure.byond.com/
