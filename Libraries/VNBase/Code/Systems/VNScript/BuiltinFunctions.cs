using System;
using System.Linq;
using System.Collections.Generic;

namespace VNScript;

internal static class BuiltinFunctions
{
	/// <summary>
	/// Contains mappings from symbols to builtin executable functions
	/// </summary>
	public static Dictionary<string, Value.FunctionValue> Builtins { get; } = new()
	{
		["="] = new Value.FunctionValue( EqualityFunction ),
		["!="] = new Value.FunctionValue( NotEqualFunction ),
		[">"] = new Value.FunctionValue( GreaterThanFunction ),
		["<"] = new Value.FunctionValue( LessThanFunction ),
		[">="] = new Value.FunctionValue( GreaterThanOrEqualFunction ),
		["<="] = new Value.FunctionValue( LessThanOrEqualFunction ),
		["+"] = new Value.FunctionValue( SumFunction ),
		["-"] = new Value.FunctionValue( SubtractFunction ),
		["*"] = new Value.FunctionValue( MulFunction ),
		["set"] = new Value.FunctionValue( SetFunction ),
		["defun"] = new Value.FunctionValue( DefineFunction ),
		["pow"] = new Value.FunctionValue( PowFunction ),
		["sqrt"] = new Value.FunctionValue( SqrtFunction ),
		["if"] = new Value.FunctionValue( IfFunction ),
		["not"] = new Value.FunctionValue( NotFunction ),
		["and"]  = new Value.FunctionValue( AndFunction ),
		["or"]   = new Value.FunctionValue( OrFunction ),
		["xor"]  = new Value.FunctionValue( XorFunction ),
		["nand"] = new Value.FunctionValue( NandFunction ),
		["nor"]  = new Value.FunctionValue( NorFunction ),
		["when"] = new Value.FunctionValue( WhenFunction ),
		["body"] = new Value.FunctionValue( ExpressionBodyFunction ),
	};
	
	private static Value.BooleanValue GreaterThanFunction( IEnvironment env, Value[] values )
	{
		var ( a, b ) = GetTwoNumbers( env, values, ">" );
		return new Value.BooleanValue( a > b );
	}
	
	private static Value.BooleanValue LessThanFunction( IEnvironment env, Value[] values )
	{
		var ( a, b ) = GetTwoNumbers( env, values, "<" );
		return new Value.BooleanValue( a < b );
	}
	
	private static Value.BooleanValue GreaterThanOrEqualFunction( IEnvironment environment, Value[] values )
	{
		var ( a, b ) = GetTwoNumbers( environment, values, ">=" );
		return new Value.BooleanValue( a >= b );
	}
	
	private static Value.BooleanValue LessThanOrEqualFunction( IEnvironment environment, Value[] values )
	{
		var ( a, b ) = GetTwoNumbers( environment, values, "<=" );
		return new Value.BooleanValue( a <= b );
	}
	
	private static Value.BooleanValue EqualityFunction( IEnvironment environment, Value[] values )
	{
		if ( values.Length != 2 )
		{
			throw new InvalidParametersException( values );
		}
		
		var v1 = values[0].Evaluate( environment );
		var v2 = values[1].Evaluate( environment );
		
		if ( v1.GetType() != v2.GetType() )
		{
			return new Value.BooleanValue( false );
		}
		
		return v1 switch
		{
			Value.BooleanValue bool1 => new Value.BooleanValue( bool1.Boolean.Equals( ((Value.BooleanValue)v2).Boolean ) ),
			Value.NumberValue num1 => new Value.BooleanValue( num1.Number.Equals( ((Value.NumberValue)v2).Number ) ),
			Value.StringValue str1 => new Value.BooleanValue( str1.Text.Equals( ((Value.StringValue)v2).Text ) ),
			_ => new Value.BooleanValue( false )
		};
	}
	
	private static Value.BooleanValue NotEqualFunction( IEnvironment environment, Value[] values )
	{
		var eq = EqualityFunction( environment, values );
		return new Value.BooleanValue( !eq.Boolean );
	}
	
	private static Value ExpressionBodyFunction( IEnvironment environment, Value[] values )
	{
		if ( values.Length == 0 )
		{
			return Value.NoneValue.None;
		}
		
		Value lastValue = Value.NoneValue.None;
		
		foreach ( var expression in values )
		{
			lastValue = expression.Evaluate( environment );
		}
		
		return lastValue;
	}
	
	private static Value IfFunction( IEnvironment environment, Value[] values )
	{
		if ( values.Length is < 2 or > 3 )
		{
			throw ParamError.Wrong( "if", "(if condition then [else])", values );
		}
		
		var condition = values[0].Evaluate( environment );
		
		if ( condition.IsTruthy() )
		{
			return values[1].Evaluate( environment );
		}
		
		return values.Length == 3 ? values[2].Evaluate( environment ) : Value.NoneValue.None;
	}
	
	private static Value.BooleanValue NotFunction( IEnvironment environment, Value[] values )
	{
		if ( values.Length != 1 )
		{
			throw ParamError.Wrong( "not", "one value", values );
		}
		
		return new Value.BooleanValue( !values[0].Evaluate( environment ).IsTruthy() );
	}
	
	private static Value.BooleanValue AndFunction( IEnvironment environment, Value[] values )
	{
		foreach ( var expr in values )
		{
			var result = expr.Evaluate( environment );
			if ( !result.IsTruthy() )
			{
				return new Value.BooleanValue( false );
			}
		}
		
		return new Value.BooleanValue( true );
	}
	
	private static Value.BooleanValue OrFunction( IEnvironment environment, Value[] values )
	{
		foreach ( var expr in values )
		{
			var result = expr.Evaluate( environment );
			if ( result.IsTruthy() )
			{
				return new Value.BooleanValue( true );
			}
		}
		
		return new Value.BooleanValue( false );
	}
	
	private static Value.BooleanValue XorFunction( IEnvironment environment, Value[] values )
	{
		var result = false;
		
		foreach ( var expr in values )
		{
			if ( expr.Evaluate( environment ).IsTruthy() )
			{
				result = !result;
			}
		}
		
		return new Value.BooleanValue( result );
	}
	
	private static Value.BooleanValue NandFunction( IEnvironment environment, Value[] values )
	{
		return new Value.BooleanValue( !AndFunction( environment, values ).Boolean );
	}
	
	private static Value.BooleanValue NorFunction( IEnvironment environment, Value[] values )
	{
		return new Value.BooleanValue( !OrFunction( environment, values ).Boolean );
	}
	
	private static Value WhenFunction( IEnvironment environment, Value[] values )
	{
		if ( values.Length < 2 )
		{
			throw new InvalidParametersException( values );
		}
		
		var condition = values[0].Evaluate( environment );
		
		if ( !condition.IsTruthy() )
		{
			return Value.NoneValue.None;
		}
		
		Value last = Value.NoneValue.None;
		
		for ( var i = 1; i < values.Length; i++ )
		{
			last = values[i].Evaluate( environment );
		}
		
		return last;
	}
	
	private static Value.NumberValue MulFunction( IEnvironment environment, Value[] values )
	{
		if ( values.Length == 0 )
		{
			return new Value.NumberValue( 1 ); // Identity for multiplication
		}
		
		var evaluatedValues = values.Select( v => v.Evaluate( environment ) ).ToArray();
		
		if ( !evaluatedValues.All( v => v is Value.NumberValue ) )
		{
			throw new InvalidParametersException( evaluatedValues );
		}
		
		var result = evaluatedValues
			.Cast<Value.NumberValue>()
			.Select( nv => nv.Number )
			.Aggregate( ( acc, v ) => acc * v );
		
		return new Value.NumberValue( result );
	}
	
	private static Value.NumberValue PowFunction( IEnvironment environment, Value[] values )
	{
		if ( values.Length != 2 )
		{
			throw new InvalidParametersException( values );
		}
		
		var baseVal = values[0].Evaluate( environment );
		var exponentVal = values[1].Evaluate( environment );
		
		if ( baseVal is not Value.NumberValue baseNum || exponentVal is not Value.NumberValue expNum )
		{
			throw new InvalidParametersException( [baseVal, exponentVal] );
		}
		
		// This can introduce precision artifacts.
		return new Value.NumberValue( new decimal( Math.Pow( (double)baseNum.Number, (double)expNum.Number ) ) );
	}
	
	private static Value.NumberValue SqrtFunction( IEnvironment environment, Value[] values )
	{
		if ( values.Length != 1 )
		{
			throw new InvalidParametersException( values );
		}
		
		var val = values[0].Evaluate( environment );
		
		if ( val is not Value.NumberValue numVal || numVal.Number < 0 )
		{
			throw new InvalidParametersException( [val] );
		}
		
		return new Value.NumberValue( new decimal( Math.Sqrt( (double)numVal.Number ) ) );
	}

	internal static Value.FunctionValue DefineFunction( IEnvironment environment, Value[] values )
	{
		// Expect: (defun function-name (param1 param2 ...) (body))
		if ( values.Length != 3 )
		{
			throw ParamError.Wrong( "defun", "(defun name (params...) body)", values );
		}
		
		// Extract function name
		var functionName = values[0] switch
		{
			Value.VariableReferenceValue varRef => varRef.Name,
			Value.StringValue strVal => strVal.Text,
			_ => throw ParamError.Wrong( "defun", "function name as first parameter", values )
		};
		
		// Extract parameter list
		if ( values[1] is not Value.ListValue paramList )
		{
			throw ParamError.Wrong( "defun", "parameter list as second parameter", values );
		}
		
		// Extract body
		if ( values[2] is not Value.ListValue body )
		{
			throw ParamError.Wrong( "defun", "function body as third parameter", values );
		}
		
		var argNames = paramList.ValueList.Select( p => p switch
		{
			Value.StringValue stringValue => stringValue.Text,
			Value.VariableReferenceValue variableReferenceValue => variableReferenceValue.Name,
			_ => throw new InvalidParametersException( [p] )
		} ).ToArray();
		
		var functionValue = new Value.FunctionValue( ( env, arglist ) =>
		{
			if ( arglist.Length != argNames.Length )
			{
				throw new InvalidParametersException( arglist );
			}
			
			var functionEnv = new EnvironmentMap( environment );
			
			for ( var i = 0; i < argNames.Length; i++ )
			{
				functionEnv.SetVariable( argNames[i], arglist[i].Evaluate( env ) );
			}
			
			body.Deconstruct( out var valueList );
			
			return valueList.Execute( functionEnv );
		} );
		
		// Register the function in the environment
		environment.SetVariable( functionName, functionValue );
		
		return functionValue;
	}
	
	private static Value SetFunction( IEnvironment environment, params Value[] values )
	{
		if ( values is not [Value.VariableReferenceValue, _] )
		{
			throw ParamError.Wrong( "set", "(set variable value)", values );
		}
		
		var value = values[1].Evaluate( environment );
		environment.SetVariable( ((Value.VariableReferenceValue)values[0]).Name, value );
		return value;
	}
	
	private static Value.NumberValue SubtractFunction( IEnvironment environment, params Value[] values )
	{
		if ( values.Length == 0 )
		{
			throw new InvalidParametersException( values );
		}
		
		var evaluatedValues = new Value[values.Length];
		
		for ( var i = 0; i < values.Length; i++ )
		{
			evaluatedValues[i] = values[i].Evaluate( environment );
			
			if ( evaluatedValues[i] is not Value.NumberValue )
			{
				throw new InvalidParametersException( evaluatedValues );
			}
		}
		
		if ( values.Length == 1 )
		{
			// Unary minus: negate the single argument
			return new Value.NumberValue( -((Value.NumberValue)evaluatedValues[0]).Number );
		}
		
		// Binary/n-ary minus: subtract all subsequent values from the first
		var result = evaluatedValues
			.Cast<Value.NumberValue>()
			.Select( nv => nv.Number )
			.Aggregate( ( acc, v ) => acc - v );
		
		return new Value.NumberValue( result );
	}
	
	private static Value SumFunction( IEnvironment environment, params Value[] values )
	{
		if ( values.Length == 0 )
		{
			return new Value.NumberValue( 0 ); // Identity for addition
		}
		
		var evaluatedValues = values.Select( v => v.Evaluate( environment ) ).ToArray();
		
		// Check if all are strings
		if ( evaluatedValues.All( v => v is Value.StringValue ) )
		{
			var result = evaluatedValues
				.Cast<Value.StringValue>()
				.Select( sv => sv.Text )
				.Aggregate( ( acc, v ) => acc + v );
			
			return new Value.StringValue( result );
		}
		
		// Check if all are numbers
		if ( evaluatedValues.All( v => v is Value.NumberValue ) )
		{
			var result = evaluatedValues.Cast<Value.NumberValue>().Select( nv => nv.Number ).Sum();
			return new Value.NumberValue( result );
		}
		
		throw new InvalidParametersException( evaluatedValues );
	}
	
	private static (decimal a, decimal b) GetTwoNumbers( IEnvironment environment, Value[] values, string opName )
	{
		if ( values.Length != 2 )
		{
			throw ParamError.Wrong( opName, "two numbers", values );
		}
		
		var v1 = values[0].Evaluate( environment );
		var v2 = values[1].Evaluate( environment );
		
		if ( v1 is not Value.NumberValue n1 || v2 is not Value.NumberValue n2 )
		{
			throw ParamError.Wrong( opName, "two numbers", [v1, v2] );
		}
		
		return ( n1.Number, n2.Number );
	}
}
