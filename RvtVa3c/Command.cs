#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Interop;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Windows.Forms;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;
using DialogResult = System.Windows.Forms.DialogResult;
#endregion // Namespaces

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
    public void ExportView3D( 
      View3D view3d, 
      string filename )
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
      //
      //exporter.IncludeFaces = false; // removed in Revit 2017

      exporter.ShouldStopOnError = false;

      exporter.Export( view3d );
    }

    #region UI to Filter Parameters
    public static ParameterFilter _filter;
    public static TabControl _tabControl;
    public static Dictionary<string, List<string>> _parameterDictionary;
    public static Dictionary<string, List<string>> _toExportDictionary;

    /// <summary>
    /// Function to filter the parameters of the objects in the scene
    /// </summary>
    /// <param name="doc">Revit Document</param>
    /// <param name="includeType">Include Type Parameters in the filter dialog</param>
    public void filterElementParameters( Document doc, bool includeType )
    {
      _parameterDictionary = new Dictionary<string, List<string>>();
      _toExportDictionary = new Dictionary<string, List<string>>();

      FilteredElementCollector collector = new FilteredElementCollector( doc, doc.ActiveView.Id );

      // Create a dictionary with all the properties for each category.

      foreach( var fi in collector )
      {

        string category = fi.Category.Name;

        if( category != "Title Blocks" && category != "Generic Annotations" && category != "Detail Items" && category != "Cameras" )
        {
          IList<Parameter> parameters = fi.GetOrderedParameters();
          List<string> parameterNames = new List<string>();

          foreach( Parameter p in parameters )
          {
            string pName = p.Definition.Name;
            string tempVal = "";

            if( StorageType.String == p.StorageType )
            {
              tempVal = p.AsString();
            }
            else
            {
              tempVal = p.AsValueString();
            }
            if( !string.IsNullOrEmpty( tempVal ) )
            {
              if( _parameterDictionary.ContainsKey( category ) )
              {
                if( !_parameterDictionary[category].Contains( pName ) )
                {
                  _parameterDictionary[category].Add( pName );
                }
              }
              else
              {
                parameterNames.Add( pName );
              }
            }
          }
          if( parameterNames.Count > 0 )
          {
            _parameterDictionary.Add( category, parameterNames );
          }
          if( includeType )
          {
            ElementId idType = fi.GetTypeId();

            if( ElementId.InvalidElementId != idType )
            {
              Element typ = doc.GetElement( idType );
              parameters = typ.GetOrderedParameters();
              List<string> parameterTypes = new List<string>();
              foreach( Parameter p in parameters )
              {
                string pName = "Type " + p.Definition.Name;
                string tempVal = "";
                if( !_parameterDictionary[category].Contains( pName ) )
                {
                  if( StorageType.String == p.StorageType )
                  {
                    tempVal = p.AsString();
                  }
                  else
                  {
                    tempVal = p.AsValueString();
                  }

                  if( !string.IsNullOrEmpty( tempVal ) )
                  {
                    if( _parameterDictionary.ContainsKey( category ) )
                    {
                      if( !_parameterDictionary[category].Contains( pName ) )
                      {
                        _parameterDictionary[category].Add( pName );
                      }
                    }
                    else
                    {
                      parameterTypes.Add( pName );
                    }
                  }
                }
              }
              if( parameterTypes.Count > 0 )
              {
                _parameterDictionary[category].AddRange( parameterTypes );
              }
            }

          }
        }
      }

      // Create filter UI.

      _filter = new ParameterFilter();

      _tabControl = new TabControl();
      _tabControl.Size = new System.Drawing.Size( 600, 375 );
      _tabControl.Location = new System.Drawing.Point( 0, 55 );
      _tabControl.Anchor = ( (System.Windows.Forms.AnchorStyles) ( ( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom )
          | System.Windows.Forms.AnchorStyles.Left )
          | System.Windows.Forms.AnchorStyles.Right ) ) );

      int j = 8;

      // Populate the parameters as a checkbox in each tab
      foreach( string c in _parameterDictionary.Keys )
      {
        //Create a checklist
        CheckedListBox checkList = new CheckedListBox();

        //set the properties of the checklist
        checkList.Anchor = ( (System.Windows.Forms.AnchorStyles) ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right ) ) );
        checkList.FormattingEnabled = true;
        checkList.HorizontalScrollbar = true;
        checkList.Items.AddRange( _parameterDictionary[c].ToArray() );
        checkList.MultiColumn = true;
        checkList.Size = new System.Drawing.Size( 560, 360 );
        checkList.ColumnWidth = 200;
        checkList.CheckOnClick = true;
        checkList.TabIndex = j;
        j++;

        for( int i = 0; i <= ( checkList.Items.Count - 1 ); i++ )
        {
          checkList.SetItemCheckState( i, CheckState.Checked );
        }

        //add a tab
        TabPage tab = new TabPage( c );
        tab.Name = c;

        //attach the checklist to the tab
        tab.Controls.Add( checkList );

        // attach the tab to the tab control
        _tabControl.TabPages.Add( tab );
      }

      // Attach the tab control to the filter form

      _filter.Controls.Add( _tabControl );

      // Display filter ui

      _filter.ShowDialog();

      // Loop thru each tab and get the parameters to export

      foreach( TabPage tab in _tabControl.TabPages )
      {
        List<string> parametersToExport = new List<string>();
        foreach( var checkedP in ( (CheckedListBox) tab.Controls[0] ).CheckedItems )
        {
          parametersToExport.Add( checkedP.ToString() );
        }
        _toExportDictionary.Add( tab.Name, parametersToExport );
      }
    }
    #endregion // UI to Filter Parameters

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
      Autodesk.Revit.ApplicationServices.Application app 
        = uiapp.Application;
      Document doc = uidoc.Document;

      // Check that we are in a 3D view.

      View3D view = doc.ActiveView as View3D;

      if( null == view )
      {
        Util.ErrorMsg(
          "You must be in a 3D view to export." );

        return Result.Failed;
      }

      // Prompt for output filename selection.

      string filename = doc.PathName;

      if( 0 == filename.Length )
      {
        filename = doc.Title;
      }

      if( null == _output_folder_path )
      {
        // Sometimes the command fails if the file is 
        // detached from central and not saved locally

        try
        {
          _output_folder_path = Path.GetDirectoryName(
            filename );
        }
        catch
        {
          TaskDialog.Show( "Folder not found",
            "Please save the file and run the command again." );
          return Result.Failed;
        }
      }

      filename = Path.GetFileName( filename ) + ".js";

      if( !SelectFile( ref _output_folder_path,
        ref filename ) )
      {
        return Result.Cancelled;
      }

      filename = Path.Combine( _output_folder_path, 
        filename );

      // Ask user whether to interactively choose 
      // which parameters to export or just export 
      // them all.

      TaskDialog td = new TaskDialog( "Ask user to filter parameters" );
      td.Title = "Filter parameters";
      td.CommonButtons = TaskDialogCommonButtons.No | TaskDialogCommonButtons.Yes;
      td.MainInstruction = "Do you want to filter the parameters of the objects to be exported?";
      td.MainContent = "Click Yes and you will be able to select parameters for each category in the next window";
      td.AllowCancellation = true;
      td.VerificationText = "Check this to include type properties";

      if( TaskDialogResult.Yes == td.Show() )
      {
        filterElementParameters( doc, td.WasVerificationChecked() );
        if( ParameterFilter.status == "cancelled" )
        {
          ParameterFilter.status = "";
          return Result.Cancelled;
        }
      }

      // Save file.

      ExportView3D( doc.ActiveView as View3D,
        filename );

      return Result.Succeeded;
    }
  }
}
