#region Namespaces
using System;
using System.Windows.Interop;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Windows.Forms;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;
using DialogResult = System.Windows.Forms.DialogResult;




#endregion

namespace RvtVa3c
{
    [Transaction(TransactionMode.Manual)]
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
            ResolveEventArgs args)
        {
            if (args.Name.Contains("Newtonsoft"))
            {
                string filename = Path.GetDirectoryName(
                  System.Reflection.Assembly
                    .GetExecutingAssembly().Location);

                filename = Path.Combine(filename,
                  "Newtonsoft.Json.dll");

                if (File.Exists(filename))
                {
                    return System.Reflection.Assembly
                      .LoadFrom(filename);
                }
            }
            return null;
        }

        /// <summary>
        /// Export a given 3D view to JSON using
        /// our custom exporter context.
        /// </summary>
        public void ExportView3D(View3D view3d, string filename)
        {
            AppDomain.CurrentDomain.AssemblyResolve
              += CurrentDomain_AssemblyResolve;

            Document doc = view3d.Document;

            Va3cExportContext context
              = new Va3cExportContext(doc, filename);

            CustomExporter exporter = new CustomExporter(
              doc, context);

            // Note: Excluding faces just suppresses the 
            // OnFaceBegin calls, not the actual processing 
            // of face tessellation. Meshes of the faces 
            // will still be received by the context.

            exporter.IncludeFaces = false;

            exporter.ShouldStopOnError = false;

            exporter.Export(view3d);
        }



        public static ParameterFilter _filter;
        public static bool _filterParameters = false;
        public static TabControl _tabControl;
        public static Dictionary<string, List<string>> _parameterDictionary;
        public static Dictionary<string, List<string>> _toExportDictionary;

        public void filterElementParameters(Document doc)
        {
            _parameterDictionary = new Dictionary<string, List<string>>();
            _toExportDictionary = new Dictionary<string, List<string>>();
            //get all the family instances in the document
            FilteredElementCollector collector
                = new FilteredElementCollector(doc).
                OfClass(typeof(FamilyInstance));

            // create a dictionary with all the properties for each object
            foreach (FamilyInstance fi in collector)
            {
                string category = fi.Category.Name;
                // skip these categories, do not show them in the form
                if (category != "Title Blocks" && category != "Generic Annotations" && category != "Detail Items")
                {
                    IList<Parameter> parameters = fi.GetOrderedParameters();
                    List<string> parameterNames = new List<string>();

                    foreach (Parameter p in parameters)
                    {

                        string pName = p.Definition.Name;
                        string tempVal = "";

                        if (StorageType.String == p.StorageType)
                        {
                            tempVal = p.AsString();
                        }
                        else
                        {
                            tempVal = p.AsValueString();
                        }
                        if (!string.IsNullOrEmpty(tempVal))
                        {
                            if (_parameterDictionary.ContainsKey(category))
                            {
                                if (!_parameterDictionary[category].Contains(pName))
                                {
                                    _parameterDictionary[category].Add(pName);
                                }
                            }
                            else
                            {
                                parameterNames.Add(pName);
                            }
                        }
                    }
                    if (parameterNames.Count > 0)
                    {
                        _parameterDictionary.Add(category, parameterNames);
                    }
                }
            }

            //CREATE FILTER UI
            _filter = new ParameterFilter();

            _tabControl = new TabControl();
            _tabControl.Size = new System.Drawing.Size(420, 220);
            foreach (string c in _parameterDictionary.Keys)
            {
                //Create a checklist
                CheckedListBox checkList = new CheckedListBox();
                //set the properties of the checklist
                checkList.Size = new System.Drawing.Size(400, 200);
                checkList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
                checkList.MultiColumn = true;
                checkList.ColumnWidth = 200;
                checkList.CheckOnClick = true;
                checkList.BorderStyle = System.Windows.Forms.BorderStyle.None;
                checkList.HorizontalScrollbar = false;

                checkList.Items.AddRange(_parameterDictionary[c].ToArray());

                for (int i = 0; i <= (checkList.Items.Count - 1); i++)
                {
                    checkList.SetItemCheckState(i, CheckState.Checked);
                }

                //add A tab
                TabPage tab = new TabPage(c);
                tab.Name = c;

                //attach the checklist to the tab
                tab.Controls.Add(checkList);

                // attach the tab to the tab control
                _tabControl.TabPages.Add(tab);
            }

            //attach the tab control to the filter form
            _filter.Controls.Add(_tabControl);


            //DISPLAY FILTER UI
            _filter.ShowDialog();

            //loop thru each tab
            foreach (TabPage tab in _tabControl.TabPages)
            {
                List<string> parametersToExport = new List<string>();
                foreach (var checkedP in ((CheckedListBox)tab.Controls[0]).CheckedItems)
                {
                    parametersToExport.Add(checkedP.ToString());
                }

                _toExportDictionary.Add(tab.Name, parametersToExport);
            }


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
          ref string filename)
        {
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Title = "Select JSON Output File";
            dlg.Filter = "JSON files|*.js";

            if (null != folder_path
              && 0 < folder_path.Length)
            {
                dlg.InitialDirectory = folder_path;
            }

            dlg.FileName = filename;

            bool rc = DialogResult.OK == dlg.ShowDialog();

            if (rc)
            {
                filename = Path.Combine(dlg.InitialDirectory,
                  dlg.FileName);

                folder_path = Path.GetDirectoryName(
                  filename);
            }
            return rc;
        }
        #endregion // SelectFile




        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;
            Document doc = uidoc.Document;

            if (doc.ActiveView is View3D)
            {
                TaskDialog td = new TaskDialog("Ask user to filter parameters");
                td.Title = "Filter parameters";
                td.CommonButtons = TaskDialogCommonButtons.No | TaskDialogCommonButtons.Yes;
                td.MainInstruction = "Do you want to filter the parameters to be exported?";
                td.MainContent = "Click Yes and you will be able to select parameters for each category in the next window";
                td.AllowCancellation = true;
                TaskDialogResult tdResult = td.Show();

                if (tdResult == TaskDialogResult.Yes)
                {
                   // Call the filter
                    filterElementParameters(doc);
                    _filterParameters = true;
                    if (ParameterFilter.status == "cancelled") return Result.Cancelled;
                }

                string filename = doc.PathName;
                if (0 == filename.Length)
                {
                    filename = doc.Title;
                }
                if (null == _output_folder_path)
                {
                    _output_folder_path = Path.GetDirectoryName(
                      filename);
                }
                filename = Path.GetFileName(filename) + ".js";

                if (SelectFile(ref _output_folder_path,
                  ref filename))
                {
                    filename = Path.Combine(_output_folder_path,
                      filename);

                    ExportView3D(doc.ActiveView as View3D,
                      filename);

                    return Result.Succeeded;
                }
                return Result.Cancelled;


            }
            else
            {
                Util.ErrorMsg(
                  "You must be in a 3D view to export.");
            }
            return Result.Failed;
        }
    }
}
