CoApp for Visual Studio
=======================

CoApp for Visual Studio is an extension for managing CoApp-packages inside Visual Studio. Both native C++ and .NET packages can be added to projects.

## Requirements

- CoApp 1.2.0.443
- Visual Studio 2010 SP1 or Visual Studio 2012
- Visual Studio 2010 SP1 SDK

## Getting Started

- Run Visual Studio as administrator.
- Set startup project to "VsExtension".
- Go to "VsExtension" project properties and select Debug tab:
	- Start external program: C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe
	- Command line arguments: /rootsuffix Exp
- Build solution and start debugging.