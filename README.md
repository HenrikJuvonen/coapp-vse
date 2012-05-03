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
		[ ] Search by name
		[X] Sort by name
		[|] Categories
			[ ] Active
				[ ] Dev
			[ ] Installed
				[ ] All
				[ ] Dev
			[|] Online
				[|] All
				[ ] Dev
			[|] Updates
				[|] All
				[ ] Dev
			[ ] Recent
				[ ] All
				[ ] Dev

		[ ] Activate Package (C#)
		[ ] Activate Package (C++)
		[ ] Packages.config, prompt required library installs

		[|] List Packages
		[|] Get Package Details
		[ ] Install Package
		[ ] Uninstall Package
		[ ] Update Packages
		[ ] Upgrade Packages
	
		[ ] Options
			[ ] Feed Configuration
				[|] List Feeds
				[ ] Add Feed
				[ ] Remove Feed
	
		[|] Console window (one instance)
		[ ] Update notifications
    
