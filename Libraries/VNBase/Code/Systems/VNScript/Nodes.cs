using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace VNScript;

/// <summary>
/// Exception thrown when a variable is not found in the environment.
/// </summary>
public class UndefinedVariableException : Exception
{
	public UndefinedVariableException( string name ) : base( $"Failed to find variable {name}!" )
	{
		base.Data["Missing Variable"] = name;
	}
}

/// <summary>
/// Exception thrown when the parameters passed to a function are invalid.
/// </summary>
public sealed class InvalidParametersException : Exception
{
	public InvalidParametersException( IEnumerable<Value> values ) : base( $"Invalid parameter types: {FormatValues( values )}" )
	{
		Data["Values"] = values;
	}
	
	public InvalidParametersException( string functionName, string expected, IEnumerable<Value> values ) : base( $"Error in `{functionName}`\n" + $"Expected: {expected}\n" + $"Got: {FormatValues(values)}" )
	{
		Data["Function"] = functionName;
		Data["Expected"] = expected;
		Data["Values"] = values;
	}
	
	private static string FormatValues( IEnumerable<Value> values )
	{
		return string.Join( ", ", values.Select( v => v.ToString() ) );
	}
}

/// <summary>
/// Exception that is thrown if a required resource is unable to be found.
/// </summary>
public class ResourceNotFoundException : FileNotFoundException
{
	private string ResourceName { get; set; }
	
	public ResourceNotFoundException( string message, string? resourceName = null, string? fileName = null, Exception? innerException = null ) : base( message, fileName )
	{
		ResourceName = resourceName ?? string.Empty;
		
		if ( innerException is not null )
		{
			base.Data["InnerException"] = innerException;
		}
	}
	
	public override string Message => $"{base.Message} Resource: {ResourceName}, File: {FileName ?? "N/A"}";
}

internal static class ParamError
{
	public static InvalidParametersException Wrong( string name, string expected, IEnumerable<Value> values )
	{
		return new InvalidParametersException( name, expected, values );
	}
}

/// <summary>
/// A list of values that can be parsed from a string.
/// </summary>
public class SParen : IReadOnlyList<Value>
{
	/// <summary>
	/// A token that can be parsed from a string.
	/// </summary>
	public abstract record Token
	{
		public record OpenParen : Token;
		
		public record Symbol( string Name ) : Token;
		
		public record String( string Text ) : Token;
		
		public record Number( string Value ) : Token;
		
		public record CloseParen : Token;
	}
	
	private readonly List<Value> _backingList;
	
	private SParen( List<Value> backingList )
	{
		_backingList = backingList;
	}
	
	public IEnumerator<Value> GetEnumerator()
	{
		return _backingList.GetEnumerator();
	}
	
	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable)_backingList).GetEnumerator();
	}
	
	public int Count => _backingList.Count;
	
	public Value this[ int index ] => _backingList[index];
	
	/// <summary>
	/// Tokenizes a string into a list of tokens.
	/// </summary>
	/// <param name="text">The string to tokenize.</param>
	private static IEnumerable<Token> TokenizeText( string text )
	{
		var symbolStart = 0;
		var isInQuote = false;
		var isInSingleLineComment = false;
		var isInMultiLineComment = false;
		
		for ( var i = 0; i < text.Length; i++ )
		{
			if ( isInSingleLineComment )
			{
				if ( text[i] == '\n' )
				{
					// End of single-line comment
					isInSingleLineComment = false;
					symbolStart = i + 1;
				}
				
				continue;
			}
			
			if ( isInMultiLineComment )
			{
				if ( text[i] == '*' && i + 1 < text.Length && text[i + 1] == '/' )
				{
					// End of multi-line comment
					isInMultiLineComment = false;
					i++; // Skip '/'
					symbolStart = i + 1;
				}
				
				continue;
			}
			
			if ( isInQuote )
			{
				// Check for escaped quote
				if ( text[i] == '\\' && i + 1 < text.Length && text[i + 1] == '"' )
				{
					i++; // Skip the escaped quote
					continue;
				}
				
				if ( text[i] != '"' )
				{
					continue;
				}
				
				if ( text[i] == '"' )
				{
					// Extract string content without quotes
					var stringContent = text.Substring( symbolStart, i - symbolStart );
					// Unescape any escaped quotes
					stringContent = stringContent.Replace( "\\\"", "\"" );
					yield return new Token.String( stringContent );
					symbolStart = i + 1;
					isInQuote = false;
				}
			}
			else
			{
				if ( char.IsWhiteSpace( text[i] ) )
				{
					if ( i != symbolStart )
					{
						var sym = text[symbolStart..i];
						
						if ( IsValidNumber( sym ) )
						{
							yield return new Token.Number( sym );
						}
						else
						{
							yield return new Token.Symbol( sym );
						}
					}
					
					symbolStart = i + 1;
					
					continue;
				}
				
				switch ( text[i] )
				{
					case '"': 
						isInQuote = true;
						symbolStart = i + 1; // Start after the opening quote
						continue; // Skip rest of loop iteration
					case '/' when i + 1 < text.Length && text[i + 1] == '/':
						isInSingleLineComment = true;
						i++; // Skip '/'
						symbolStart = i + 1;
						continue;
					case '/' when i + 1 < text.Length && text[i + 1] == '*':
						isInMultiLineComment = true;
						i++; // Skip '*'
						symbolStart = i + 1;
						continue;
				}
			}
			
			if ( symbolStart != i && IsValidSymbolName( text[symbolStart] ) && !IsValidSymbolName( text[i] ) )
			{
				var sym = text[symbolStart..i];
				
				if ( IsValidNumber( sym ) )
				{
					yield return new Token.Number( sym );
				}
				else
				{
					yield return new Token.Symbol( sym );
				}
				
				symbolStart = i + 1;
			}
			
			if ( text[i] == '(' )
			{
				yield return new Token.OpenParen();
				symbolStart = i + 1;
			}
			
			if ( text[i] != ')' )
			{
				continue;
			}
			
			yield return new Token.CloseParen();
			symbolStart = i + 1;
		}
		
		if ( symbolStart >= text.Length )
		{
			yield break;
		}
		
		// New scope for sym.
		{
			var sym = text[symbolStart..];
			
			if ( IsValidNumber( sym ) )
			{
				yield return new Token.Number( sym );
			}
			else
			{
				yield return new Token.Symbol( sym );
			}
		}
	}
	
	private static bool IsValidNumber( string str )
	{
		if ( string.IsNullOrEmpty( str ) )
		{
			return false;
		}
		
		// A valid number must contain at least one digit
		if ( !str.Any( char.IsDigit ) )
		{
			return false;
		}
		
		// Try parsing to validate
		return decimal.TryParse( str, out _ );
	}
	
	private static bool IsValidSymbolName( char character )
	{
		return char.IsLetterOrDigit( character ) || character is '=' or '<' or '>' or '-' or '+' or '/' or '*' or '.' or '_';
	}
	
	public static IEnumerable<SParen> ParseText( string text )
	{
		var tokenList = TokenizeText( text ).ToList();
		
		foreach ( var token in ProcessTokens( tokenList ) )
		{
			yield return token;
		}
	}
	
	private static IEnumerable<SParen> ProcessTokens( List<Token> tokenList )
	{
		SParen? currentParen = null;
		var tokenDepth = 0;
		
		for ( var tokenIndex = 0; tokenIndex < tokenList.Count; tokenIndex++ )
		{
			var token = tokenList[tokenIndex];
			
			switch ( token )
			{
				case Token.CloseParen:
					tokenDepth--;
					
					if ( tokenDepth == 0 && currentParen is not null )
					{
						yield return currentParen;
						currentParen = null;
					}
					
					break;
				case Token.OpenParen:
					tokenDepth++;
					
					if ( tokenDepth == 1 )
					{
						currentParen = new SParen( [] );
					}
					else
					{
						var subDepth = 1;
						var subToken = tokenIndex + 1;
						
						for ( ; subToken < tokenList.Count; subToken++ )
						{
							subDepth = tokenList[subToken] switch
							{
								Token.CloseParen => subDepth - 1, Token.OpenParen => subDepth + 1, _ => subDepth
							};
							
							if ( subDepth == 0 )
							{
								break;
							}
						}
						
						foreach ( var sub in ProcessTokens( tokenList.GetRange( tokenIndex, subToken - tokenIndex + 1 ) ) )
						{
							currentParen!._backingList.Add( new Value.ListValue( sub ) );
						}
						
						tokenDepth--;
						tokenIndex = subToken;
					}
					
					break;
				case Token.Number number:
					currentParen!._backingList.Add( new Value.NumberValue( decimal.Parse( number.Value ) ) );
					break;
				case Token.String str:
					currentParen!._backingList.Add( new Value.StringValue( str.Text ) );
					break;
				case Token.Symbol symbol:
					currentParen!._backingList.Add( new Value.VariableReferenceValue( symbol.Name ) );
					break;
			}
		}
	}
	
	public Value Execute( IEnvironment environment )
	{
		return new Value.ListValue( this ).Evaluate( environment );
	}
}
