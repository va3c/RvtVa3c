#region Namespaces
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
#endregion // Namespaces

namespace RvtVa3c
{
  /// <summary>
  /// three.js scene class
  /// </summary>
  [DataContract] public class Va3cScene
  {
    //public class Va3cMaterialMetadata
    //{
    //  [DataMember] public double version { get; set; }
    //  [DataMember] public string type { get; set; }
    //  [DataMember] public string generator { get; set; }
    //}

    public class Va3cMaterial
    {
      //[DataMember] public Va3cMaterialMetadata metadata { get; set; }
      [DataMember] public string uuid { get; set; }
      [DataMember] public string type { get; set; } // MeshPhongMaterial
      [DataMember] public int color { get; set; } // 16777215
      [DataMember] public int ambient { get; set; } //16777215
      [DataMember] public int emissive { get; set; } // 1
      [DataMember] public int specular { get; set; } //1118481
      [DataMember] public int shininess { get; set; } // 30
      [DataMember] public int opacity { get; set; } // 1
      [DataMember] public bool transparent { get; set; } // false
      [DataMember] public bool wireframe { get; set; } // false

      //"DbgColor" : 15658734
      //"DbgIndex" : 0
      //"DbgName" : "Material"
      //"blending" : "NormalBlending"
      //"colorAmbient" : [0.6400000190734865, 0.10179081114814892, 0.126246120426746]
      //"colorDiffuse" : [0.6400000190734865, 0.10179081114814892, 0.126246120426746]
      //"colorSpecular" : [0.5, 0.5, 0.5]
      //"depthTest" : true
      //"depthWrite" : true
      //"shading" : "Lambert"
      //"specularCoef" : 50
      //"transparency" : 1.0
      //"transparent" : false
      //"vertexColors" : false
    }

    //  MeshPhongMaterial from https://github.com/mrdoob/three.js/wiki/JSON-Material-format-4

    //{
    //"metadata": {
    //    "version": 4.2
    //    "type": "material"
    //    "generator": "MaterialExporter"
    //}
    //"type": "MeshPhongMaterial"
    //"color": 16777215
    //"ambient": 16777215
    //"emissive": 0
    //"specular": 1118481
    //"shininess": 30
    //"opacity": 1
    //"transparent": false

    [DataContract]
    public class Va3cGeometryData
    {
      // populate data object properties
      //jason.data.vertices = new object[mesh.Vertices.Count * 3];
      //jason.data.normals = new object[0];
      //jason.data.uvs = new object[0];
      //jason.data.faces = new object[mesh.Faces.Count * 4];
      //jason.data.scale = 1;
      //jason.data.visible = true;
      //jason.data.castShadow = true;
      //jason.data.receiveShadow = false;
      //jason.data.doubleSided = true;

      [DataMember] public List<long> vertices { get; set; } // millimetres
      // "morphTargets": []
      [DataMember] public List<double> normals { get; set; }
      // "colors": []
      [DataMember] public List<double> uvs { get; set; }
      [DataMember] public List<int> faces { get; set; } // indices into Vertices + Materials
      [DataMember] public double scale { get; set; }
      [DataMember] public bool visible { get; set; }
      [DataMember] public bool castShadow { get; set; }
      [DataMember] public bool receiveShadow { get; set; }
      [DataMember] public bool doubleSided { get; set; }
    }

    [DataContract]
    public class Va3cGeometry
    {
      [DataMember] public string uuid { get; set; }
      [DataMember] public string type { get; set; } // "Geometry"
      [DataMember] public Va3cGeometryData data { get; set; }
      //[DataMember] public double scale { get; set; }
      [DataMember] public List<Va3cMaterial> materials { get; set; }
    }

    [DataContract]
    public class Va3cObject
    {
      [DataMember] public string uuid { get; set; }
      [DataMember] public string type { get; set; }
      [DataMember] public double[] matrix { get; set; }
      [DataMember] public List<Va3cObject> children { get; set; }
      // The following are only on the children:

      [DataMember] public string geometry { get; set; }
      [DataMember] public string name { get; set; }
      [DataMember] public string material { get; set; }
      //[DataMember] public List<double> position { get; set; }
      //[DataMember] public List<double> rotation { get; set; }
      //[DataMember] public List<double> quaternion { get; set; }
      //[DataMember] public List<double> scale { get; set; }
      //[DataMember] public bool visible { get; set; }
      //[DataMember] public bool castShadow { get; set; }
      //[DataMember] public bool receiveShadow { get; set; }
      //[DataMember] public bool doubleSided { get; set; }
    }

    // https://github.com/mrdoob/three.js/wiki/JSON-Model-format-3

    // for the faces, we will use
    // triangle with material
    // 00 00 00 10 = 2
    // 2, [vertex_index, vertex_index, vertex_index], [material_index]     // e.g.:
    //
    //2, 0,1,2, 0

    public class SceneMetadata
    {
      [DataMember] public string type { get; set; } //  "Object"
      [DataMember] public double version { get; set; } // 4.3
      [DataMember] public string generator { get; set; } //  "RvtVa3c Revit va3c exporter"
      //[DataMember] public int vertices { get; set; } //  770
      //[DataMember] public int faces { get; set; } //  768
      //[DataMember] public int normals { get; set; } //  770
      //[DataMember] public int colors { get; set; } //  0
      //[DataMember] public int uvs { get; set; } //  0
      //[DataMember] public int materials { get; set; } //  1
      //[DataMember] public int morphTargets { get; set; } //  0
    }

    [DataMember] public SceneMetadata metadata { get; set; }
    //[DataMember] public Dictionary<string, Va3cObject> objects { get; set; }
    [DataMember(Name="object")] public Va3cObject obj { get; set; }
    [DataMember] public List<Va3cGeometry> geometries;
    [DataMember] public List<Va3cMaterial> materials;
  }
}
