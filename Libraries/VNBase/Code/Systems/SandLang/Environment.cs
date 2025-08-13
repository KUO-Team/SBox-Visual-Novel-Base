using System.Linq;
using System.Collections.Generic;
using Sandbox.Diagnostics;

namespace SandLang;

/// <summary>
/// Interface for an environment that can store and retrieve variables.
/// </summary>
public interface IEnvironment
{
	public Value GetVariable( string name );
	public void SetVariable( string name, Value value );
	public IEnumerable<string> VariableSet();
	public Dictionary<string, Value> GetVariables();
}

/// <summary>
/// A map of environment variables.
/// </summary>
public class EnvironmentMap( Dictionary<string, Value> variables ) : IEnvironment
{
	protected readonly Dictionary<string, Value> Variables = variables;
	protected static Logger Log { get; } = new( "Environment" );

	public EnvironmentMap() : this( new Dictionary<string, Value>() ) { }

	public EnvironmentMap( IEnvironment copy ) : this()
	{
		foreach ( var key in copy.VariableSet() )
		{
			SetVariable( key, copy.GetVariable( key ) );
		}
	}

	public override string ToString()
	{
		return VariableSet().Select( k => '"' + k + '"' + " = " + GetVariable( k ) )
			.Aggregate( ( acc, v ) => acc + '\n' + v ).Trim();
	}

	public Value GetVariable( string name )
	{
		if ( GlobalEnvironment.Map.VariableSet().Contains( name ) )
		{
			return GlobalEnvironment.Map.Variables[name];
		}

		if ( !Variables.TryGetValue( name, out var value ) )
		{
			throw new UndefinedVariableException( name );
		}

		return value;
	}

	public void SetVariable( string name, Value value )
	{
		if ( name.StartsWith( Value.GlobalPrefix ) )
		{
			GlobalEnvironment.Map.Variables[name] = value;
			return;
		}

		Variables[name] = value;
	}

	public IEnumerable<string> VariableSet()
	{
		return Variables.Keys;
	}

	public Dictionary<string, Value> GetVariables()
	{
		return Variables;
	}
}

/// <summary>
/// The global environment.
/// </summary>
public static class GlobalEnvironment
{
	/// <summary>
	/// Provides access to the global environment map.
	/// </summary>
	public static EnvironmentMap Map { get; private set; } = new();

	/// <summary>
	/// Clears the global environment.
	/// </summary>
	public static void Clear() => Map = new EnvironmentMap();
}
