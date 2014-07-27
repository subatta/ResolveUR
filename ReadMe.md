Resolves project references for a Visual Studio Solution by <b>removing unused references</b> in each project of the solution.

Done by removing a reference in a project, building project for errors and restoring the removed reference if there were build errors.

Tested for few project types including console, windows, unit test and website project types.

Usage at commandline: ReferenceResolution.exe [Path of solution file]