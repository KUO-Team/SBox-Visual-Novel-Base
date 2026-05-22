namespace VNBase.UI;

public partial class DialogueHistoryEntry
{
	[Parameter]
	public ScriptPlayer.HistoryEntry? Entry { get; set; }
	
	private void PlayDialogueVoiceline()
	{
		if ( Entry is null || Entry.Dialogue.Voiceline is null )
		{
			return;
		}
		
		var voiceline = Entry.Dialogue.Voiceline;
		voiceline.Stop();
		voiceline.Play();
	}
}
