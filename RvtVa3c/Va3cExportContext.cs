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
#endregion

namespace RvtVa3c
{
  class Va3cExportContext : IExportContext
  {
    private Document _doc;
    private XNamespace _ns;
    private XElement _collada;
    private XElement _libraryMaterials;
    private XElement _libraryGeometry;
    //private XElement _libraryImages;
    private XElement _libraryEffects;
    private XElement _libraryVisualScenes;

    public uint CurrentPolymeshIndex { get; set; }

    ElementId CurrentElementId
    {
      get
      {
        return ( elementStack.Count > 0 )
          ? elementStack.Peek()
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
        return transformationStack.Peek();
      }
    }

    private bool isCancelled = false;

    Stack<ElementId> elementStack = new Stack<ElementId>();

    private Stack<Transform> transformationStack = new Stack<Transform>();

    ElementId currentMaterialId = ElementId.InvalidElementId;

    //StreamWriter streamWriter = null;

    Dictionary<uint, ElementId> polymeshToMaterialId = new Dictionary<uint, ElementId>();
    List<Va3cFace> _faces = new List<Va3cFace>();

    public Va3cExportContext( Document document )
    {
      _doc = document;
      transformationStack.Push( Transform.Identity );
    }

    public bool Start()
    {
      CurrentPolymeshIndex = 0;
      _faces.Clear();
      polymeshToMaterialId.Clear();

      //streamWriter = new StreamWriter( "c:\\temp\\test.dae" );

      WriteXmlColladaBegin();
      WriteXmlAsset();

      WriteXmlLibraryGeometriesBegin();

      return true;
    }

    public void Finish()
    {


      WriteXmlLibraryMaterials();
      WriteXmlLibraryEffects();
      WriteXmlLibraryVisualScenes();
      WriteXmlColladaEnd();

      //streamWriter.Close();

      _collada.Save( @"C:\temp\testnew.dae" );
    }

    private void WriteXmlColladaBegin()
    {
      _ns = "http://www.collada.org/2005/11/COLLADASchema";
      _collada = new XElement( _ns + "COLLADA",
                              new XAttribute( "version", "1.4.1" ) );

      //streamWriter.Write( "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" );
      //streamWriter.Write( "<COLLADA xmlns=\"http://www.collada.org/2005/11/COLLADASchema\" version=\"1.4.1\">\n" );
    }

    private void WriteXmlColladaEnd()
    {
      //streamWriter.Write( "</COLLADA>\n" );
    }

    private void WriteXmlAsset()
    {
      _collada.Add(
          new XElement( _ns + "asset",
              new XElement( _ns + "contributor",
                  new XElement( _ns + "authoring_tool", "IMAGINiT Revit to Collada Addin" ) ),
              new XElement( _ns + "created", DateTime.Now.ToString( "O" ) ),
              new XElement( _ns + "modified", DateTime.Now.ToString( "O" ) ),
              new XElement( _ns + "unit",
                  new XAttribute( "name", "meter" ),
                  new XAttribute( "meter", "1.00" ) ),
              new XElement( _ns + "up_axis", "Z_UP" ) )


                  );

      //streamWriter.Write( "<asset>\n" );
      //streamWriter.Write( "<contributor>\n" );
      //streamWriter.Write( "  <authoring_tool>IMAGINiT Revit to Collada Addin</authoring_tool>\n" );
      //streamWriter.Write( "</contributor>\n" );
      //streamWriter.Write( "<created>" + DateTime.Now.ToString("O") + "</created>\n" );
      //streamWriter.Write("<modified>" + DateTime.Now.ToString("O") + "</modified>\n");

      //Units
      //streamWriter.Write( "<unit name=\"meter\" meter=\"1.00\"/>\n" );
      //streamWriter.Write( "<up_axis>Z_UP</up_axis>\n" );
      //streamWriter.Write( "</asset>\n" );
    }

    private void WriteXmlLibraryGeometriesBegin()
    {
      _libraryGeometry = new XElement( _ns + "library_geometries" );
      _collada.Add( _libraryGeometry );

      //streamWriter.Write( "<library_geometries>\n" );
    }


    public void OnPolymesh( PolymeshTopology polymesh )
    {
      Debug.WriteLine( "    OnPolymesh: " + polymesh.NumberOfPoints + " points: Normals: " + polymesh.DistributionOfNormals );
      CurrentPolymeshIndex++;

      XElement geom = WriteXmlGeometryBegin();
      _libraryGeometry.Add( geom );
      XElement mesh = new XElement( _ns + "mesh" );
      geom.Add( mesh );

      WriteXmlGeometrySourcePositions( mesh, polymesh );
      WriteXmlGeometrySourceNormals( mesh, polymesh );
      if( polymesh.NumberOfUVs > 0 )
        WriteXmlGeometrySourceMap( mesh, polymesh );

      WriteXmlGeometryVertices( mesh );

      if( polymesh.NumberOfUVs > 0 )
        WriteXmlGeometryTrianglesWithMap( mesh, polymesh );
      else
        WriteXmlGeometryTrianglesWithoutMap( mesh, polymesh );


      _faces.Add( new Va3cFace( CurrentElement, CurrentPolymeshIndex, currentMaterialId ) );
      polymeshToMaterialId.Add( CurrentPolymeshIndex, currentMaterialId );
    }

    private XElement WriteXmlGeometryBegin()
    {
      XElement geom = new XElement( _ns + "geometry",
                          new XAttribute( "id", "geom-" + CurrentPolymeshIndex ),
                          new XAttribute( "name", GetElementName( CurrentElement, "Type" ) ) );

      return geom;
      //streamWriter.Write( "<geometry id=\"geom-" + CurrentPolymeshIndex + "\" name=\"" + GetElementName(CurrentElement) + "\">\n" );
      //streamWriter.Write( "<mesh>\n" );
    }

    private string GetElementName( Element element, string defaultPrefix )
    {
      //make it an NCName

      //Element element = CurrentElement;
      if( element != null )
      {
        string name = element.Name;
        name = name.Replace( " ", "" );
        name = name.Replace( "\"", "in" );
        name = name.Replace( "&", "" );
        name = name.Replace( ":", "_" );
        name = name.Replace( ",", "_" );
        name = name.Replace( "/", "_" );
        name = name.Replace( "(", "_" );
        name = name.Replace( ")", "_" );
        name = name.Replace( "@", "_" );
        name = name.Replace( "\\", "_" );
        name = name.Replace( "/", "_" );

        name = name.Replace( "'", "ft" );
        name = name.Replace( "%20", "" );
        name = name.Replace( "%", "_" );
        name = name.Replace( "[", "_" );
        name = name.Replace( "]", "_" );
        if( char.IsNumber( name[0] ) ) name = defaultPrefix + name;
        return name;
      }
      return ""; //default name
    }



    private void WriteXmlGeometrySourcePositions( XElement mesh, PolymeshTopology polymesh )
    {

      XElement floatArray = new XElement( _ns + "float_array",
                              new XAttribute( "id", "geom-" + CurrentPolymeshIndex + "-positions-array" ),
                              new XAttribute( "count", ( polymesh.NumberOfPoints * 3 ).ToString() ) );



      //streamWriter.Write( "<source id=\"geom-" + CurrentPolymeshIndex + "-positions" + "\">\n" );
      //streamWriter.Write( "<float_array id=\"geom-" + CurrentPolymeshIndex + "-positions-array" + "\" count=\"" + ( polymesh.NumberOfPoints * 3 ) + "\">\n" );

      XYZ point;
      Transform currentTransform = transformationStack.Peek();
      System.Text.StringBuilder sb = new System.Text.StringBuilder();
      for( int iPoint = 0; iPoint < polymesh.NumberOfPoints; ++iPoint )
      {
        point = polymesh.GetPoint( iPoint );
        point = currentTransform.OfPoint( point );
        sb.AppendLine( String.Format( "{0:0.0000} {1:0.0000} {2:0.0000}", point.X, point.Y, point.Z ) );
      }
      floatArray.Value = sb.ToString();

      mesh.Add(

            new XElement( _ns + "source",
                new XAttribute( "id", "geom-" + CurrentPolymeshIndex + "-positions" ),
                floatArray,
                new XElement( _ns + "technique_common",
                    new XElement( _ns + "accessor",
                        new XAttribute( "source", "#geom-" + CurrentPolymeshIndex + "-positions-array" ),
                        new XAttribute( "count", polymesh.NumberOfPoints ),
                        new XAttribute( "stride", "3" ),
                        new XElement( _ns + "param",
                            new XAttribute( "name", "X" ),
                            new XAttribute( "type", "float" ) ),
                        new XElement( _ns + "param",
                            new XAttribute( "name", "Y" ),
                            new XAttribute( "type", "float" ) ),
                        new XElement( _ns + "param",
                            new XAttribute( "name", "Z" ),
                            new XAttribute( "type", "float" ) ) ) ) ) );


      //streamWriter.Write( "</float_array>\n" );
      //streamWriter.Write( "<technique_common>\n" );
      //streamWriter.Write( "<accessor source=\"#geom-" + CurrentPolymeshIndex + "-positions-array\"" + " count=\"" + polymesh.NumberOfPoints + "\" stride=\"3\">\n" );
      //streamWriter.Write( "<param name=\"X\" type=\"float\"/>\n" );
      //streamWriter.Write( "<param name=\"Y\" type=\"float\"/>\n" );
      //streamWriter.Write( "<param name=\"Z\" type=\"float\"/>\n" );
      //streamWriter.Write( "</accessor>\n" );
      //streamWriter.Write( "</technique_common>\n" );
      //streamWriter.Write( "</source>\n" );



    }

    private void WriteXmlGeometrySourceNormals( XElement mesh, PolymeshTopology polymesh )
    {
      int nNormals = 0;

      switch( polymesh.DistributionOfNormals )
      {
        case DistributionOfNormals.AtEachPoint:
          nNormals = polymesh.NumberOfPoints;
          break;
        case DistributionOfNormals.OnePerFace:
          nNormals = 1;
          break;
        case DistributionOfNormals.OnEachFacet:
          //TODO : DistributionOfNormals.OnEachFacet
          nNormals = 1;
          break;
      }

      XElement source = new XElement( _ns + "source",
                            new XAttribute( "id", "geom-" + CurrentPolymeshIndex + "-normals" ) );
      mesh.Add( source );

      XElement float_array = new XElement( _ns + "float_array",
                                new XAttribute( "id", "geom-" + CurrentPolymeshIndex + "-normals-array" ),
                                new XAttribute( "count", ( nNormals * 3 ) ) );

      //streamWriter.Write( "<source id=\"geom-" + CurrentPolymeshIndex + "-normals" + "\">\n" );
      //streamWriter.Write( "<float_array id=\"geom-" + CurrentPolymeshIndex + "-normals" + "-array" + "\" count=\"" + ( nNormals * 3 ) + "\">\n" );

      XYZ point;
      Transform currentTransform = transformationStack.Peek();
      System.Text.StringBuilder sb = new System.Text.StringBuilder();
      for( int iNormal = 0; iNormal < nNormals; ++iNormal )
      {
        point = polymesh.GetNormal( iNormal );
        point = currentTransform.OfVector( point );
        sb.AppendLine( String.Format( "{0:0.0000} {1:0.0000} {2:0.0000}", point.X, point.Y, point.Z ) );
      }
      float_array.Value = sb.ToString();

      source.Add( float_array );
      source.Add( new XElement( _ns + "technique_common",
                      new XElement( _ns + "accessor",
                          new XAttribute( "source", "#geom-" + CurrentPolymeshIndex + "-normals-array" ),
                          new XAttribute( "count", nNormals ),
                          new XAttribute( "stride", 3 ),
                          new XElement( _ns + "param",
                              new XAttribute( "name", "X" ),
                              new XAttribute( "type", "float" ) ),
                          new XElement( _ns + "param",
                              new XAttribute( "name", "Y" ),
                              new XAttribute( "type", "float" ) ),
                          new XElement( _ns + "param",
                              new XAttribute( "name", "Z" ),
                              new XAttribute( "type", "float" ) ) ) ) );

      //streamWriter.Write( "</float_array>\n" );
      //streamWriter.Write( "<technique_common>\n" );
      //streamWriter.Write( "<accessor source=\"#geom-" + CurrentPolymeshIndex + "-normals" + "-array\"" + " count=\"" + nNormals + "\" stride=\"3\">\n" );
      //streamWriter.Write( "<param name=\"X\" type=\"float\"/>\n" );
      //streamWriter.Write( "<param name=\"Y\" type=\"float\"/>\n" );
      //streamWriter.Write( "<param name=\"Z\" type=\"float\"/>\n" );
      //streamWriter.Write( "</accessor>\n" );
      //streamWriter.Write( "</technique_common>\n" );
      //streamWriter.Write( "</source>\n" );
    }

    private void WriteXmlGeometrySourceMap( XElement mesh, PolymeshTopology polymesh )
    {
      XElement source = new XElement( _ns + "source",
                          new XAttribute( "id", "geom-" + CurrentPolymeshIndex + "-map" ) );
      mesh.Add( source );

      XElement float_array = new XElement( _ns + "float_array",
                                  new XAttribute( "id", "geom-" + CurrentPolymeshIndex + "-map-array" ),
                                  new XAttribute( "count", ( polymesh.NumberOfUVs * 2 ) ) );
      source.Add( float_array );

      //streamWriter.Write( "<source id=\"geom-" + CurrentPolymeshIndex + "-map" + "\">\n" );
      //streamWriter.Write( "<float_array id=\"geom-" + CurrentPolymeshIndex + "-map" + "-array" + "\" count=\"" + ( polymesh.NumberOfUVs * 2 ) + "\">\n" );

      UV uv;

      StringBuilder sb = new StringBuilder();
      for( int iUv = 0; iUv < polymesh.NumberOfUVs; ++iUv )
      {
        uv = polymesh.GetUV( iUv );
        sb.AppendLine( String.Format( "{0:0.0000} {1:0.0000}", uv.U, uv.V ) );
      }
      float_array.Value = sb.ToString();

      source.Add( new XElement( _ns + "technique_common",
                      new XElement( _ns + "accessor",
                          new XAttribute( "source", "#geom-" + CurrentPolymeshIndex + "-map-array" ),
                          new XAttribute( "count", polymesh.NumberOfPoints ),
                          new XAttribute( "stride", 2 ),
                          new XElement( _ns + "param",
                              new XAttribute( "name", "S" ),
                              new XAttribute( "type", "float" ) ),
                          new XElement( _ns + "param",
                              new XAttribute( "name", "T" ),
                              new XAttribute( "type", "float" ) ) ) ) );

      //streamWriter.Write( "</float_array>\n" );
      //streamWriter.Write( "<technique_common>\n" );
      //streamWriter.Write( "<accessor source=\"#geom-" + CurrentPolymeshIndex + "-map" + "-array\"" + " count=\"" + polymesh.NumberOfPoints + "\" stride=\"2\">\n" );
      //streamWriter.Write( "<param name=\"S\" type=\"float\"/>\n" );
      //streamWriter.Write( "<param name=\"T\" type=\"float\"/>\n" );
      //streamWriter.Write( "</accessor>\n" );
      //streamWriter.Write( "</technique_common>\n" );
      //streamWriter.Write( "</source>\n" );
    }

    private void WriteXmlGeometryVertices( XElement mesh )
    {
      mesh.Add( new XElement( _ns + "vertices",
                  new XAttribute( "id", "geom-" + CurrentPolymeshIndex + "-vertices" ),
                  new XElement( _ns + "input",
                      new XAttribute( "semantic", "POSITION" ),
                      new XAttribute( "source", "#geom-" + CurrentPolymeshIndex + "-positions" ) ) ) );

      //streamWriter.Write( "<vertices id=\"geom-" + CurrentPolymeshIndex + "-vertices" + "\">\n" );
      //streamWriter.Write( "<input semantic=\"POSITION\" source=\"#geom-" + CurrentPolymeshIndex + "-positions" + "\"/>\n" );
      //streamWriter.Write( "</vertices>\n" );
    }

    private void WriteXmlGeometryTrianglesWithoutMap( XElement mesh, PolymeshTopology polymesh )
    {
      XElement triangles = new XElement( _ns + "triangles",
                              new XAttribute( "count", polymesh.NumberOfFacets ) );
      mesh.Add( triangles );

      if( IsMaterialValid( currentMaterialId ) ) triangles.AddAnnotation( new XAttribute( "material", "material-" + currentMaterialId ) );

      triangles.Add( new XElement( _ns + "input",
                          new XAttribute( "offset", "0" ),
                          new XAttribute( "semantic", "VERTEX" ),
                          new XAttribute( "source", "#geom-" + CurrentPolymeshIndex + "-vertices" ) ),
                     new XElement( _ns + "input",
                         new XAttribute( "offset", "1" ),
                         new XAttribute( "semantic", "NORMAL" ),
                         new XAttribute( "source", "#geom-" + CurrentPolymeshIndex + "-normals" ) ) );

      //streamWriter.Write( "<triangles count=\"" + polymesh.NumberOfFacets + "\"" );
      //if( IsMaterialValid( currentMaterialId ) )
      //  streamWriter.Write( " material=\"material-" + currentMaterialId.ToString() + "\"" );
      //streamWriter.Write( ">\n" );
      //streamWriter.Write( "<input offset=\"0\" semantic=\"VERTEX\" source=\"#geom-" + CurrentPolymeshIndex + "-vertices" + "\"/>\n" );
      //streamWriter.Write( "<input offset=\"1\" semantic=\"NORMAL\" source=\"#geom-" + CurrentPolymeshIndex + "-normals" + "\"/>\n" );
      //streamWriter.Write( "<p>\n" );
      PolymeshFacet facet;

      StringBuilder sb = new StringBuilder();
      switch( polymesh.DistributionOfNormals )
      {
        case DistributionOfNormals.AtEachPoint:

          for( int i = 0; i < polymesh.NumberOfFacets; ++i )
          {
            facet = polymesh.GetFacet( i );
            sb.AppendLine( facet.V1 + " " + facet.V1 + " " +
                        facet.V2 + " " + facet.V2 + " " +
                        facet.V3 + " " + facet.V3 + " " );
          }
          break;

        case DistributionOfNormals.OnEachFacet:
        //TODO : DistributionOfNormals.OnEachFacet
        case DistributionOfNormals.OnePerFace:
          for( int i = 0; i < polymesh.NumberOfFacets; ++i )
          {
            facet = polymesh.GetFacet( i );
            sb.AppendLine( facet.V1 + " 0 " +
                        facet.V2 + " 0 " +
                        facet.V3 + " 0 " );
          }
          break;

      }

      triangles.Add( new XElement( _ns + "p", sb.ToString() ) );
      //streamWriter.Write( "</p>\n" );
      //streamWriter.Write( "</triangles>\n" );
    }

    private void WriteXmlGeometryTrianglesWithMap( XElement mesh, PolymeshTopology polymesh )
    {
      XElement triangles = new XElement( _ns + "triangles",
                             new XAttribute( "count", polymesh.NumberOfFacets ) );
      mesh.Add( triangles );

      if( IsMaterialValid( currentMaterialId ) ) triangles.AddAnnotation( new XAttribute( "material", "material-" + currentMaterialId ) );

      triangles.Add( new XElement( _ns + "input",
                          new XAttribute( "offset", "0" ),
                          new XAttribute( "semantic", "VERTEX" ),
                          new XAttribute( "source", "#geom-" + CurrentPolymeshIndex + "-vertices" ) ),
                     new XElement( _ns + "input",
                         new XAttribute( "offset", "1" ),
                         new XAttribute( "semantic", "NORMAL" ),
                         new XAttribute( "source", "#geom-" + CurrentPolymeshIndex + "-normals" ) ),
                     new XElement( _ns + "input",
                         new XAttribute( "offset", "2" ),
                         new XAttribute( "semantic", "TEXCOORD" ),
                         new XAttribute( "source", "#geom-" + CurrentPolymeshIndex + "-map" ),
                         new XAttribute( "set", "0" ) ) );


      //streamWriter.Write( "<triangles count=\"" + polymesh.NumberOfFacets + "\"" );
      //if( IsMaterialValid( currentMaterialId ) )
      //  streamWriter.Write( " material=\"material-" + currentMaterialId.ToString() + "\"" );
      //streamWriter.Write( ">\n" );
      //streamWriter.Write( "<input offset=\"0\" semantic=\"VERTEX\" source=\"#geom-" + CurrentPolymeshIndex + "-vertices" + "\"/>\n" );
      //streamWriter.Write( "<input offset=\"1\" semantic=\"NORMAL\" source=\"#geom-" + CurrentPolymeshIndex + "-normals" + "\"/>\n" );
      //streamWriter.Write( "<input offset=\"2\" semantic=\"TEXCOORD\" source=\"#geom-" + CurrentPolymeshIndex + "-map" + "\" set=\"0\"/>\n" );
      //streamWriter.Write( "<p>\n" );
      PolymeshFacet facet;

      StringBuilder sb = new StringBuilder();
      switch( polymesh.DistributionOfNormals )
      {
        case DistributionOfNormals.AtEachPoint:
          for( int i = 0; i < polymesh.NumberOfFacets; ++i )
          {
            facet = polymesh.GetFacet( i );
            sb.AppendLine( facet.V1 + " " + facet.V1 + " " + facet.V1 + " " +
                        facet.V2 + " " + facet.V2 + " " + facet.V2 + " " +
                        facet.V3 + " " + facet.V3 + " " + facet.V3 + " " );
          }
          break;

        case DistributionOfNormals.OnEachFacet:
        //TODO : DistributionOfNormals.OnEachFacet
        case DistributionOfNormals.OnePerFace:
          for( int i = 0; i < polymesh.NumberOfFacets; ++i )
          {
            facet = polymesh.GetFacet( i );
            sb.AppendLine( facet.V1 + " 0 " + facet.V1 + " " +
                        facet.V2 + " 0 " + facet.V2 + " " +
                        facet.V3 + " 0 " + facet.V3 + " " );
          }
          break;
      }

      triangles.Add( new XElement( _ns + "p", sb.ToString() ) );
      //streamWriter.Write( "</p>\n" );
      //streamWriter.Write( "</triangles>\n" );
    }

    public void OnMaterial( MaterialNode node )
    {
      // OnMaterial method can be invoked for every single out-coming mesh
      // even when the material has not actually changed. Thus it is usually
      // beneficial to store the current material and only get its attributes
      // when the material actually changes.

      currentMaterialId = node.MaterialId;
    }

    private void WriteXmlLibraryMaterials()
    {
      _libraryMaterials = new XElement( _ns + "library_materials" );
      _collada.Add( _libraryMaterials );

      //streamWriter.Write( "<library_materials>\n" );

      foreach( var materialId in polymeshToMaterialId.Values.Distinct() )
      {
        if( IsMaterialValid( materialId ) == false )
          continue;

        _libraryMaterials.Add( new XElement( _ns + "material",
                                  new XAttribute( "id", "material-" + materialId ),
                                  new XAttribute( "name", GetMaterialName( materialId ) ),
                                  new XElement( _ns + "instance_effect",
                                        new XAttribute( "url", "#effect-" + materialId ) ) ) );

        //streamWriter.Write( "<material id=\"material-" + materialId.ToString() + "\" name=\"" + GetMaterialName( materialId ) + "\">\n" );
        //streamWriter.Write( "<instance_effect url=\"#effect-" + materialId.ToString() + "\" />\n" );
        //streamWriter.Write( "</material>\n" );
      }

      //streamWriter.Write( "</library_materials>\n" );
    }

    private string GetMaterialName( ElementId materialId )
    {
      Material material = _doc.GetElement( materialId ) as Material;
      if( material != null )
      {
        return GetElementName( material, "Material" );
      }

      return ""; //default material name
    }

    private bool IsMaterialValid( ElementId materialId )
    {
      if( materialId.IntegerValue < 0 ) return false;
      Material material = _doc.GetElement( materialId ) as Material;
      if( material != null )
        return true;

      return false;
    }

    private void WriteXmlLibraryEffects()
    {
      _libraryEffects = new XElement( _ns + "library_effects" );
      _collada.Add( _libraryEffects );

      //streamWriter.Write( "<library_effects>\n" );

      foreach( var materialId in polymeshToMaterialId.Values.Distinct() )
      {
        if( IsMaterialValid( materialId ) == false )
          continue;

        Material material = _doc.GetElement( materialId ) as Material;

        XElement profileCommon = new XElement( _ns + "profile_COMMON" );
        XElement texture = extractMaterialTexture( material, profileCommon );



        _libraryEffects.Add( new XElement( _ns + "effect",
                                new XAttribute( "id", "effect-" + materialId ),
                                new XAttribute( "name", GetMaterialName( materialId ) ),
                                new XElement( _ns + "profile_COMMON",
                                    new XElement( _ns + "technique",
                                        new XAttribute( "sid", "common" ),
                                        new XElement( _ns + "phong",
                                            new XElement( _ns + "ambient",
                                                new XElement( _ns + "color", "0.1 0.1 0.1 1.000000" ) ),
                                            new XElement( _ns + "diffuse",
                                                new XElement( _ns + "color", ( (double) material.Color.Red / 255.0 ) + " " + ( (double) material.Color.Green / 255.0 ) + " " + ( (double) material.Color.Blue / 255.0 ) + " " + (double) ( 100 - material.Transparency ) / 100.0 ) ),
                                            new XElement( _ns + "specular",
                                                new XElement( _ns + "color", "0.1 0.1 0.1 1.000000" ) ),
                                            new XElement( _ns + "shininess",
                                                new XElement( _ns + "float", (double) material.Shininess / 100.0 ) ),
                                            new XElement( _ns + "reflective",
                                                new XElement( _ns + "color", "0 0 0 1.000000" ) ),
                                            new XElement( _ns + "reflectivity",
                                                new XElement( _ns + "float", "1.000" ) ),
                                            new XElement( _ns + "transparent",
                                                new XAttribute( "opaque", "RGB_ZERO" ),
                                                new XElement( _ns + "color", "1.000000 1.000000 1.000000 1.000000" ) ),
                                            new XElement( _ns + "transparency",
                                                new XElement( _ns + "float", (double) material.Transparency / 100.0 ) )
                                                ) ) ) ) );



        //streamWriter.Write( "<effect id=\"effect-" + materialId.ToString() + "\" name=\"" + GetMaterialName( materialId ) + "\">\n" );
        //streamWriter.Write( "<profile_COMMON>\n" );

        //streamWriter.Write( "<technique sid=\"common\">\n" );
        //streamWriter.Write( "<phong>\n" );
        //streamWriter.Write( "<ambient>\n" );
        //streamWriter.Write( "<color>0.1 0.1 0.1 1.000000</color>\n" );
        //streamWriter.Write( "</ambient>\n" );


        ////diffuse
        //streamWriter.Write( "<diffuse>\n" );
        //streamWriter.Write( "<color>" + material.Color.Red + " " + material.Color.Green + " " + material.Color.Blue + " 1.0</color>\n" );
        //streamWriter.Write( "</diffuse>\n" );


        //streamWriter.Write( "<specular>\n" );
        //streamWriter.Write( "<color>1.000000 1.000000 1.000000 1.000000</color>\n" );
        //streamWriter.Write( "</specular>\n" );

        //streamWriter.Write( "<shininess>\n" );
        //streamWriter.Write( "<float>" + material.Shininess + "</float>\n" );
        //streamWriter.Write( "</shininess>\n" );

        //streamWriter.Write( "<reflective>\n" );
        //streamWriter.Write( "<color>0 0 0 1.000000</color>\n" );
        //streamWriter.Write( "</reflective>\n" );
        //streamWriter.Write( "<reflectivity>\n" );
        //streamWriter.Write( "<float>1.000000</float>\n" );
        //streamWriter.Write( "</reflectivity>\n" );

        //streamWriter.Write( "<transparent opaque=\"RGB_ZERO\">\n" );
        //streamWriter.Write( "<color>1.000000 1.000000 1.000000 1.000000</color>\n" );
        //streamWriter.Write( "</transparent>\n" );

        //streamWriter.Write( "<transparency>\n" );
        //streamWriter.Write( "<float>" + material.Transparency + "</float>\n" );
        //streamWriter.Write( "</transparency>\n" );

        //streamWriter.Write( "</phong>\n" );
        //streamWriter.Write( "</technique>\n" );


        //streamWriter.Write( "</profile_COMMON>\n" );
        //streamWriter.Write( "</effect>\n" );
      }

      //streamWriter.Write( "</library_effects>\n" );
    }

    private XElement extractMaterialTexture( Material mat, XElement profileNode )
    {
      // we need to figure out if we can get at the texture... if we can, then pull it out to the target folder.
      AppearanceAssetElement assetElement = _doc.GetElement( mat.AppearanceAssetId ) as AppearanceAssetElement;

      if( assetElement == null ) return null;

      Asset asset = assetElement.GetRenderingAsset();

      Va3cMaterial cMat = new Va3cMaterial( mat );

      return null;
    }

    public void WriteXmlLibraryVisualScenes()
    {

      _libraryVisualScenes = new XElement( _ns + "library_visual_scenes" );
      _collada.Add( _libraryVisualScenes );

      string sceneName = "RevitModel-" + _doc.Title.Replace( " ", "" );

      XElement scene = new XElement( _ns + "visual_scene",
                          new XAttribute( "id", sceneName ) );
      _libraryVisualScenes.Add( scene );

      // streamWriter.Write( "<library_visual_scenes>\n" );
      //streamWriter.Write( "<visual_scene id=\"Revit_project\">\n" );
      Dictionary<int, XElement> categoryNodes = new Dictionary<int, XElement>();

      _faces = _faces.OrderBy( f => f.ElementId ).ToList();

      int lastId = -1;
      XElement elemNode = null;
      XElement categoryNode = null;
      string name = "elementName";
      foreach( var face in _faces )
      {
        if( categoryNodes.ContainsKey( face.CategoryId ) )
        {
          categoryNode = categoryNodes[face.CategoryId];
        }
        else
        {
          Category c = _doc.Settings.Categories.get_Item( (BuiltInCategory) face.CategoryId );
          if( c != null )
          {
            categoryNode = new XElement( _ns + "node",
                                          new XAttribute( "id", "category-" + face.CategoryId ),
                                          new XAttribute( "name", c.Name.Replace( " ", "" ) ) );
          }
          else
          {
            categoryNode = new XElement( _ns + "node",
                                          new XAttribute( "id", "category-" + face.CategoryId ),
                                          new XAttribute( "name", "other" ) );
          }
          scene.Add( categoryNode );
          categoryNodes.Add( face.CategoryId, categoryNode );
        }

        if( face.ElementId != lastId )
        {
          Element e = face.Element;
          if( e != null ) name = GetElementName( e, "Elem" );
          elemNode = new XElement( _ns + "node",
                                new XAttribute( "id", "elem-" + face.ElementId ),
                                new XAttribute( "name", name ) );
          if( categoryNode != null ) categoryNode.Add( elemNode );
          lastId = face.ElementId;
        }


        XElement node = new XElement( _ns + "node",
                          new XAttribute( "id", "node-" + face.FaceId ),
                          new XAttribute( "name", name ) );
        if( elemNode != null ) elemNode.Add( node );
        XElement inst = new XElement( _ns + "instance_geometry",
                              new XAttribute( "url", "#geom-" + face.FaceId ) );
        node.Add( inst );
        if( IsMaterialValid( face.MaterialId ) )
        {
          inst.Add( new XElement( _ns + "bind_material",
                        new XElement( _ns + "technique_common",
                            new XElement( _ns + "instance_material",
                                new XAttribute( "target", "#material-" + face.MaterialId.IntegerValue ),
                                new XAttribute( "symbol", "material-" + face.MaterialId.IntegerValue ) ) ) ) );
        }

        //streamWriter.Write( "<node id=\"node-" + pair.Key + "\" name=\"" + name + "\">\n" );
        //streamWriter.Write( "<instance_geometry url=\"#geom-" + pair.Key + "\">\n" );
        //if( IsMaterialValid( pair.Value ) )
        //{
        //  streamWriter.Write( "<bind_material>\n" );
        //  streamWriter.Write( "<technique_common>\n" );
        //  streamWriter.Write( "<instance_material target=\"#material-" + pair.Value + "\" symbol=\"material-" + pair.Value + "\" >\n" );
        //  streamWriter.Write( "</instance_material>\n" );
        //  streamWriter.Write( "</technique_common>\n" );
        //  streamWriter.Write( "</bind_material>\n" );
        //}
        //streamWriter.Write( "</instance_geometry>\n" );
        //streamWriter.Write( "</node>\n" );
      }

      //streamWriter.Write( "</visual_scene>\n" );
      //streamWriter.Write( "</library_visual_scenes>\n" );

      _collada.Add( new XElement( _ns + "scene",
                      new XElement( _ns + "instance_visual_scene",
                          new XAttribute( "url", "#" + sceneName ) ) ) );
      //streamWriter.Write( "<scene>\n" );
      //streamWriter.Write( "<instance_visual_scene url=\"#Revit_project\"/>\n" );
      //streamWriter.Write( "</scene>\n" );
    }

    public bool IsCanceled()
    {
      // This method is invoked many times during the export process.
      return isCancelled;
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

    public RenderNodeAction OnElementBegin( ElementId elementId )
    {
      Element e = _doc.GetElement( elementId );
      Debug.WriteLine( "OnElementBegin: " + elementId.IntegerValue + ": " + e.Category.Name + ": " + e.Name );
      elementStack.Push( elementId );

      return RenderNodeAction.Proceed;
    }

    public void OnElementEnd( ElementId elementId )
    {
      Debug.WriteLine( "OnElementEnd: " + elementId.IntegerValue );
      // Note: this method is invoked even for elements that were skipped.
      elementStack.Pop();
    }

    public RenderNodeAction OnFaceBegin( FaceNode node )
    {
      Debug.WriteLine( "  OnFaceBegin: " + node.NodeName );
      // This method is invoked only if the custom exporter was set to include faces.
      return RenderNodeAction.Proceed;
    }

    public void OnFaceEnd( FaceNode node )
    {
      Debug.WriteLine( "  OnFaceEnd: " + node.NodeName );
      // This method is invoked only if the custom exporter was set to include faces.
      // Note: This method is invoked even for faces that were skipped.
    }

    public RenderNodeAction OnInstanceBegin( InstanceNode node )
    {
      Debug.WriteLine( "  OnInstanceBegin: " + node.NodeName + " symbol: " + node.GetSymbolId().IntegerValue );
      // This method marks the start of processing a family instance
      transformationStack.Push( transformationStack.Peek().Multiply( node.GetTransform() ) );

      // We can either skip this instance or proceed with rendering it.
      return RenderNodeAction.Proceed;
    }

    public void OnInstanceEnd( InstanceNode node )
    {
      Debug.WriteLine( "  OnInstanceEnd: " + node.NodeName );
      // Note: This method is invoked even for instances that were skipped.
      transformationStack.Pop();
    }

    public RenderNodeAction OnLinkBegin( LinkNode node )
    {
      Debug.WriteLine( "  OnLinkBegin: " + node.NodeName + " Document: " + node.GetDocument().Title + ": Id: " + node.GetSymbolId().IntegerValue );
      transformationStack.Push( transformationStack.Peek().Multiply( node.GetTransform() ) );
      return RenderNodeAction.Proceed;
    }

    public void OnLinkEnd( LinkNode node )
    {
      Debug.WriteLine( "  OnLinkEnd: " + node.NodeName );
      // Note: This method is invoked even for instances that were skipped.
      transformationStack.Pop();
    }

    public void OnLight( LightNode node )
    {
      Debug.WriteLine( "OnLight: " + node.NodeName );
      Asset asset = node.GetAsset();
      Debug.WriteLine( "OnLight: Asset:" + ( ( asset != null ) ? asset.Name : "Null" ) );
    }
  }
}
