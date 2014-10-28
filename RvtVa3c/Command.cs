#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;
using DialogResult = System.Windows.Forms.DialogResult;
#endregion

namespace RvtVa3c
{
  [Transaction( TransactionMode.Manual )]
  public class Command : IExternalCommand
  {
    /// <summary>
    /// Custom assembly resolver to find our support
    /// DLL without being forced to place our entire 
    /// application in a subfolder of the Revit.exe
    /// directory.
    /// </summary>
    System.Reflection.Assembly
      CurrentDomain_AssemblyResolve(
        object sender,
        ResolveEventArgs args )
    {
      if( args.Name.Contains( "Newtonsoft" ) )
      {
        string filename = Path.GetDirectoryName(
          System.Reflection.Assembly
            .GetExecutingAssembly().Location );

        filename = Path.Combine( filename,
          "Newtonsoft.Json.dll" );

        if( File.Exists( filename ) )
        {
          return System.Reflection.Assembly
            .LoadFrom( filename );
        }
      }
      return null;
    }

    /// <summary>
    /// Export a given 3D view to JSON using
    /// our custom exporter context.
    /// </summary>
    void ExportView3D( View3D view3d, string filename )
    {
      AppDomain.CurrentDomain.AssemblyResolve
        += CurrentDomain_AssemblyResolve;

      Document doc = view3d.Document;

      Va3cExportContext context
        = new Va3cExportContext( doc, filename );

      CustomExporter exporter = new CustomExporter(
        doc, context );

      // Note: Excluding faces just suppresses the 
      // OnFaceBegin calls, not the actual processing 
      // of face tessellation. Meshes of the faces 
      // will still be received by the context.

      exporter.IncludeFaces = false;

      exporter.ShouldStopOnError = false;

      exporter.Export( view3d );
    }

    #region SelectFile
    /// <summary>
    /// Store the last user selected output folder
    /// in the current editing session.
    /// </summary>
    static string _output_folder_path = null;

    /// <summary>
    /// Return true is user selects and confirms
    /// output file name and folder.
    /// </summary>
    static bool SelectFile(
      ref string folder_path,
      ref string filename )
    {
      SaveFileDialog dlg = new SaveFileDialog();

      dlg.Title = "Select JSON Output File";
      dlg.Filter = "JSON files|*.js";

      if( null != folder_path
        && 0 < folder_path.Length )
      {
        dlg.InitialDirectory = folder_path;
      }

      dlg.FileName = filename;

      bool rc = DialogResult.OK == dlg.ShowDialog();

      if( rc )
      {
        filename = Path.Combine( dlg.InitialDirectory, 
          dlg.FileName );

        folder_path = Path.GetDirectoryName( 
          filename );
      }
      return rc;
    }
    #endregion // SelectFile

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      if( doc.ActiveView is View3D )
      {
        string filename = doc.PathName;
        if( 0 == filename.Length )
        {
          filename = doc.Title;
        }
        if( null == _output_folder_path )
        {
          _output_folder_path = Path.GetDirectoryName(
            filename );
        }
        filename = Path.GetFileName( filename ) + ".js";

        if( SelectFile( ref _output_folder_path,
          ref filename ) )
        {
          filename = Path.Combine( _output_folder_path,
            filename );

          ExportView3D( doc.ActiveView as View3D,
            filename );

          return Result.Succeeded;
        }
        return Result.Cancelled;
      }
      else
      {
        Util.ErrorMsg( 
          "You must be in a 3D view to export." );
      }
      return Result.Failed;
    }
  }
}
