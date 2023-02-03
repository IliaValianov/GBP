using UnityEngine;

[CreateAssetMenu]
public class NoteInfo : ScriptableObject
{
	public string noteName;
	public string screenName;
	public string text;
	public Sprite icon;
	public bool isReceipt;
	public Receipt receipt;
}