CoApp for Visual Studio
=======================

CoApp for Visual Studio is an extension for managing CoApp-packages inside Visual Studio. Both native C++ and .NET packages can be added to projects.

## Requirements

- CoApp
- MahApps.Metro
- Visual Studio 2010/2012
- Visual Studio 2010/2012 SDK

## Build with pTk

- Have coapp.devtools installed and setup properly
- Open Visual Studio command prompt as Administrator and change directory to the solution directory
- "ptk package"

## Debug with Visual Studio

- Set startup project to "VsExtension".
- Go to "VsExtension" project properties and select Debug tab:
	- Start external program: C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe
	- Command line arguments: /rootsuffix Exp
- Build solution and start debugging.