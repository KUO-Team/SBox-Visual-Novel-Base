using System;
using System.IO;
using Editor;
using Sandbox;

namespace VNBase.Editor;

public static class EditorEvents
{
	private const string DefaultFileName = "Untitled";
	private const string Extension = ".vnscript";

	[Event( "folder.contextmenu" )]
	private static void FolderContextMenu( FolderContextMenu obj )
	{
		if ( obj.Context is not AssetList assetList )
		{
			return;
		}

		var newMenu = obj.Menu.FindOrCreateMenu( "New" );
		if ( !newMenu.IsValid() )
		{
			return;
		}

		var browser = assetList.Browser;
		var option = new Option( "VNScript", "description", () => CreateNewFile( browser ) );
		newMenu.AddOption( option );
	}

	private static void CreateNewFile( IBrowser browser )
	{
		var directoryPath = browser.CurrentLocation.Path;
		if ( !Directory.Exists( directoryPath ) )
		{
			Log.Error( $"Directory does not exist: {directoryPath}" );
			return;
		}

		var defaultPath = Path.Combine( directoryPath, DefaultFileName + Extension );
		try
		{
			var chosenPath = EditorUtility.SaveFileDialog( "Save vnscript file...", Extension, defaultPath );
			if ( string.IsNullOrWhiteSpace( chosenPath ) )
			{
				return;
			}
			
			File.WriteAllText( chosenPath, GetScriptTemplate() );
			Log.Info( $"Created new VNScript file at: {chosenPath}" );
		}
		catch ( Exception ex )
		{
			Log.Error( $"Failed to create VNScript file: {ex}" );
		}
	}

	private static string GetScriptTemplate()
	{
		return """
		       // This is a basic vnscript template.
		       // For more information, please refer to the wiki: https://github.com/KUO-Team/SBox-Visual-Novel-Base/wiki/How-To-Write-Your-First-Script
		       (label start
		           (text "This is a starting example script!")
		           (after end-dialogue)
		       )

		       (start-dialogue start)
		       """;
	}
}
