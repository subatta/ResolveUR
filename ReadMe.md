<h2>ResolveUR - Resolve Unused References</h2>
<hr />
<p>
    Update 8/1/2014: Visual studio extension is published at <a href="http://visualstudiogallery.msdn.microsoft.com/fe96c042-9a83-4fa2-921d-6b09aa044315">Visual Studio Gallery</a>
</p>
<p>
    Update 8/2/2014: Visual studio console is published at <a href="http://visualstudiogallery.msdn.microsoft.com/faf25a06-0490-4eaf-82ab-c42b729a764e">Visual Studio Gallery</a>
</p>
<p>
    Resolves project references for a Visual Studio Solution by <b>removing unused references</b> in each project of the solution.
</p>
<p>
    This is done by removing a reference in a project, building project for errors and restoring removed reference if there were build errors.
</p>
<p>
    Tested for few project types including console, windows, unit test and website project types.
</p>
<p>
    Checks for MSBuild on local system in predetermined paths specified in app.config. As second argument, platform to build against can be specified.
</p>

<h3>Usage at commandline</h3>
<p>
    With just path, looks for x64 .net v4.0 msbuild, then x64 v3.5, x64 v2.0, x86 v4.0, x86 v3.5, x86 v2.0
    Note: Path or platform arguments are without brackets
</p>
<p>
    <code>
        ResolveUR.exe [Path of solution file]
    </code>
</p>
<p>
    If nuget packages are also to be resolved, add true/false next to path
</p>
<p>
    <code>
        ResolveUR.exe [Path of solution file] [true/false]
    </code>
</p>
<p>
    With platform also specified, x86 for example looks only x86 .net msbuild versions, highest first
</p>
<p>
    <code>
        ResolveUR.exe [Path of solution file] [true/false] [platform]
    </code>
</p>

<h3>Change and Version History</h3>
<ul>
    <li>8/23/2014 - v2.2 - Make package solution optional since getting it right is much more involved, better left to developer discretion at this point. Plan to add a package developer edited exclusion list in future.</li>
    <li>8/22/2014 - v2.1 - fixed regression bug. Resolution continued in spit of build errors
    <li>8/15/2014 - v2.0 - Remove nuget package references as well as folders. v2.0</li>
    <li>8/5/2014 - v1.3 - Fixed couple of bugs and permissions issue</li>
    <li>8/2/2014 - Added setup project to install console app</li>
    <li>8/1/2014 - v1.0 - VSIX project added and extension published</li>
</ul>