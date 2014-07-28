ResolveUR - Resolve Unused References
-------------------------------------

Resolves project references for a Visual Studio Solution by <b>removing unused references</b> in each project of the solution.

This id done by removing a reference in a project, building project for errors and restoring removed reference if there were build errors.

Tested for few project types including console, windows, unit test and website project types.

Checks for MSBuild on local system in predetermined paths specified in app.config. As second argument, platform to build against can be specified.

Usage at commandline:

With just path, looks for x64 .net v4.0 msbuild, then x64 v3.5, x64 v2.0, x86 v4.0, x86 v3.5, x86 v2.0
Note: Path or platform arguments are without brackets

ResolveUR.exe [Path of solution file]


With platform also specified, x86 for example looks only x86 .net msbuild versions, highest first
ResolveUR.exe [Path of solution file] [platform]