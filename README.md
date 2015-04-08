# RvtVa3c

A Revit custom exporter add-in generating JSON output for the [vA3C](http://va3c.github.io) [three.js](http://threejs.org) AEC viewer.


## Setup, Compilation and Installation

RvtVa3c is a standard Revit add-in application.

It is installed in the standard manner, i.e., by copying two files to the standard Revit Add-Ins folder:

- The .NET assembly DLL RvtVa3c.dll
- The add-in manifest RvtVa3c.addin

In order to generate the DLL, you download and compile:

- Download the git directory RvtVa3c-gh-pages.
- Open the solution file RvtVa3c.sln in Visual Studio 2012 or later.
- Build solution locally:
    - Add references to the Revit API assembly files RevitAPI.dll and RevitAPIUI.dll, located in your Revit installation directory, e.g.,C:\Program Files\Autodesk\Revit Architecture 2015.
    - If you wish to debug, set up the path to the Revit executable in the Debug tab, Start External Program; change the path to your system installation, e.g.,C:\Program Files\Autodesk\Revit Architecture 2015\Revit.exe.
    - Build the solution.

This will open the Revit installation you referred to, and install the plugin, which can then be launched from the Revit Add-Ins tab.

And wonderfully exports the your Revit model to a JSON file.



## Tools and Technologies

* [three.js JavaScript 3D Library](https://github.com/mrdoob/three.js)
* [vA3C three.js AEC Viewer](http://va3c.github.io)


## Further Reading

* [AEC Hackathon – From the Midst of the Fray](http://thebuildingcoder.typepad.com/blog/2014/05/aec-hackathon-from-the-midst-of-the-fray.html)
* [RvtVa3c – Revit Va3c Generic AEC Viewer JSON Export](http://thebuildingcoder.typepad.com/blog/2014/05/rvtva3c-revit-va3c-generic-aec-viewer-json-export.html)
* [RvtVa3c Assembly Resolver](http://thebuildingcoder.typepad.com/blog/2014/05/rvtva3c-assembly-resolver.html)
* [Three.js AEC Viewer Progress](http://thebuildingcoder.typepad.com/blog/2014/08/threejs-aec-viewer-progress-on-two-fronts.html#4)
* [Integrating RvtVa3c into Three.js](http://thebuildingcoder.typepad.com/blog/2014/09/adn-labs-xtra-on-github-and-rvtva3c-in-threejs.html#5)
* [Custom User Settings Storage and RvtVa3c Update](http://thebuildingcoder.typepad.com/blog/2014/10/berlin-hackathon-results-3d-viewer-and-web-news.html#7)
* [RvtVa3c Enhancement Filters Parameters](http://thebuildingcoder.typepad.com/blog/2015/03/state-of-the-view-and-data-api-va3c-and-edge-ids.html#3)


## Wishlist

* Texture support
* Improved handling of normals to gracefully display non-planar surfaces


## Authors

Implemented by Matt Mason and Jeremy Tammik,
[The Building Coder](http://thebuildingcoder.typepad.com), Autodesk Inc.,
at the New York AEC Hackathon in May 2014.
