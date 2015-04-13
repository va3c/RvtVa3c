#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.Utility;
using Newtonsoft.Json;
#endregion // Namespaces

namespace RvtVa3c
{
  // Done:
  // Check instance transformation
  // Support transparency
  // Add scaling for Theo [(0,0),(20000,20000)]
  // Implement the external application button
  // Implement element properties
  // Eliminate multiple materials 
  // Prompt user for output file name and location
  // Eliminate null element properties, i.e. useless 
  //     JSON userData entries
  // Todo:
  // Check for file size
  // Instance/type reuse

  public class Va3cExportContext : IExportContext
  {
    /// <summary>
    /// Scale entire top level BIM object nove in JSON
    /// output. A scale of 1.0 will output the model in 
    /// millimetres. Currently we scale it to decimetres
    /// so that a typical model has a chance of fitting 
    /// into a cube with side length 100, i.e. 10 metres.
    /// </summary>
    double _scale_bim = 1.0;

    /// <summary>
    /// Scale applied to each vertex in each individual 
    /// BIM element. This can be used to scale the model 
    /// down from millimetres to metres, e.g.
    /// Currently we stick with millimetres after all
    /// at this level.
    /// </summary>
    double _scale_vertex = 1.0;

    /// <summary>
    /// If true, switch Y and Z coordinate 
    /// and flip X to negative to convert from
    /// Revit coordinate system to standard 3d
    /// computer graphics coordinate system with
    /// Z pointing out of screen, X towards right,
    /// Y up.
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
    string _filename;
    Va3cContainer _container;
    Dictionary<string, Va3cContainer.Va3cMaterial> _materials;
    Dictionary<string, Va3cContainer.Va3cObject> _objects;
    Dictionary<string, Va3cContainer.Va3cGeometry> _geometries;

    Va3cContainer.Va3cObject _currentElement;

    // Keyed on material uid to handle several materials per element:

    Dictionary<string, Va3cContainer.Va3cObject> _currentObject;
    Dictionary<string, Va3cContainer.Va3cGeometry> _currentGeometry;
    Dictionary<string, VertexLookupInt> _vertices;

    Stack<ElementId> _elementStack = new Stack<ElementId>();
    Stack<Transform> _transformationStack = new Stack<Transform>();

    string _currentMaterialUid;

    public string myjs = null;

    Va3cContainer.Va3cObject CurrentObjectPerMaterial
    {
      get
      {
        return _currentObject[_currentMaterialUid];
      }
    }

    Va3cContainer.Va3cGeometry CurrentGeometryPerMaterial
    {
      get
      {
        return _currentGeometry[_currentMaterialUid];
      }
    }

    VertexLookupInt CurrentVerticesPerMaterial
    {
      get
      {
        return _vertices[_currentMaterialUid];
      }
    }

    Transform CurrentTransform
    {
      get
      {
        return _transformationStack.Peek();
      }
    }

    public override string ToString()
    {
      return myjs;
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

        Va3cContainer.Va3cMaterial m
          = new Va3cContainer.Va3cMaterial();

        //m.metadata = new Va3cContainer.Va3cMaterialMetadata();
        //m.metadata.type = "material";
        //m.metadata.version = 4.2;
        //m.metadata.generator = "RvtVa3c 2015.0.0.0";

        m.uuid = uidMaterial;
        m.name = material.Name;
        m.type = "MeshPhongMaterial";
        m.color = Util.ColorToInt( material.Color );
        m.ambient = m.color;
        m.emissive = 0;
        m.specular = m.color;
        m.shininess = 1; // todo: does this need scaling to e.g. [0,100]?
        m.opacity = 0.01 * (double) ( 100 - material.Transparency ); // Revit has material.Transparency in [0,100], three.js expects opacity in [0.0,1.0]
        m.transparent = 0 < material.Transparency;
        m.wireframe = false;

        _materials.Add( uidMaterial, m );
      }
      _currentMaterialUid = uidMaterial;

      string uid_per_material = _currentElement.uuid + "-" + uidMaterial;

      if( !_currentObject.ContainsKey( uidMaterial ) )
      {
        Debug.Assert( !_currentGeometry.ContainsKey( uidMaterial ), "expected same keys in both" );

        _currentObject.Add( uidMaterial, new Va3cContainer.Va3cObject() );
        CurrentObjectPerMaterial.name = _currentElement.name;
        CurrentObjectPerMaterial.geometry = uid_per_material;
        CurrentObjectPerMaterial.material = _currentMaterialUid;
        CurrentObjectPerMaterial.matrix = new double[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
        CurrentObjectPerMaterial.type = "Mesh";
        CurrentObjectPerMaterial.uuid = uid_per_material;
      }

      if( !_currentGeometry.ContainsKey( uidMaterial ) )
      {
        _currentGeometry.Add( uidMaterial, new Va3cContainer.Va3cGeometry() );
        CurrentGeometryPerMaterial.uuid = uid_per_material;
        CurrentGeometryPerMaterial.type = "Geometry";
        CurrentGeometryPerMaterial.data = new Va3cContainer.Va3cGeometryData();
        CurrentGeometryPerMaterial.data.faces = new List<int>();
        CurrentGeometryPerMaterial.data.vertices = new List<double>();
        CurrentGeometryPerMaterial.data.normals = new List<double>();
        CurrentGeometryPerMaterial.data.uvs = new List<double>();
        CurrentGeometryPerMaterial.data.visible = true;
        CurrentGeometryPerMaterial.data.castShadow = true;
        CurrentGeometryPerMaterial.data.receiveShadow = false;
        CurrentGeometryPerMaterial.data.doubleSided = true;
        CurrentGeometryPerMaterial.data.scale = 1.0;
      }

      if( !_vertices.ContainsKey( uidMaterial ) )
      {
        _vertices.Add( uidMaterial, new VertexLookupInt() );
      }
    }

    public Va3cExportContext( Document document, string filename )
    {
      _doc = document;
      _filename = filename;
    }

    public bool Start()
    {
      _materials = new Dictionary<string, Va3cContainer.Va3cMaterial>();
      _geometries = new Dictionary<string, Va3cContainer.Va3cGeometry>();
      _objects = new Dictionary<string, Va3cContainer.Va3cObject>();

      _transformationStack.Push( Transform.Identity );

      _container = new Va3cContainer();

      _container.metadata = new Va3cContainer.Metadata();
      _container.metadata.type = "Object";
      _container.metadata.version = 4.3;
      _container.metadata.generator = "RvtVa3c Revit vA3C exporter";
      _container.geometries = new List<Va3cContainer.Va3cGeometry>();

      _container.obj = new Va3cContainer.Va3cObject();
      _container.obj.uuid = _doc.ActiveView.UniqueId;
      _container.obj.name = "BIM " + _doc.Title;
      _container.obj.type = "Scene";

      // Scale entire BIM from millimetres to metres.

      _container.obj.matrix = new double[] { 
        _scale_bim, 0, 0, 0, 
        0, _scale_bim, 0, 0, 
        0, 0, _scale_bim, 0, 
        0, 0, 0, _scale_bim };

      return true;
    }

    public void Finish()
    {
      // Finish populating scene

      _container.materials = _materials.Values.ToList();

      _container.geometries = _geometries.Values.ToList();

      _container.obj.children = _objects.Values.ToList();

      // Serialise scene

      //using( FileStream stream
      //  = File.OpenWrite( filename ) )
      //{
      //  DataContractJsonSerializer serialiser
      //    = new DataContractJsonSerializer(
      //      typeof( Va3cContainer ) );
      //  serialiser.WriteObject( stream, _container );
      //}

      JsonSerializerSettings settings
        = new JsonSerializerSettings();

      settings.NullValueHandling
        = NullValueHandling.Ignore;

      Formatting formatting
        = UserSettings.JsonIndented
          ? Formatting.Indented
          : Formatting.None;

      myjs = JsonConvert.SerializeObject(
        _container, formatting, settings );

      File.WriteAllText( _filename, myjs );

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
    }

    public void OnPolymesh( PolymeshTopology polymesh )
    {
      //Debug.WriteLine( string.Format(
      //  "    OnPolymesh: {0} points, {1} facets, {2} normals {3}",
      //  polymesh.NumberOfPoints,
      //  polymesh.NumberOfFacets,
      //  polymesh.NumberOfNormals,
      //  polymesh.DistributionOfNormals ) );

      IList<XYZ> pts = polymesh.GetPoints();

      Transform t = CurrentTransform;

      pts = pts.Select( p => t.OfPoint( p ) ).ToList();

      int v1, v2, v3;

      foreach( PolymeshFacet facet
        in polymesh.GetFacets() )
      {
        //Debug.WriteLine( string.Format(
        //  "      {0}: {1} {2} {3}", i++,
        //  facet.V1, facet.V2, facet.V3 ) );

        v1 = CurrentVerticesPerMaterial.AddVertex( new PointInt(
          pts[facet.V1], _switch_coordinates ) );

        v2 = CurrentVerticesPerMaterial.AddVertex( new PointInt(
          pts[facet.V2], _switch_coordinates ) );

        v3 = CurrentVerticesPerMaterial.AddVertex( new PointInt(
          pts[facet.V3], _switch_coordinates ) );

        CurrentGeometryPerMaterial.data.faces.Add( 0 );
        CurrentGeometryPerMaterial.data.faces.Add( v1 );
        CurrentGeometryPerMaterial.data.faces.Add( v2 );
        CurrentGeometryPerMaterial.data.faces.Add( v3 );
      }
    }

    public void OnMaterial( MaterialNode node )
    {
      //Debug.WriteLine( "     --> On Material: " 
      //  + node.MaterialId + ": " + node.NodeName );

      // OnMaterial method can be invoked for every 
      // single out-coming mesh even when the material 
      // has not actually changed. Thus it is usually
      // beneficial to store the current material and 
      // only get its attributes when the material 
      // actually changes.

      ElementId id = node.MaterialId;

      if( ElementId.InvalidElementId != id )
      {
        Element m = _doc.GetElement( node.MaterialId );
        SetCurrentMaterial( m.UniqueId );
      }
      else
      {
        //string uid = Guid.NewGuid().ToString();

        // Generate a GUID based on colour, 
        // transparency, etc. to avoid duplicating
        // non-element material definitions.

        int iColor = Util.ColorToInt( node.Color );

        string uid = string.Format( "MaterialNode_{0}_{1}",
          iColor, Util.RealString( node.Transparency * 100 ) );

        if( !_materials.ContainsKey( uid ) )
        {
          Va3cContainer.Va3cMaterial m
            = new Va3cContainer.Va3cMaterial();

          m.uuid = uid;
          m.type = "MeshPhongMaterial";
          m.color = iColor;
          m.ambient = m.color;
          m.emissive = 0;
          m.specular = m.color;
          m.shininess = node.Glossiness; // todo: does this need scaling to e.g. [0,100]?
          m.opacity = 1; // 128 - material.Transparency;
          m.opacity = 1.0 - node.Transparency; // Revit MaterialNode has double Transparency in ?range?, three.js expects opacity in [0.0,1.0]
          m.transparent = 0.0 < node.Transparency;
          m.wireframe = false;

          _materials.Add( uid, m );
        }
        SetCurrentMaterial( uid );
      }
    }

    public bool IsCanceled()
    {
      // This method is invoked many 
      // times during the export process.

      return false;
    }

    public void OnDaylightPortal( DaylightPortalNode node )
    {
      Debug.WriteLine( "OnDaylightPortal: " + node.NodeName );
      Asset asset = node.GetAsset();
      Debug.WriteLine( "OnDaylightPortal: Asset:"
        + ( ( asset != null ) ? asset.Name : "Null" ) );
    }

    public void OnRPC( RPCNode node )
    {
      Debug.WriteLine( "OnRPC: " + node.NodeName );
      Asset asset = node.GetAsset();
      Debug.WriteLine( "OnRPC: Asset:"
        + ( ( asset != null ) ? asset.Name : "Null" ) );
    }

    public RenderNodeAction OnViewBegin( ViewNode node )
    {
      Debug.WriteLine( "OnViewBegin: "
        + node.NodeName + "(" + node.ViewId.IntegerValue
        + "): LOD: " + node.LevelOfDetail );

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

      if( null == e.Category )
      {
        Debug.WriteLine( "\r\n*** Non-category element!\r\n" );
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

      // We handle a current element, which may either
      // be identical to the current object and have
      // one single current geometry or have 
      // multiple current child objects each with a 
      // separate current geometry.

      _currentElement = new Va3cContainer.Va3cObject();

      _currentElement.name = Util.ElementDescription( e );
      _currentElement.material = _currentMaterialUid;
      _currentElement.matrix = new double[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
      _currentElement.type = "RevitElement";
      _currentElement.uuid = uid;

      _currentObject = new Dictionary<string, Va3cContainer.Va3cObject>();
      _currentGeometry = new Dictionary<string, Va3cContainer.Va3cGeometry>();
      _vertices = new Dictionary<string, VertexLookupInt>();

      if( null != e.Category
        && null != e.Category.Material )
      {
        SetCurrentMaterial( e.Category.Material.UniqueId );
      }

      return RenderNodeAction.Proceed;
    }

    public void OnElementEnd(
      ElementId id )
    {
      // Note: this method is invoked even for 
      // elements that were skipped.

      Element e = _doc.GetElement( id );
      string uid = e.UniqueId;

      Debug.WriteLine( string.Format(
        "OnElementEnd: id {0} category {1} name {2}",
        id.IntegerValue, e.Category.Name, e.Name ) );

      if( _objects.ContainsKey( uid ) )
      {
        Debug.WriteLine( "\r\n*** Duplicate element!\r\n" );
        return;
      }

      if( null == e.Category )
      {
        Debug.WriteLine( "\r\n*** Non-category element!\r\n" );
        return;
      }

      List<string> materials = _vertices.Keys.ToList();

      int n = materials.Count;

      _currentElement.children = new List<Va3cContainer.Va3cObject>( n );

      foreach( string material in materials )
      {
        Va3cContainer.Va3cObject obj = _currentObject[material];
        Va3cContainer.Va3cGeometry geo = _currentGeometry[material];

        foreach( KeyValuePair<PointInt, int> p in _vertices[material] )
        {
          geo.data.vertices.Add( _scale_vertex * p.Key.X );
          geo.data.vertices.Add( _scale_vertex * p.Key.Y );
          geo.data.vertices.Add( _scale_vertex * p.Key.Z );
        }
        obj.geometry = geo.uuid;
        _geometries.Add( geo.uuid, geo );
        _currentElement.children.Add( obj );
      }

      Dictionary<string, string> d
        = Util.GetElementProperties( e, true );

      _currentElement.userData = d;

      // Add Revit element unique id to user data dict.

      _currentElement.userData.Add( "revit_id", uid );

      _objects.Add( _currentElement.uuid, _currentElement );

      _elementStack.Pop();
    }

    public RenderNodeAction OnFaceBegin( FaceNode node )
    {
      // This method is invoked only if the 
      // custom exporter was set to include faces.

      Debug.Assert( false, "we set exporter.IncludeFaces false" );
      Debug.WriteLine( "  OnFaceBegin: " + node.NodeName );
      return RenderNodeAction.Proceed;
    }

    public void OnFaceEnd( FaceNode node )
    {
      // This method is invoked only if the 
      // custom exporter was set to include faces.

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
