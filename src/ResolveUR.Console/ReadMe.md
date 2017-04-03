## ResolveUR - Resolve Unused References

* * *

Update 8/2/2014: Visual studio console is published at [Visual Studio Gallery](http://visualstudiogallery.msdn.microsoft.com/faf25a06-0490-4eaf-82ab-c42b729a764e)

ResResolves project references for a Visual Studio Solution by removing unused references in each project of the solution.

This is done by removing a reference in a project, building project for errors and restoring removed reference if there were build errors.

Tested for few project types including console, windows, unit test and website project types.

Checks for MSBuild on the local system in predetermined paths specified in app.config. As the second argument, the platform to build against can be specified.

### Usage at commandline

With just path, looks for x64 .net v4.0 MSBuild, then x64 v3.5, x64 v2.0, x86 v4.0, x86 v3.5, x86 v2.0 Note: Path or platform arguments are without brackets

`ResolveUR.exe [Path of solution file]`

If NuGet packages are also to be resolved, add true/false next to path

`ResolveUR.exe [Path of solution file] [true/false]`

With platform also specified, x86, for example, looks only x86 .net MSBuild versions, highest first

`ResolveUR.exe [Path of solution file] [true/false] [platform]`

### Change and Version History
* 4/2/2017    - v3.1 - Added Nuget packages marked as development dependency to be excluded
* 4/2/2017    - v3.0 - Updated with a preview mode allowing exclusion and final confirmation before removal of references
* 12/17/2015  - v2.3 - Version for 2015
* 8/23/2014   - v2.2 - Make package solution optional since getting it right is much more involved, better left to developer discretion at this point. Plan to add a package developer edited exclusion list in future.
* 8/22/2014   - v2.1 - fixed regression bug. Resolution continued in spite of build errors
* 8/15/2014   - v2.0 - Remove NuGet package references as well as folders. v2.0
* 8/5/2014    - v1.3 - Fixed couple of bugs and permissions issue
* 8/2/2014    - Added setup project to install console app
* 8/1/2014    - v1.0 - VSIX project added and extension published
