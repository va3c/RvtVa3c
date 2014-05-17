using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.Utility;
using Autodesk.Revit.DB;

namespace RvtVa3c
{
  internal class Va3cMaterial
  {
    #region Declarations
    internal ElementId Id { get; set; }
    internal String Name { get; set; }
    internal Dictionary<string, AssetProperty> Properties { get; set; }
    #endregion

    #region Constructors
    internal Va3cMaterial( Material m )
    {
      Id = m.Id;
      Name = m.Name;
      Properties = new Dictionary<string, AssetProperty>();

      extractProperties( m );

      List<AssetPropertyString> images =
      Properties.Values.Where( p => p.Type == AssetPropertyType.APT_String ).Cast<AssetPropertyString>().Where( p => p.Value.ToUpper().EndsWith( ".PNG" ) ).ToList();
    }
    #endregion

    #region Private Methods
    private void extractProperties( Material m )
    {
      if( m.AppearanceAssetId.IntegerValue < 0 ) return;

      AppearanceAssetElement assetElem = m.Document.GetElement( m.AppearanceAssetId ) as AppearanceAssetElement;

      Asset asset = assetElem.GetRenderingAsset();

      extractProperty( String.Empty, asset );

    }

    private void extractProperty( string parent, AssetProperty prop )
    {
      if( Properties.ContainsKey( prop.Name ) )
      {
        string name = parent + "." + prop.Name;
      }
      else
      {
        Properties.Add( prop.Name, prop );
      }

      switch( prop.Type )
      {
        case AssetPropertyType.APT_Asset:
          Asset asset = prop as Asset;
          for( int i = 0; i < asset.Size; i++ ) extractProperty( parent + asset.Name, asset[i] );
          break;

        case AssetPropertyType.APT_List:
          AssetPropertyList list = prop as AssetPropertyList;
          IList<AssetProperty> nested = list.GetValue();
          if( ( nested != null ) && ( nested.Count > 0 ) )
          {
            foreach( AssetProperty sub in nested ) extractProperty( parent + list.Name, sub );
          }
          break;

        case AssetPropertyType.APT_Properties:
          AssetProperties props = prop as AssetProperties;
          for( int i = 0; i < props.Size; i++ ) extractProperty( parent + props.Name, props[i] );
          break;
      }

      IList<AssetProperty> connected = prop.GetAllConnectedProperties();
      if( ( connected != null ) && ( connected.Count > 0 ) )
      {
        for( int i = 0; i < connected.Count; i++ ) extractProperty( parent + prop.Name, connected[i] );
      }
    }
    #endregion
  }

   
}
