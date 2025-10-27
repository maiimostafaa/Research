using UnityEngine;
using Meta.WitAi.TTS.Utilities;   // TTSSpeaker lives here

public class LLMVoiceOutput : MonoBehaviour
{
    public TTSSpeaker speaker; // assign in Inspector

    // Call this when your language model returns text
    public void SpeakResponse(string responseText)
    {
        if (speaker != null && !string.IsNullOrEmpty(responseText))
        {
            // Stop any ongoing playback if you want to interrupt
            speaker.Stop();
            speaker.Speak(responseText);
        }
        else
        {
            Debug.LogWarning("TTS speaker not set or response text empty.");
        }
    }
}
