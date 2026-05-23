using System.Collections.Generic;
using VNBase.Assets;
using VNScript.State;

namespace VNScript;

public partial class Script
{
	public class Label
	{
		public string Name { get; set; } = string.Empty;
		
		public List<Dialogue> Dialogues { get; set; } = [];
		
		public List<CharacterState> Characters { get; set; } = [];
		
		public List<Choice> Choices { get; set; } = [];
		
		public List<Sound> Sounds { get; set; } = [];
		
		public Music? Music { get; set; }
		
		public BackgroundImage? BackgroundImage { get; set; }
		
		public Input? ActiveInput { get; set; }
		
		public After? AfterLabel { get; set; }
		
		internal IEnvironment Environment { get; set; } = new EnvironmentMap();
	}
}
