using Autodesk.Revit.DB;
using System;

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
  }
}
