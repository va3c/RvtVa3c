#region Namespaces
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
#endregion // Namespaces

namespace RvtVa3c
{
  /// <summary>
  /// three.js object class, successor of Va3cScene.
  /// The structure and properties defined here were
  /// reverse engineered ftom JSON files exported 
  /// by the three.js and vA3C editors.
  /// </summary>
  [DataContract]
  public class Va3cContainer
  {
    /// <summary>
    /// Based on MeshPhongMaterial obtained by exporting a cube from the thr
    /// </summary>
    public class Va3cMaterial
    {
      [DataMember]
      public string uuid { get; set; }
      [DataMember]
      public string name { get; set; }
      [DataMember]
      public string type { get; set; } // MeshPhongMaterial
      [DataMember]
      public int color { get; set; } // 16777215
      [DataMember]
      public int ambient { get; set; } //16777215
      [DataMember]
      public int emissive { get; set; } // 1
      [DataMember]
      public int specular { get; set; } //1118481
      [DataMember]
      public int shininess { get; set; } // 30
      [DataMember]
      public double opacity { get; set; } // 1
      [DataMember]
      public bool transparent { get; set; } // false
      [DataMember]
      public bool wireframe { get; set; } // false
    }

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

      [DataMember]
      public List<double> vertices { get; set; } // millimetres
      // "morphTargets": []
      [DataMember]
      public List<double> normals { get; set; }
      // "colors": []
      [DataMember]
      public List<double> uvs { get; set; }
      [DataMember]
      public List<int> faces { get; set; } // indices into Vertices + Materials
      [DataMember]
      public double scale { get; set; }
      [DataMember]
      public bool visible { get; set; }
      [DataMember]
      public bool castShadow { get; set; }
      [DataMember]
      public bool receiveShadow { get; set; }
      [DataMember]
      public bool doubleSided { get; set; }
    }

    [DataContract]
    public class Va3cGeometry
    {
      [DataMember]
      public string uuid { get; set; }
      [DataMember]
      public string type { get; set; } // "Geometry"
      [DataMember]
      public Va3cGeometryData data { get; set; }
      //[DataMember] public double scale { get; set; }
      [DataMember]
      public List<Va3cMaterial> materials { get; set; }
    }

    [DataContract]
    public class Va3cObject
    {
      [DataMember]
      public string uuid { get; set; }
      [DataMember]
      public string name { get; set; } // BIM <document name>
      [DataMember]
      public string type { get; set; } // Object3D
      [DataMember]
      public double[] matrix { get; set; } // [1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1]
      [DataMember]
      public List<Va3cObject> children { get; set; }

      // The following are only on the children:

      [DataMember]
      public string geometry { get; set; }
      [DataMember]
      public string material { get; set; }
      
      //[DataMember] public List<double> position { get; set; }
      //[DataMember] public List<double> rotation { get; set; }
      //[DataMember] public List<double> quaternion { get; set; }
      //[DataMember] public List<double> scale { get; set; }
      //[DataMember] public bool visible { get; set; }
      //[DataMember] public bool castShadow { get; set; }
      //[DataMember] public bool receiveShadow { get; set; }
      //[DataMember] public bool doubleSided { get; set; }
      
      [DataMember]
      public Dictionary<string, string> userData { get; set; }
    }

    // https://github.com/mrdoob/three.js/wiki/JSON-Model-format-3

    // for the faces, we will use
    // triangle with material
    // 00 00 00 10 = 2
    // 2, [vertex_index, vertex_index, vertex_index], [material_index]     // e.g.:
    //
    //2, 0,1,2, 0

    public class Metadata
    {
      [DataMember]
      public string type { get; set; } //  "Object"
      [DataMember]
      public double version { get; set; } // 4.3
      [DataMember]
      public string generator { get; set; } //  "RvtVa3c Revit vA3C exporter"
    }

    [DataMember]
    public Metadata metadata { get; set; }
    [DataMember( Name = "object" )]
    public Va3cObject obj { get; set; }
    [DataMember]
    public List<Va3cGeometry> geometries;
    [DataMember]
    public List<Va3cMaterial> materials;
  }
}
