# OpenDream

OpenDream is a C# project that aims to compile games made in the [DM language], and run them.

This project is currently in early stages, and is not feature-complete.

## Building

To build, one can use a C# compiler (such as MSBuild) to compile the various projects described in the solution.

There's 3 main parts: Compiler, Server, and Client

## Running

**Compiler:** Run `DMCompiler.exe`, and pass any number of .dm or .dme files to compile as arguments.

**Server:** Run `OpenDreamServer.exe` and pass the compiled JSON file you got as a result of running the compiler above as the first argument.

**Client:** Run `OpenDreamClient.exe`. You will be prompted to choose a server address, port, and username. The defaults should work for a locally hosted server.

## Screenshots

![](https://github.com/wixoaGit/OpenDream/blob/master/screenshot.png?raw=true)
![](https://github.com/wixoaGit/OpenDream/blob/master/screenshot2.png?raw=true)

[DM Language]: http://secure.byond.com/