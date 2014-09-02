#region Namespaces
using System;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Reflection;
#endregion

namespace RvtVa3c
{
  class App : IExternalApplication
  {
    /// <summary>
    /// Add buttons for our three commands 
    /// to the ribbon panel.
    /// </summary>
    void PopulatePanel( RibbonPanel p )
    {
      string path = Assembly.GetExecutingAssembly()
        .Location;

      RibbonItemData i1 = new PushButtonData(
          "RvtVa3c_Command", "vA3C \r\n Export",
          path, "RvtVa3c.Command" );

      i1.ToolTip = "Export three.js JSON objects "
        + "for vA3C AEC viewer";

      //p.AddStackedItems( i1, i2, i3 );

      p.AddItem( i1 );
    }

    public Result OnStartup( UIControlledApplication a )
    {
      PopulatePanel(
        a.CreateRibbonPanel(
          "vA3C Export" ) );

      return Result.Succeeded;
    }

    public Result OnShutdown( UIControlledApplication a )
    {
      return Result.Succeeded;
    }
  }
}
