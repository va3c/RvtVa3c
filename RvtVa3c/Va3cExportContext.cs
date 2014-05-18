#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using Autodesk.Revit.DB;
using System.Text;
using Autodesk.Revit.Utility;
using System.Xml.Linq;
using System.Dynamic;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
#endregion

namespace RvtVa3c
{
  // Todo:
  // Implement the external application button
  // Implement element properties
  // Instance/type reuse
  // Check instance transformation
  // Check for file size
  // Add scaling for Theo [(0,0),(20000,20000)]
  // Eliminate multiple materials 
  // Support transparency

  class Va3cExportContext : IExportContext
  {
    string _output_folder_path = "C:/a/vs/RvtVa3c/models/";

    /// <summary>
    /// If true, switch Y and Z coordinate 
    /// and flip X to negative.
    /// </summary>
    bool _switch_coordinates = true;

    #region VertexLookupXyz
    /// <summary>
    /// A vertex lookup class to eliminate 
    /// duplicate vertex definitions.
    /// </summary>
    class VertexLookupXyz : Dictionary<XYZ, int>
    {
      #region XyzEqualityComparer
      /// <summary>
      /// Define equality for Revit XYZ points.
      /// Very rough tolerance, as used by Revit itself.
      /// </summary>
      class XyzEqualityComparer : IEqualityComparer<XYZ>
      {
        const double _sixteenthInchInFeet
          = 1.0 / ( 16.0 * 12.0 );

        public bool Equals( XYZ p, XYZ q )
        {
          return p.IsAlmostEqualTo( q,
            _sixteenthInchInFeet );
        }

        public int GetHashCode( XYZ p )
        {
          return Util.PointString( p ).GetHashCode();
        }
      }
      #endregion // XyzEqualityComparer

      public VertexLookupXyz()
        : base( new XyzEqualityComparer() )
      {
      }

      /// <summary>
      /// Return the index of the given vertex,
      /// adding a new entry if required.
      /// </summary>
      public int AddVertex( XYZ p )
      {
        return ContainsKey( p )
          ? this[p]
          : this[p] = Count;
      }
    }
    #endregion // VertexLookupXyz

    #region VertexLookupInt
    /// <summary>
    /// An integer-based 3D point class.
    /// </summary>
    class PointInt : IComparable<PointInt>
    {
      public long X { get; set; }
      public long Y { get; set; }
      public long Z { get; set; }

      //public PointInt( int x, int y, int z )
      //{
      //  X = x;
      //  Y = y;
      //  Z = z;
      //}

      /// <summary>
      /// Consider a Revit length zero 
      /// if is smaller than this.
      /// </summary>
      const double _eps = 1.0e-9;

      /// <summary>
      /// Conversion factor from feet to millimetres.
      /// </summary>
      const double _feet_to_mm = 25.4 * 12;

      /// <summary>
      /// Conversion a given length value 
      /// from feet to millimetre.
      /// </summary>
      static long ConvertFeetToMillimetres( double d )
      {
        if( 0 < d )
        {
          return _eps > d
            ? 0
            : (long) ( _feet_to_mm * d + 0.5 );

        }
        else
        {
          return _eps > -d
            ? 0
            : (long) ( _feet_to_mm * d - 0.5 );

        }
      }

      public PointInt( XYZ p, bool switch_coordinates )
      {
        X = ConvertFeetToMillimetres( p.X );
        Y = ConvertFeetToMillimetres( p.Y );
        Z = ConvertFeetToMillimetres( p.Z );

        if( switch_coordinates )
        {
          X = -X;
          long tmp = Y;
          Y = Z;
          Z = tmp;
        }
      }

      public int CompareTo( PointInt a )
      {
        long d = X - a.X;

        if( 0 == d )
        {
          d = Y - a.Y;

          if( 0 == d )
          {
            d = Z - a.Z;
          }
        }
        return ( 0 == d ) ? 0 : ( ( 0 < d ) ? 1 : -1 );
      }
    }

    /// <summary>
    /// A vertex lookup class to eliminate 
    /// duplicate vertex definitions.
    /// </summary>
    class VertexLookupInt : Dictionary<PointInt, int>
    {
      #region PointIntEqualityComparer
      /// <summary>
      /// Define equality for integer-based PointInt.
      /// </summary>
      class PointIntEqualityComparer : IEqualityComparer<PointInt>
      {
        public bool Equals( PointInt p, PointInt q )
        {
          return 0 == p.CompareTo( q );
        }

        public int GetHashCode( PointInt p )
        {
          return ( p.X.ToString()
            + "," + p.Y.ToString()
            + "," + p.Z.ToString() )
            .GetHashCode();
        }
      }
      #endregion // PointIntEqualityComparer

      public VertexLookupInt()
        : base( new PointIntEqualityComparer() )
      {
      }

      /// <summary>
      /// Return the index of the given vertex,
      /// adding a new entry if required.
      /// </summary>
      public int AddVertex( PointInt p )
      {
        return ContainsKey( p )
          ? this[p]
          : this[p] = Count;
      }
    }
    #endregion // VertexLookupInt

    Document _doc;
    Va3cScene _scene;
    //VertexLookupXyz _vertices;
    VertexLookupInt _vertices;
    Dictionary<string, Va3cScene.Va3cMaterial> _materials;
    Dictionary<string, Va3cScene.Va3cObject> _objects;
    Dictionary<string, Va3cScene.Va3cGeometry> _geometries;

    Va3cScene.Va3cObject _currentObject = null;
    Va3cScene.Va3cGeometry _currentGeometry = null;

    Stack<ElementId> _elementStack = new Stack<ElementId>();
    Stack<Transform> _transformationStack = new Stack<Transform>();

    string _currentMaterialUid;

    Dictionary<uint, ElementId> polymeshToMaterialId = new Dictionary<uint, ElementId>();

    public uint CurrentPolymeshIndex { get; set; }

    ElementId CurrentElementId
    {
      get
      {
        return ( _elementStack.Count > 0 )
          ? _elementStack.Peek()
          : ElementId.InvalidElementId;
      }
    }

    Element CurrentElement
    {
      get
      {
        return _doc.GetElement( CurrentElementId );
      }
    }

    Transform CurrentTransform
    {
      get
      {
        return _transformationStack.Peek();
      }
    }

    /// <summary>
    /// Set the current material
    /// </summary>
    void SetCurrentMaterial( string uidMaterial )
    {
      if( !_materials.ContainsKey( uidMaterial ) )
      {
        Material material = _doc.GetElement(
          uidMaterial ) as Material;

        Va3cScene.Va3cMaterial m
          = new Va3cScene.Va3cMaterial();

        //m.metadata = new Va3cScene.Va3cMaterialMetadata();
        //m.metadata.type = "material";
        //m.metadata.version = 4.2;
        //m.metadata.generator = "RvtVa3c 2015.0.0.0";

        m.uuid = uidMaterial;
        m.type = "MeshPhongMaterial";
        m.color = Util.ColorToInt( material.Color );
        m.ambient = m.color;
        m.emissive = 0;
        m.specular = m.color;
        m.shininess = material.Shininess; // todo: does this need scaling to e.g. [0,100]?
        m.opacity = 1; // 128 - material.Transparency;
        m.transparent = false;
        m.wireframe = false;

        _materials.Add( uidMaterial, m );
      }
      _currentMaterialUid = uidMaterial;
    }

    public Va3cExportContext( Document document )
    {
      _doc = document;
    }

    public bool Start()
    {
      //_faces = new List<FaceMaterial>();
      _materials = new Dictionary<string, Va3cScene.Va3cMaterial>();
      _vertices = new VertexLookupInt();
      _geometries = new Dictionary<string, Va3cScene.Va3cGeometry>();
      _objects = new Dictionary<string, Va3cScene.Va3cObject>();

      _transformationStack.Push( Transform.Identity );

      _scene = new Va3cScene();
      //_scene = new ExpandoObject();

      _scene.metadata = new Va3cScene.SceneMetadata();
      _scene.metadata.type = "Object";
      //_scene.metadata.colors = 0;
      //_scene.metadata.faces = 0;
      _scene.metadata.version = 4.3;
      _scene.metadata.generator = "RvtVa3c Revit va3c exporter";
      //_scene.metadata.materials = 0;
      _scene.geometries = new List<Va3cScene.Va3cGeometry>();

      _scene.obj = new Va3cScene.Va3cObject();
      _scene.obj.uuid = _doc.ActiveView.UniqueId;
      _scene.obj.type = "Scene";
      _scene.obj.matrix = new double[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };

      return true;
    }

    public void Finish()
    {
      // Finish populating scene

      //_scene.metadata.materials = _materials.Count;
      _scene.materials = _materials.Values.ToList();

      _scene.geometries = _geometries.Values.ToList();

      _scene.obj.children = _objects.Values.ToList();

      // Serialise scene

      string filename = _output_folder_path + _doc.Title + ".js";

      //using( FileStream stream
      //  = File.OpenWrite( filename ) )
      //{
      //  DataContractJsonSerializer serialiser
      //    = new DataContractJsonSerializer(
      //      typeof( Va3cScene ) );

      //  serialiser.WriteObject( stream, _scene );
      //}

      JsonSerializerSettings settings 
        = new JsonSerializerSettings();

      settings.NullValueHandling 
        = NullValueHandling.Ignore;

      File.WriteAllText( filename, 
        JsonConvert.SerializeObject( _scene, 
          Formatting.Indented, settings ) );

#if USE_DYNAMIC_JSON
      // This saves the whole hassle of explicitly 
      // defining a whole hierarchy of C# classes
      // to serialise to JSON - do it all on the 
      // fly instead.

      // https://github.com/va3c/GHva3c/blob/master/GHva3c/GHva3c/va3c_geometry.cs

      dynamic jason = new ExpandoObject();

      //populate object properties

      jason.geometry = new ExpandoObject();
      jason.groups = new object[0];
      jason.material = matName;
      jason.position = new object[3];
      jason.position[0] = 0; jason.position[1] = 0; jason.position[2] = 0;
      jason.rotation = new object[3];
      jason.rotation[0] = 0; jason.rotation[1] = 0; jason.rotation[2] = 0;
      jason.quaternion = new object[4];
      jason.quaternion[0] = 0; jason.quaternion[1] = 0; jason.quaternion[2] = 0; jason.quaternion[3] = 0;
      jason.scale = new object[3];
      jason.scale[0] = 1; jason.scale[1] = 1; jason.scale[2] = 1;
      jason.visible = true;
      jason.castShadow = true;
      jason.receiveShadow = false;
      jason.doubleSided = true;


      //populate geometry object
      jason.geometry.metadata = new ExpandoObject();
      jason.geometry.metadata.version = 3.1;
      jason.geometry.metadata.generatedBy = "RvtVa3c Revit va3c exporter";
      jason.geometry.metadata.vertices = mesh.Vertices.Count;
      jason.geometry.metadata.faces = mesh.Faces.Count;
      jason.geometry.metadata.normals = 0;
      jason.geometry.metadata.colors = 0;
      jason.geometry.metadata.uvs = 0;
      jason.geometry.metadata.materials = 0;
      jason.geometry.metadata.morphTargets = 0;
      jason.geometry.metadata.bones = 0;

      jason.geometry.scale = 1.000;
      jason.geometry.materials = new object[0];
      jason.geometry.vertices = new object[mesh.Vertices.Count * 3];
      jason.geometry.morphTargets = new object[0];
      jason.geometry.normals = new object[0];
      jason.geometry.colors = new object[0];
      jason.geometry.uvs = new object[0];
      jason.geometry.faces = new object[mesh.Faces.Count * 3];
      jason.geometry.bones = new object[0];
      jason.geometry.skinIndices = new object[0];
      jason.geometry.skinWeights = new object[0];
      jason.geometry.animation = new ExpandoObject();

      //populate vertices
      int counter = 0;
      int i = 0;
      foreach( var v in mesh.Vertices )
      {
        jason.geometry.vertices[counter++] = mesh.Vertices[i].X;
        jason.geometry.vertices[counter++] = mesh.Vertices[i].Y;
        jason.geometry.vertices[counter++] = mesh.Vertices[i].Z;
        i++;
      }

      //populate faces
      counter = 0;
      i = 0;
      foreach( var f in mesh.Faces )
      {
        jason.geometry.faces[counter++] = mesh.Faces[i].A;
        jason.geometry.faces[counter++] = mesh.Faces[i].B;
        jason.geometry.faces[counter++] = mesh.Faces[i].C;
        i++;
      }

      return JsonConvert.SerializeObject( jason );
#endif // USE_DYNAMIC_JSON

      //_scene.metadata.
    }

    public void OnPolymesh( PolymeshTopology polymesh )
    {
      Debug.WriteLine( string.Format(
        "    OnPolymesh: {0} points, {1} facets, {2} normals {3}",
        polymesh.NumberOfPoints,
        polymesh.NumberOfFacets,
        polymesh.NumberOfNormals,
        polymesh.DistributionOfNormals ) );

      IList<XYZ> pts = polymesh.GetPoints();

      Transform t = CurrentTransform;

      pts = pts.Select( p => t.OfPoint( p ) ).ToList();

      int i = 0, v1, v2, v3;

      foreach( PolymeshFacet facet
        in polymesh.GetFacets() )
      {
        Debug.WriteLine( string.Format(
          "      {0}: {1} {2} {3}", i++,
          facet.V1, facet.V2, facet.V3 ) );

        v1 = _vertices.AddVertex( new PointInt(
          pts[facet.V1], _switch_coordinates ) );

        v2 = _vertices.AddVertex( new PointInt(
          pts[facet.V2], _switch_coordinates ) );

        v3 = _vertices.AddVertex( new PointInt(
          pts[facet.V3], _switch_coordinates ) );

        _currentGeometry.data.faces.Add( 0 );
        _currentGeometry.data.faces.Add( v1 );
        _currentGeometry.data.faces.Add( v2 );
        _currentGeometry.data.faces.Add( v3 );
      }
    }

    public void OnMaterial( MaterialNode node )
    {
      Debug.WriteLine( "     --> On Material: " + node.MaterialId + ": " + node.NodeName );
      // OnMaterial method can be invoked for every 
      // single out-coming mesh even when the material 
      // has not actually changed. Thus it is usually
      // beneficial to store the current material and 
      // only get its attributes when the material 
      // actually changes.

      ElementId id = node.MaterialId;
      string uid;

      if( ElementId.InvalidElementId != id )
      {
        Element m = _doc.GetElement( node.MaterialId );
        uid = m.UniqueId;

        SetCurrentMaterial( m.UniqueId );
      }
      else
      {
        // Todo: generate a GUID based on colour, 
        // transparency, etc. to avoid duplicating
        // non-element material definitions.

        uid = Guid.NewGuid().ToString();

        if( !_materials.ContainsKey( uid ) )
        {
          Va3cScene.Va3cMaterial m
            = new Va3cScene.Va3cMaterial();

          m.uuid = uid;
          m.type = "MeshPhongMaterial";
          m.color = Util.ColorToInt( node.Color );
          m.ambient = m.color;
          m.emissive = 0;
          m.specular = m.color;
          m.shininess = node.Glossiness; // todo: does this need scaling to e.g. [0,100]?
          m.opacity = 1; // 128 - material.Transparency;
          m.transparent = false;
          m.wireframe = false;

          _materials.Add( uid, m );

          _currentMaterialUid = uid;
        }
      }
    }

    public bool IsCanceled()
    {
      // This method is invoked many times during the export process.
      return false;
    }

    public void OnDaylightPortal( DaylightPortalNode node )
    {
      Debug.WriteLine( "OnDaylightPortal: " + node.NodeName );
      Asset asset = node.GetAsset();
      Debug.WriteLine( "OnDaylightPortal: Asset:" + ( ( asset != null ) ? asset.Name : "Null" ) );
    }

    public void OnRPC( RPCNode node )
    {
      Debug.WriteLine( "OnRPC: " + node.NodeName );
      Asset asset = node.GetAsset();
      Debug.WriteLine( "OnRPC: Asset:" + ( ( asset != null ) ? asset.Name : "Null" ) );
    }

    public RenderNodeAction OnViewBegin( ViewNode node )
    {
      Debug.WriteLine( "OnViewBegin: " + node.NodeName + "(" + node.ViewId.IntegerValue + "): LOD: " + node.LevelOfDetail );
      return RenderNodeAction.Proceed;
    }

    public void OnViewEnd( ElementId elementId )
    {
      Debug.WriteLine( "OnViewEnd: Id: " + elementId.IntegerValue );
      // Note: This method is invoked even for a view that was skipped.
    }

    public RenderNodeAction OnElementBegin(
      ElementId elementId )
    {
      Element e = _doc.GetElement( elementId );
      string uid = e.UniqueId;

      Debug.WriteLine( string.Format(
        "OnElementBegin: id {0} category {1} name {2}",
        elementId.IntegerValue, e.Category.Name, e.Name ) );

      if( _objects.ContainsKey( uid ) )
      {
        Debug.WriteLine( "\r\n*** Duplicate element!\r\n" );
        return RenderNodeAction.Skip;
      }

      _elementStack.Push( elementId );

      ICollection<ElementId> idsMaterialGeometry = e.GetMaterialIds( false );
      ICollection<ElementId> idsMaterialPaint = e.GetMaterialIds( true );

      int n = idsMaterialGeometry.Count;

      if( 1 < n )
      {
        Debug.Print( "{0} has {1} materials: {2}",
          Util.ElementDescription( e ), n,
          string.Join( ", ", idsMaterialGeometry.Select( 
            id => _doc.GetElement( id ).Name ) ) );
      }

      if( null != e.Category
        && null != e.Category.Material )
      {
        SetCurrentMaterial( e.Category.Material.UniqueId );
        //MaterialNode node = new MaterialNode();
        //node.MaterialId = e.Category.Material.Id;
        //OnMaterial( node );
      }

      _currentObject = new Va3cScene.Va3cObject();

      _currentObject.name = Util.ElementDescription( e );
      _currentObject.geometry = uid;
      _currentObject.material = _currentMaterialUid;
      _currentObject.matrix = new double[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
      _currentObject.type = "Mesh";
      _currentObject.uuid = uid;

      _currentGeometry = new Va3cScene.Va3cGeometry();

      _currentGeometry.uuid = uid;
      _currentGeometry.type = "Geometry";
      _currentGeometry.data = new Va3cScene.Va3cGeometryData();
      _currentGeometry.data.faces = new List<int>();
      _currentGeometry.data.vertices = new List<long>();
      _currentGeometry.data.normals = new List<double>();
      _currentGeometry.data.uvs = new List<double>();
      _currentGeometry.data.visible = true;
      _currentGeometry.data.castShadow = true;
      _currentGeometry.data.receiveShadow = false;
      _currentGeometry.data.doubleSided = true;
      _currentGeometry.data.scale = 1.0;

      _vertices.Clear();

      return RenderNodeAction.Proceed;
    }

    public void OnElementEnd(
      ElementId elementId )
    {
      // Note: this method is invoked even for 
      // elements that were skipped.

      Debug.WriteLine( "OnElementEnd: " + elementId.IntegerValue );

      foreach( KeyValuePair<PointInt,int> p in _vertices )
      {
        _currentGeometry.data.vertices.Add( p.Key.X );
        _currentGeometry.data.vertices.Add( p.Key.Y );
        _currentGeometry.data.vertices.Add( p.Key.Z );
      }

      _currentObject.geometry = _currentGeometry.uuid;

      _objects.Add( _currentObject.uuid, _currentObject );
      _currentObject = null;

      _geometries.Add( _currentGeometry.uuid, _currentGeometry );
      _currentGeometry = null;

      _elementStack.Pop();
    }

    public RenderNodeAction OnFaceBegin( FaceNode node )
    {
      // This method is invoked only if the custom exporter was set to include faces.
      Debug.Assert( false, "we set exporter.IncludeFaces false" );
      Debug.WriteLine( "  OnFaceBegin: " + node.NodeName );
      return RenderNodeAction.Proceed;
    }

    public void OnFaceEnd( FaceNode node )
    {
      // This method is invoked only if the custom exporter was set to include faces.
      Debug.Assert( false, "we set exporter.IncludeFaces false" );
      Debug.WriteLine( "  OnFaceEnd: " + node.NodeName );
      // Note: This method is invoked even for faces that were skipped.
    }

    public RenderNodeAction OnInstanceBegin( InstanceNode node )
    {
      Debug.WriteLine( "  OnInstanceBegin: " + node.NodeName + " symbol: " + node.GetSymbolId().IntegerValue );
      // This method marks the start of processing a family instance
      _transformationStack.Push( CurrentTransform.Multiply( node.GetTransform() ) );

      // We can either skip this instance or proceed with rendering it.
      return RenderNodeAction.Proceed;
    }

    public void OnInstanceEnd( InstanceNode node )
    {
      Debug.WriteLine( "  OnInstanceEnd: " + node.NodeName );
      // Note: This method is invoked even for instances that were skipped.
      _transformationStack.Pop();
    }

    public RenderNodeAction OnLinkBegin( LinkNode node )
    {
      Debug.WriteLine( "  OnLinkBegin: " + node.NodeName + " Document: " + node.GetDocument().Title + ": Id: " + node.GetSymbolId().IntegerValue );
      _transformationStack.Push( CurrentTransform.Multiply( node.GetTransform() ) );
      return RenderNodeAction.Proceed;
    }

    public void OnLinkEnd( LinkNode node )
    {
      Debug.WriteLine( "  OnLinkEnd: " + node.NodeName );
      // Note: This method is invoked even for instances that were skipped.
      _transformationStack.Pop();
    }

    public void OnLight( LightNode node )
    {
      Debug.WriteLine( "OnLight: " + node.NodeName );
      Asset asset = node.GetAsset();
      Debug.WriteLine( "OnLight: Asset:" + ( ( asset != null ) ? asset.Name : "Null" ) );
    }
  }
}
