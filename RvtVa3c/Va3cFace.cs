using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;

namespace RvtVa3c
{
  internal class Va3cFace
  {
    internal Element Element { get; set; }
    internal int CategoryId { get; set; }
    internal int ElementId { get; set; }
    internal uint FaceId { get; set; }
    internal ElementId MaterialId { get; set; }

    internal Va3cFace( Element e, uint id, ElementId material )
    {
      Element = e;
      if( e.Category != null )
      {
        CategoryId = e.Category.Id.IntegerValue;
      }
      else
      {
        CategoryId = -1;
      }
      FaceId = id;
      MaterialId = material;
      ElementId = e.Id.IntegerValue;
    }
  }
}
