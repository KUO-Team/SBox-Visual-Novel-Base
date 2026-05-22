using Sandbox;
using Sandbox.Diagnostics;
using System;
using System.Linq;
using System.Collections.Generic;
using VNBase;
using VNBase.Assets;
using VNScript.State;

namespace VNScript;

/// <summary>
/// This class contains the dialogue structures as well as the functions to process dialogue and labels from the S-expression code
/// </summary>
public partial class Script
{
	public Dictionary<string, Label> Labels { get; } = new();
	
	public Label InitialLabel { get; internal set; } = new();
	
	internal Dictionary<Value, Value> Variables { get; } = new();
	
	private static readonly Dictionary<string, LabelArgument> BuiltInLabelArguments = new()
	{
		["dialogue"] = LabelDialogueArgument,
		["choice"] = LabelChoiceArgument,
		["char"] = LabelCharacterArgument,
		["sound"] = LabelSoundArgument,
		["music"] = LabelMusicArgument,
		["bg"] = LabelBackgroundArgument,
		["input"] = LabelInputArgument,
		["after"] = LabelAfterArgument
	};
	
	private static readonly Logger Log = new( "VNScript" );
	
	/// <summary>
	/// Parse a new script from the provided code.
	/// </summary>
	public static Script ParseScript( List<SParen> codeBlocks )
	{
		var script = new Script();
		script.Parse( codeBlocks );
		
		return script;
	}
	
	private void Parse( List<SParen> codeBlocks )
	{
		var parsingFunctions = CreateFunctionEnvironment();
		
		foreach ( var sParen in codeBlocks )
		{
			sParen.Execute( parsingFunctions );
		}
	}
	
	private EnvironmentMap CreateFunctionEnvironment()
	{
		var functionEnvironment = new EnvironmentMap();
		
		// Add all builtins so expressions can be evaluated during parse-time set calls
		foreach ( var builtin in BuiltinFunctions.Builtins )
		{
			functionEnvironment.SetVariable( builtin.Key, builtin.Value );
		}
		
		// These override builtins where names clash
		// must use the script versions so variables land in Script.Variables,
		// not the throwaway parse-time environment
		var functions = new Dictionary<string, Value.FunctionValue>
		{
			{ "label", new Value.FunctionValue( CreateLabel ) },
			{ "start", new Value.FunctionValue( SetStartDialogue ) },
			{ "set", new Value.FunctionValue( SetVariable ) },
			{ "defun", new Value.FunctionValue( DefineFunction ) }
		};
		
		foreach ( var function in functions )
		{
			functionEnvironment.SetVariable( function.Key, function.Value );
		}
		
		return functionEnvironment;
	}
	
	private Value.NoneValue SetVariable( IEnvironment environment, Value[] values )
	{
		// Find the key value pair
		for ( var i = 0; i < values.Length - 1; i += 2 )
		{
			var key = values[i];
			var value = values[i + 1].Evaluate( environment );
			Variables[key] = value;
		}
		
		return Value.NoneValue.None;
	}
	
	private Value.FunctionValue DefineFunction( IEnvironment environment, Value[] values )
	{
		var functionValue = BuiltinFunctions.DefineFunction( environment, values );
		
		// Also store in Variables so it survives into the runtime environment
		var functionName = values[0] switch
		{
			Value.VariableReferenceValue varRef => varRef.Name,
			Value.StringValue strVal => strVal.Text,
			_ => throw ParamError.Wrong( "defun", "function name as first parameter", values )
		};
		
		Variables[new Value.VariableReferenceValue( functionName )] = functionValue;
		return functionValue;
	}
	
	private Value.NoneValue SetStartDialogue( IEnvironment environment, Value[] values )
	{
		InitialLabel = Labels[(values[0] as Value.VariableReferenceValue)!.Name];
		return Value.NoneValue.None;
	}
	
	private Value.NoneValue CreateLabel( IEnvironment environment, Value[] values )
	{
		var label = new Label();
		
		var labelName = values[0] switch
		{
			Value.StringValue stringValue => stringValue.Text,
			Value.VariableReferenceValue variableReferenceValue => variableReferenceValue.Name,
			_ => throw new InvalidParametersException( [values[0]] )
		};
		
		Labels[labelName] = label;
		label.Name = labelName;
		
		for ( var i = 1; i < values.Length; i++ )
		{
			var argument = ((Value.ListValue)values[i]).ValueList;
			ProcessLabelArgument( argument, label );
		}
		
		return Value.NoneValue.None;
	}
	
	private static void ProcessLabelArgument( SParen arguments, Label label )
	{
		var argumentType = ((Value.VariableReferenceValue)arguments[0]).Name;
		
		if ( BuiltInLabelArguments.TryGetValue( argumentType, out var builtInArgument ) )
		{
			var reader = new ArgumentReader( arguments, startIndex: 1 );
			builtInArgument( reader, label );
		}
		else
		{
			// This is an executable code block (like (if ...), (when ...), (set ...), etc.)
			// Store it to be executed when the label becomes active
			if ( label.AfterLabel is null )
			{
				label.AfterLabel = new After();
			}
			
			label.AfterLabel.CodeBlocks.Add( arguments );
		}
	}
	
	private delegate void LabelArgument( ArgumentReader reader, Label label );
	private delegate void DialogueArgument( ArgumentReader reader, Label label, Dialogue dialogue );
	private delegate void ChoiceArgument( ArgumentReader reader, Choice choice );
	private delegate void CharacterArgument( ArgumentReader reader, Label label, CharacterState character );
	private delegate void SoundArgument( ArgumentReader reader, Label label, VNBase.Assets.Sound sound );
	private delegate void AfterArgument( ArgumentReader reader, After after );
	
	private static void LabelAfterArgument( ArgumentReader reader, Label label )
	{
		if ( label.AfterLabel is null )
		{
			label.AfterLabel = new After();
		}
		
		while ( reader.HasMore )
		{
			switch ( reader.Read() )
			{
				case Value.ListValue listValue:
					label.AfterLabel.CodeBlocks.Add( listValue.ValueList );
					break;
				case Value.VariableReferenceValue { Name: var name }:
					AfterArgument afterArgument = name switch
					{
						"end" => AfterEndDialogueArgument,
						"jump" => AfterJumpArgument,
						"load" => AfterLoadScriptArgument,
						_ => throw new ArgumentOutOfRangeException( name )
					};
					
					afterArgument( reader, label.AfterLabel );
					break;
				default:
					throw new InvalidParametersException( [reader.Peek()] );
			}
		}
	}
	
	private static void AfterJumpArgument( ArgumentReader reader, After after )
	{
		after.TargetLabel = reader.Read<Value.VariableReferenceValue>().Name;
	}
	
	private static void AfterEndDialogueArgument( ArgumentReader reader, After after )
	{
		after.IsLastLabel = true;
	}
	
	private static void AfterLoadScriptArgument( ArgumentReader reader, After after )
	{
		after.ScriptPath = reader.Read<Value.StringValue>().Text;
	}
	
	private static void LabelChoiceArgument( ArgumentReader reader, Label label )
	{
		var choice = new Choice
		{
			Text = reader.Read<Value.StringValue>().Text
		};
		
		label.Choices.Add( choice );
		
		while ( reader.HasMore )
		{
			var keyword = reader.Read<Value.VariableReferenceValue>().Name;
			
			ChoiceArgument choiceArgument = keyword switch
			{
				"jump" => ChoiceJumpArgument,
				"cond" => ChoiceConditionArgument,
				_ => throw new ArgumentOutOfRangeException( keyword )
			};
			
			choiceArgument( reader, choice );
		}
	}
	
	private static void ChoiceConditionArgument( ArgumentReader reader, Choice choice )
	{
		choice.Condition = reader.Read<Value.ListValue>().ValueList;
	}
	
	private static void ChoiceJumpArgument( ArgumentReader reader, Choice choice )
	{
		choice.TargetLabel = reader.Read<Value.VariableReferenceValue>().Name;
	}
	
	private static void LabelDialogueArgument( ArgumentReader reader, Label label )
	{
		var textParts = new List<Value>();
		
		// Collect text parts until we hit a keyword
		while ( reader.HasMore )
		{
			var arg = reader.Peek();
			if ( arg is Value.VariableReferenceValue varRef && IsDialogueKeyword( varRef.Name ) )
			{
				break;
			}
			
			textParts.Add( reader.Read() );
		}
		
		if ( textParts.Count == 0 )
		{
			throw new InvalidParametersException( [reader.Peek()] );
		}
		
		var textBuilder = new System.Text.StringBuilder();
		var formattedText = new FormattableText( string.Empty );
		
		foreach ( var part in textParts )
		{
			switch ( part )
			{
				case Value.StringValue str:
					textBuilder.Append( str.Text );
					break;
				case Value.VariableReferenceValue varRef:
					// Add as a format placeholder: {variableName}
					textBuilder.Append( $"{{{varRef.Name}}}" );
					break;
				case Value.ListValue listVal:
					// Add expression placeholder and store the expression
					var placeholder = formattedText.AddExpression( listVal.ValueList );
					textBuilder.Append( $"{{{placeholder}}}" );
					break;
				default:
					throw new InvalidParametersException( [part] );
			}
		}
		
		formattedText.Text = textBuilder.ToString();
		
		var entry = new Dialogue
		{
			Text = formattedText,
			Speaker = null
		};
		
		// Process keyword arguments
		while ( reader.HasMore )
		{
			var keyword = reader.Read<Value.VariableReferenceValue>().Name;
			
			DialogueArgument dialogueArgument = keyword switch
			{
				"speaker" => DialogueSpeakerArgument,
				"voiceline" => DialogueVoicelineArgument,
				_ => throw new ArgumentOutOfRangeException( keyword )
			};
			
			dialogueArgument( reader, label, entry );
		}
		
		label.Dialogues.Add( entry );
	}
	
	private static bool IsDialogueKeyword( string name )
	{
		return name is "speaker" or "voiceline";
	}
	
	private static void DialogueSpeakerArgument( ArgumentReader reader, Label label, Dialogue dialogue )
	{
		var characterName = reader.Read<Value.VariableReferenceValue>().Name;
		dialogue.Speaker = GetCharacterResource( characterName ) ?? throw new ResourceNotFoundException( $"Unable to set speaking character, character resource with name {characterName} couldn't be found!", characterName );
	}
	
	private static void DialogueVoicelineArgument( ArgumentReader reader, Label label, Dialogue dialogue )
	{
		var soundName = reader.Read<Value.StringValue>().Text;
		var sound = new VNBase.Assets.Sound( soundName );
		dialogue.Voiceline = sound;
	}
	
	private static void LabelCharacterArgument( ArgumentReader reader, Label label )
	{
		var characterName = reader.Read<Value.VariableReferenceValue>().Name;
		var character = GetCharacterResource( characterName ) ?? throw new ResourceNotFoundException( $"Unable to add character, character resource with name {characterName} couldn't be found!", characterName );
		
		var characterState = new CharacterState
		{
			Character = character
		};
		
		label.Characters.Add( characterState );
		
		while ( reader.HasMore )
		{
			var keyword = reader.Read<Value.VariableReferenceValue>().Name;
			
			CharacterArgument characterArgument = keyword switch
			{
				"exp" => LabelCharacterExpressionArgument,
				"pos" => LabelCharacterPositionArgument,
				"rot" => LabelCharacterRotationArgument,
				_ => throw new ArgumentOutOfRangeException( keyword )
			};
			
			characterArgument( reader, label, characterState );
		}
	}
	
	private static void LabelCharacterExpressionArgument( ArgumentReader reader, Label label, CharacterState character )
	{
		character.ActivePortrait = reader.Read<Value.StringValue>().Text;
	}
	
	private static void LabelCharacterPositionArgument( ArgumentReader reader, Label label, CharacterState character )
	{
		var list = reader.Read<Value.ListValue>().ValueList;
		
		if ( list[0] is not Value.NumberValue xValue || list[1] is not Value.NumberValue yValue )
		{
			Log.Error( "Character position requires two numeric values." );
			return;
		}
		
		character.HasManualPosition = true;
		character.Position = new Vector2( (float)xValue.Number, (float)yValue.Number );
	}
	
	private static void LabelCharacterRotationArgument( ArgumentReader reader, Label label, CharacterState character )
	{
		var list = reader.Read<Value.ListValue>().ValueList;
		
		if ( list[0] is not Value.NumberValue xValue || list[1] is not Value.NumberValue yValue || list[2] is not Value.NumberValue zValue )
		{
			Log.Error( "Character position requires three numeric values." );
			return;
		}
		
		character.Rotation = new Angles( (float)xValue.Number, (float)yValue.Number, (float)zValue.Number );
	}
	
	private static void LabelSoundArgument( ArgumentReader reader, Label label )
	{
		var soundName = reader.Read<Value.StringValue>().Text;
		
		var sound = new VNBase.Assets.Sound( soundName );
		label.Assets.Add( sound );
		label.Sounds.Add( sound );
		
		while ( reader.HasMore )
		{
			var keyword = reader.Read<Value.VariableReferenceValue>().Name;
			
			SoundArgument soundArgument = keyword switch
			{
				"mixer" => SoundMixerArgument,
				_ => throw new ArgumentOutOfRangeException( keyword )
			};
			
			soundArgument( reader, label, sound );
		}
	}
	
	private static void SoundMixerArgument( ArgumentReader reader, Label label, VNBase.Assets.Sound sound )
	{
		sound.MixerName = reader.Read<Value.StringValue>().Text;
	}
	
	private static void LabelMusicArgument( ArgumentReader reader, Label label )
	{
		var musicName = reader.Read<Value.StringValue>().Text;
		var music = new Music( musicName );
		label.Music = music;
		label.Assets.Add( music );
	}
	
	private static void LabelBackgroundArgument( ArgumentReader reader, Label label )
	{
		var backgroundName = reader.Read<Value.StringValue>().Text;
		var backgroundPath = $"{Settings.BackgroundsPath}{backgroundName}";
		var background = new Background( backgroundPath );
		label.Assets.Add( background );
	}
	
	private static void LabelInputArgument( ArgumentReader reader, Label label )
	{
		if ( label.Choices.Count > 0 )
		{
			throw new InvalidOperationException( "Cannot have a text input in a label with choices!" );
		}
		
		var variableName = reader.Read<Value.VariableReferenceValue>();
		label.ActiveInput = new Input
		{
			VariableName = variableName.Name
		};
	}
	
	private static Character? GetCharacterResource( string characterName )
	{
		var characterPath = $"{Settings.CharacterResourcesPath}{characterName}.char";
		return ResourceLibrary.TryGet<Character>( characterPath, out var loadedCharacter ) ? loadedCharacter : null;
	}
}
