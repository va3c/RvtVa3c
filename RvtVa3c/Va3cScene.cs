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

    public class SceneMaterial
    {
      [DataMember]
      public double DbgColor { get; set; } // 15597568,
      [DataMember]
      public double DbgIndex { get; set; } // 1,
      [DataMember]
      public double DbgName { get; set; } // "HMI- Polished Al",
      [DataMember]
      public double blending { get; set; } // "NormalBlending",
      [DataMember]
      public double colorAmbient { get; set; } // [0.3513725571538888, 0.3513725571538888, 0.3513725571538888],
      [DataMember]
      public double colorDiffuse { get; set; } // [0.3513725571538888, 0.3513725571538888, 0.3513725571538888],
      [DataMember]
      public double colorSpecular { get; set; } // [0.4490196108818054, 0.4490196108818054, 0.4490196108818054],
      [DataMember]
      public double depthTest { get; set; } // true,
      [DataMember]
      public double depthWrite { get; set; } // true,
      [DataMember]
      public double shading { get; set; } // "Lambert",
      [DataMember]
      public double specularCoef { get; set; } // 50,
      [DataMember]
      public double transparency { get; set; } // 1.0,
      [DataMember]
      public double transparent { get; set; } // false,
      [DataMember]
      public double vertexColors { get; set; } // false
      [DataMember]
      public string Id { get; set; } // Revit Unique Id
    }

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

    [DataMember]
    public List<Va3cObject> objects { get; set; }
  }


}
