using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;

namespace RvtVa3c
{
  class Util
  {
    /// <summary>
    /// Return a string for a real number
    /// formatted to two decimal places.
    /// </summary>
    public static string RealString( double a )
    {
      return a.ToString( "0.##" );
    }

    /// <summary>
    /// Return a string for an XYZ point
    /// or vector with its coordinates
    /// formatted to two decimal places.
    /// </summary>
    public static string PointString( XYZ p )
    {
      return string.Format( "({0},{1},{2})",
        RealString( p.X ),
        RealString( p.Y ),
        RealString( p.Z ) );
    }

    /// <summary>
    /// Return an integer value for a Revit Color.
    /// </summary>
    public static int ColorToInt( Color color )
    {
      return ( (int) color.Red ) << 16
        | ( (int) color.Green ) << 8
        | (int) color.Blue;
    }

    /// <summary>
    /// Return a string describing the given element:
    /// .NET type name,
    /// category name,
    /// family and symbol name for a family instance,
    /// element id and element name.
    /// </summary>
    public static string ElementDescription(
      Element e )
    {
      if( null == e )
      {
        return "<null>";
      }

      // For a wall, the element name equals the
      // wall type name, which is equivalent to the
      // family name ...

      FamilyInstance fi = e as FamilyInstance;

      string typeName = e.GetType().Name;

      string categoryName = ( null == e.Category )
        ? string.Empty
        : e.Category.Name + " ";

      string familyName = ( null == fi )
        ? string.Empty
        : fi.Symbol.Family.Name + " ";

      string symbolName = ( null == fi
        || e.Name.Equals( fi.Symbol.Name ) )
          ? string.Empty
          : fi.Symbol.Name + " ";

      return string.Format( "{0} {1}{2}{3}<{4} {5}>",
        typeName, categoryName, familyName,
        symbolName, e.Id.IntegerValue, e.Name );
    }
    
    /// <summary>
    /// Return a dictionary of all the given 
    /// element parameter names and values.
    /// </summary>
    public static Dictionary<string, string>
      GetElementProperties(
        Element e,
        bool includeType )
    {
      IList<Parameter> parameters
        = e.GetOrderedParameters();

      Dictionary<string, string> a
        = new Dictionary<string, string>(
          parameters.Count );

      string key;
      string val;

      foreach( Parameter p in parameters )
      {
        key = p.Definition.Name;

        if( !a.ContainsKey( key ) )
        {
          val = p.AsValueString();

          if( !string.IsNullOrEmpty( val ) )
          {
            a.Add( key, val );
          }
        }
      }

      if( includeType )
      {
        ElementId idType = e.GetTypeId();

        if( ElementId.InvalidElementId != idType )
        {
          Document doc = e.Document;
          Element typ = doc.GetElement( idType );
          parameters = typ.GetOrderedParameters();
          foreach( Parameter p in parameters )
          {
            key = "Type " + p.Definition.Name;

            if( !a.ContainsKey( key ) )
            {
              val = p.AsValueString();

              if( !string.IsNullOrEmpty( val ) )
              {
                a.Add( key, val );
              }
            }
          }
        }
      }
      return a;
    }
  }
}
