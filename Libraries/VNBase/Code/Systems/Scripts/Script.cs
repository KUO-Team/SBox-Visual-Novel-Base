using Sandbox;
using Sandbox.Diagnostics;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using VNScript;

// ReSharper disable VirtualMemberCallInConstructor

namespace VNBase.Assets;

/// <summary>
/// Defines a VNBase script asset.
/// </summary>
public class Script : IAsset
{
	// ReSharper disable once MemberCanBeProtected.Global
	public virtual string Code { get; set; } = string.Empty;
	
	/// <summary>
	/// The script to run after this one has finished.
	/// </summary>
	public virtual Script? NextScript { get; set; }
	
	/// <summary>
	/// If this script is initialized from a file,
	/// this is the path to that script file.
	/// </summary>
	[FilePath]
	public string Path { get; set; } = string.Empty;
	
	/// <summary>
	/// If this script was initialized from a file or not.
	/// </summary>
	public bool FromFile => !string.IsNullOrEmpty( Path );
	
	/// <summary>
	/// Called when a choice is selected from this script.
	/// </summary>
	[Hide]
	public Action<VNScript.Script.Choice>? OnChoiceSelected { get; set; }
	
	[Hide]
	private EnvironmentMap? _environment;
	
	[Hide]
	private VNScript.Script? _parsedScript;
	
	[JsonIgnore, Hide]
	private static readonly Logger Log = new( "Script" );
	
	/// <summary>
	/// Create a new empty script.
	/// </summary>
	public Script() { }
	
	/// <summary>
	/// Create a new script from a file.
	/// </summary>
	/// <param name="path">The path to the script file.</param>
	public Script( string path )
	{
		if ( !FileSystem.Mounted.FileExists( path ) )
		{
			Log.Error( $"Unable to load script! Script file couldn't be found by path: {path}" );
			return;
		}
		
		Code = FileSystem.Mounted.ReadAllText( path );
		Path = path;
	}
	
	/// <summary>
	/// Create a new script from a file.
	/// </summary>
	/// <param name="path">The path to the script file.</param>
	/// <param name="nextScript">The next script to run after this one has finished.</param>
	public Script( string path, Script nextScript ) : this( path )
	{
		NextScript = nextScript;
	}
	
	/// <summary>
	/// Called when the script is loaded by the <see cref="ScriptPlayer"/>
	/// </summary>
	public virtual void OnLoad() { }
	
	/// <summary>
	/// Called after the script has finished executing by the <see cref="ScriptPlayer"/>
	/// </summary>
	public virtual void OnUnload() { }
	
	/// <summary>
	/// Get this script's local environment map.
	/// </summary>
	public virtual IEnvironment GetEnvironment()
	{
		if ( _environment is not null )
		{
			return _environment;
		}
		
		// Create new environment
		_environment = new EnvironmentMap( new Dictionary<string, Value>() );
		
		// If we have a parsed script, load its variables into the environment
		if ( _parsedScript is not null )
		{
			foreach ( var kvp in _parsedScript.Variables )
			{
				// Extract variable name from the key
				var varName = kvp.Key switch
				{
					Value.VariableReferenceValue varRef => varRef.Name, Value.StringValue str => str.Text, _ => null
				};
				
				if ( string.IsNullOrEmpty( varName ) )
				{
					continue;
				}
				
				// Evaluate the value in the current environment and store it
				var evaluatedValue = kvp.Value.Evaluate( _environment );
				_environment.SetVariable( varName, evaluatedValue );
			}
		}
		
		return _environment;
	}
	
	internal virtual VNScript.Script Parse()
	{
		var codeBlocks = SParen.ParseText( Code ).ToList();
		_parsedScript = VNScript.Script.ParseScript( codeBlocks );
		
		// Reset environment so it gets rebuilt
		_environment = null;
		
		return _parsedScript;
	}
}
