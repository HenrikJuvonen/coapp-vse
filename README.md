CoApp for Visual Studio
=======================

CoApp for Visual Studio is an extension for managing CoApp-packages within Visual Studio. It makes it possible to add/remove libraries to Visual Studio -projects (both native C++ and .NET) easily with a GUI without opening a web browser or a file archiver.

The extension requires Visual Studio 2010 SP1 and the latest CoApp to work.

![Solution Explorer](https://github.com/henjuv/coapp-vse/blob/master/content/solutionexplorer.jpg?raw=true)

## Requirements

- CoApp.Toolkit 1.2.0.360
- Visual Studio 2010 SP1
- Visual Studio 2010 SP1 SDK

## Getting Started

- Run Visual Studio as administrator.
- Set startup project to "VsExtension".
- Go to "VsExtension" project properties and select Debug tab:
	- Start external program: C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe
	- Command line arguments: /rootsuffix Exp
- Build solution and start debugging.

## Progress
	
	| = work in progress
	X = ok

	[|] VsExtension
		[|] VisualStudio
			[|] Manage
				[|] Add package to solution
					[X] C++
					[ ] C#
				[|] Remove package from solution
					[X] C++
					[ ] C#
				[|] coapp.config
				[|] Show added packages in Solution-category
			[ ] Package restore

		[X] Search by name
		[X] Sort by name
		[X] Filter by architecture
		[|] Categories
			[|] Solution
				[|] All
			[X] Installed
				[X] All
				[X] Dev
			[X] Online
				[X] All
				[X] Dev
			[|] Updates
				[|] All
				[|] Dev
	
		[|] Options
			[X] Clear cache
			[X] Feed Configuration
				[X] List Feeds
				[X] Add Feed
				[X] Remove Feed

		[X] List Packages
		[X] Get Package Details
		[X] Install Package (Online)
		[X] Uninstall Package (Installed)
		[ ] Update Packages (Updates)
	
		[ ] Console window
		[ ] Update-notifications