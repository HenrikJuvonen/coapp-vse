coapp-vse 0.1.0
===============

coapp-vse is an open source Visual Studio extension written in C# (.NET 4.0). It makes it possible to
install, uninstall, update and manage CoApp-packages with a GUI within Visual Studio. The goal is to
provide an easy way to add developer-libraries to Visual Studio -projects (both C++ and .NET).
It is currently work in progress and it is not ready for everyday use.

![Solution Explorer](https://github.com/henjuv/coapp-vse/blob/master/select.png?raw=true)

## Requirements

- CoApp.Toolkit 1.2.0.165 (for now)
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
				[|] Packages.config
				[|] Show added packages in Solution-category (parse each project's Packages.config)
			[ ] Dialog: "Solution requires <list of packages>, install?"

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
			[X] Updates
				[X] All
				[X] Dev
	
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