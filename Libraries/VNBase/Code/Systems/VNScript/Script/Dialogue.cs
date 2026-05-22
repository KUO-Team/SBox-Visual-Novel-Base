using VNBase.Assets;

namespace VNScript;

public partial class Script
{
	public class Dialogue
	{
		public FormattableText Text { get; set; } = string.Empty;
		
		public Character? Speaker { get; set; }
		
		public Sound? Voiceline { get; set; }
	}
}
