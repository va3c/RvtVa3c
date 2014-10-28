#region Namespaces
using System;
using System.IO;
using System.Reflection;
#endregion

namespace RvtVa3c
{
  class UserSettings
  {
    const string _JsonIndent = "JsonIndent";

    const string _error_msg_format
      = "Invalid settings in '{0}'; "
      + "please add '{1}' = {2} or {3}";

    public static bool JsonIndented
    {
      get
      {
        string path = Assembly.GetExecutingAssembly()
          .Location;

        path = Path.ChangeExtension( path, "txt" );

        if( !File.Exists( path ) )
        {
          File.WriteAllText( path,
            _JsonIndent + "=" + Boolean.TrueString );

          Util.ErrorMsg( string.Format(
            "Created a new user settings file at '{0}'.",
            path ) );
        }

        string s = File.ReadAllText( path );

        int i = s.IndexOf( _JsonIndent );

        if( 0 > i )
        {
          Util.ErrorMsg( string.Format(
            _error_msg_format, path, _JsonIndent, 
            Boolean.TrueString, Boolean.FalseString ) );

          return false;
        }

        s = s.Substring( i + _JsonIndent.Length );

        i = s.IndexOf( '=' );

        if( 0 > i )
        {
          Util.ErrorMsg( string.Format(
            _error_msg_format, path, _JsonIndent,
            Boolean.TrueString, Boolean.FalseString ) );

          return false;
        }

        s = s.Substring( i + 1 ).Trim();

        return Util.GetTrueOrFalse( s );
      }
    }
  }
}
