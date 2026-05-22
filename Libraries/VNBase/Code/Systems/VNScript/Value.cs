using System.Linq;
using System.Globalization;
using System.Collections.Generic;

namespace VNScript;

public abstract record Value
{
	public const char GlobalPrefix = '@';
	
	public virtual Value Evaluate( IEnvironment environment )
	{
		return this;
	}
	
	public delegate Value FunctionMapping( IEnvironment environment, params Value[] values );
	
	/// <summary>
	/// Represents a null value, that is the absence of a value.
	/// </summary>
	public record NoneValue : Value
	{
		private NoneValue() { }
		public static readonly NoneValue None = new();
	}
	
	/// <summary>
	/// Represents symbols, that is anything that isn't a number or a string and hasn't been dereferenced to a variable yet
	/// </summary>
	public record VariableReferenceValue( string Name ) : Value
	{
		public override Value Evaluate( IEnvironment environment )
		{
			if ( GlobalEnvironment.Map.VariableSet().Contains( Name ) )
			{
				return GlobalEnvironment.Map.GetVariable( Name );
			}
			
			if ( environment.VariableSet().Contains( Name ) )
			{
				return environment.GetVariable( Name );
			}
			
			if ( BooleanValue.BooleanMap.TryGetValue( Name, out var value ) )
			{
				return new BooleanValue( value );
			}
			
			if ( BuiltinFunctions.Builtins.TryGetValue( Name, out var builtin ) )
			{
				return builtin;
			}
			
			throw new UndefinedVariableException( Name );
		}
	}
	
	/// <summary>
	/// Represents a true or false value, a boolean.
	/// </summary>
	public record BooleanValue( bool Boolean ) : Value
	{
		public override string ToString()
		{
			return Boolean.ToString().ToLower();
		}
		
		public static Dictionary<string, bool> BooleanMap { get; } = new()
		{
			["true"] = true,
			["false"] = false,
		};
	}
	
	public record StringValue( string Text ) : Value
	{
		public override string ToString()
		{
			return Text;
		}
	}
	
	public record NumberValue( decimal Number ) : Value
	{
		public override string ToString()
		{
			return Number.ToString( CultureInfo.CurrentCulture );
		}
	}
	
	public record FunctionValue( FunctionMapping Function ) : Value;
	
	public record ListValue( SParen ValueList ) : Value
	{
		public override Value Evaluate( IEnvironment environment )
		{
			if ( ValueList.Count <= 0 )
			{
				return this;
			}
			
			var firstValue = ValueList[0];
			
			if ( firstValue is VariableReferenceValue variableRefValue )
			{
				firstValue = variableRefValue.Evaluate( environment );
			}
			
			if ( firstValue is FunctionValue functionValue )
			{
				return functionValue.Function( environment, ValueList.Skip( 1 ).ToArray() );
			}
			
			return this;
		}
		
		public override string ToString()
		{
			return ValueList.ToString() ?? "[]";
		}
	}
	
	public bool IsTruthy()
	{
		return this switch
		{
			BooleanValue boolean => boolean.Boolean,
			NumberValue number => number.Number != 0,
			NoneValue => false,
			_ => true
		};
	}
}
