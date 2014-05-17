using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RvtVa3c
{
  /// <summary>
  /// three.js scene class
  /// </summary>
  [DataContract]
  public class Va3cScene
  {
    public class SceneMetadata
    {
      [DataMember]
      public double formatVersion { get; set; } //  3,
      [DataMember]
      public string generatedBy { get; set; } //  "Blender 2.62 Exporter",
      [DataMember]
      public int vertices { get; set; } //  770,
      [DataMember]
      public int faces { get; set; } //  768,
      [DataMember]
      public int normals { get; set; } //  770,
      [DataMember]
      public int colors { get; set; } //  0,
      [DataMember]
      public int uvs { get; set; } //  0,
      [DataMember]
      public int materials { get; set; } //  1,
      [DataMember]
      public int morphTargets { get; set; } //  0
    }

    public class SceneMaterialMetadata
    {
        [DataMember]
        public string Version { get; set; }
        [DataMember]
        public string type { get; set;}
        [DataMember]
        public string generator { get; set; }
    }
    public class SceneMaterial
    {
        [DataMember]
        public SceneMaterialMetadata metadata { get; set; }

        [DataMember]
        public String type { get; set; } //MeshPhongMaterial
       
      [DataMember]
      public int color { get; set; } // 16777215,
         [DataMember]
        public int ambient { get; set; } //16777215
      [DataMember]
      public int emissive { get; set; } // 1,
      [DataMember]
      public int specular { get; set; } //1118481
      [DataMember]
      public int shininess { get; set; } // 30,
      [DataMember]
      public int opacity { get; set; } // 1
      [DataMember]
      public bool transparent { get; set; } // false
      [DataMember]
      public bool wireframe { get; set; } // false
    
    }

          //  MeshPhongMaterial from https://github.com/mrdoob/three.js/wiki/JSON-Material-format-4

    //{
    //"metadata": {
    //    "version": 4.2,
    //    "type": "material",
    //    "generator": "MaterialExporter"
    //},
    //"type": "MeshPhongMaterial",
    //"color": 16777215,
    //"ambient": 16777215,
    //"emissive": 0,
    //"specular": 1118481,
    //"shininess": 30,
    //"opacity": 1,
    //"transparent": false,

    [DataContract]
    public class Va3cObject
    {
        [DataMember]
        public String geometry { get; set; }

        [DataMember]
        public List<double> position { get; set; }

        [DataMember]
        public List<double> rotation { get; set; }

        [DataMember]
        public List<double> quaternion { get; set; }

        [DataMember]
        public List<double> scale { get; set; }

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
    public double scale { get; set; }
    [DataMember]
    public List<SceneMaterial> materials { get; set; }
    [DataMember]
    public List<double> vertices { get; set; } // XYZ list
    // "morphTargets": [],
    [DataMember]
    public List<double> normals { get; set; } // XYZ list
    // "colors": [],
    // "uvs": [[]],
    [DataMember]
    public List<int> faces { get; set; } // indices into Vertices + Materials
 
      }
    // https://github.com/mrdoob/three.js/wiki/JSON-Model-format-3

    // for the faces, we will use
    // triangle with material
    // 00 00 00 10 = 2
    // 2, [vertex_index, vertex_index, vertex_index], [material_index]
    // e.g.:
    //
    //2, 0,1,2, 0,

    [DataMember]
    public SceneMetadata metadata { get; set; }
  

    [DataMember]
    public Dictionary<string,Va3cObject> objects { get; set; }

    [DataMember]
    public Dictionary<string, Va3cGeometry> geometries;
  }


}
