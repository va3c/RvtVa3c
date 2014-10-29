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
      = "Invalid settings in '{0}':\r\n\r\n{1}"
      + "\r\n\r\nPlease add {2} = {3} or {4}.";

    static bool SyntaxError( string path, string s )
    {
      Util.ErrorMsg( string.Format(
        _error_msg_format, path, s, _JsonIndent,
        Boolean.TrueString, Boolean.FalseString ) );

      return false;
    }

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

        string s1 = File.ReadAllText( path );

        int i = s1.IndexOf( _JsonIndent );

        if( 0 > i )
        {
          return SyntaxError( path, s1 );
        }

        string s = s1.Substring( i
          + _JsonIndent.Length );

        i = s.IndexOf( '=' );

        if( 0 > i )
        {
          return SyntaxError( path, s1 );
        }

        s = s.Substring( i + 1 ).Trim();

        bool rc;

        if( !Util.GetTrueOrFalse( s, out rc ) )
        {
          return SyntaxError( path, s1 );
        }

        return rc;
      }
    }
  }
}
