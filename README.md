coapp-vsp-prototype
===================

Visual Studio plugin for CoApp packages

![Package Manager](coapp-vsp-prototype/raw/master/pkgmgr.png)

### Requirements

- CoApp.Toolkit 1.2.0.165 (for now)
- Visual Studio 2010 SP1
- Visual Studio 2010 SP1 SDK

### Debugging

- Run Visual Studio as administrator.
- Set startup project to "VsExtension".
- Go to "VsExtension" project properties and select Debug tab:
	- Start external program: C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe
	- Command line arguments: /rootsuffix Exp
- Build project and start debugging

### Progress

	[|] VsExtension
		[X] Search by name
		[X] Sort by name
		[|] Categories
			[ ] Solution
				[ ] All
			[|] Installed
				[|] All
				[ ] Dev
			[|] Online
				[|] All
				[ ] Dev
			[|] Updates
				[|] All
				[ ] Dev
	
		[ ] Options
			[ ] Feed Configuration
				[|] List Feeds
				[ ] Add Feed
				[ ] Remove Feed
		
		[ ] VisualStudio
			[ ] Add package to solution
				[ ] C#
				[ ] C++
			[ ] Remove package from solution
				[ ] C#
				[ ] C++
			[ ] Packages.config
			[ ] Dialog: "Solution requires <list of packages>, install?"

		[X] List Packages
		[X] Get Package Details
		[X] Install Package (Online)
		[X] Uninstall Package (Installed)
		[ ] Update Packages (Updates)
	
		[ ] Console window
		[ ] Update notifications
    
