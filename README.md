coapp-vsp-prototype
===================

Visual Studio plugin for CoApp packages

![Package Manager](coapp-vsp-prototype/raw/master/pkgmgr.png)

### Requirements

- CoApp.Toolkit 1.2.0.165 (for now)
- Visual Studio 2010 SP1
- Visual Studio 2010 SP1 SDK

### Notes

- Set startup project to "VsExtension" if it isn't.

### Progress

	[|] VsExtension
		[X] Search by name
		[X] Sort by name
		[|] Categories
			[ ] Project
				[ ] Project 1..n
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
			[ ] Add to project
				[ ] C#
				[ ] C++
			[ ] Remove from project
				[ ] C#
				[ ] C++
			[ ] Packages.config, prompt required library installs

		[X] List Packages
		[X] Get Package Details
		[ ] Install Package (Online)
		[ ] Uninstall Package (Installed)
		[ ] Update Packages (Updates)
	
		[|] Console window
		[ ] Update notifications
    
