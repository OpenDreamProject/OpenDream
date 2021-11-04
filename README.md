[![OpenDream](.github/assets/OpenDream.png)](#)

**OpenDream** is a C# project that aims to compile games made in the [DM language], and run them.

This project is currently in early stages, and is not feature-complete.

The compiler and server should work fine for Linux-based machines. The client should also work fine on Linux but none of the developers use it. More details and potentially useful troubleshooting for running on Linux can be found in the haphazardly written FAQ [here](https://github.com/wixoaGit/OpenDream/blob/RobustToolbox/LINUX_FAQ.md).

For more information or if you'd like to contribute, join our [Discord server](https://discord.gg/qreryhZxxs).

A detailed description of differences with BYOND can be found [here](https://github.com/wixoaGit/OpenDream/wiki/Differences-Between-OpenDream-and-BYOND).

## Building

The first step to building OpenDream is initializing the submodule for the game engine, [Robust Toolbox](https://github.com/space-wizards/RobustToolbox). To do this, simply run `git submodule update --init --recursive` in git bash and let it finish.

OpenDream requires .NET 5. To build, one can use a C# compiler (such as MSBuild) to compile the various projects described in the solution.

There's 3 main parts: Compiler, Server, and Client

## Running

**Compiler:** Run `DMCompiler.exe`, and pass any number of .dm or .dme files to compile as arguments.

**Server:** Run `OpenDreamServer.exe` and pass the compiled JSON file you got as a result of running the compiler above as an argument like this: `--cvar opendream.json_path=C:/path/to/compiler/output.json`

**Client:** OpenDream requires the Chromium Embedded Framework to render UIs, and it is not shipped with OpenDream yet. Follow these links to download for [Windows](https://cef-builds.spotifycdn.com/cef_binary_95.7.14%2Bg9f72f35%2Bchromium-95.0.4638.69_windows64_minimal.tar.bz2) or [Linux](https://cef-builds.spotifycdn.com/cef_binary_95.7.14%2Bg9f72f35%2Bchromium-95.0.4638.69_linux64_minimal.tar.bz2), or find it yourself [here](https://cef-builds.spotifycdn.com/index.html#linux32:95.7.14), using the minimal distribution for version `95.7.14`. After downloading CEF, move the entire contents of its Release and Resources folders to the same folder as your `OpenDreamClient.exe`. Finally, you can run `OpenDreamClient.exe`. You will be prompted to choose a server address, port, and username. The defaults should work for a locally hosted server.

## Screenshots
The following screenshots are taken from a stripped-down version of /tg/station available [here](https://github.com/wixoaGit/tgstation).

![](https://github.com/wixoaGit/OpenDream/blob/master/.github/assets/screenshot.png?raw=true)
![](https://github.com/wixoaGit/OpenDream/blob/master/.github/assets/screenshot2.png?raw=true)

[DM Language]: http://secure.byond.com/
