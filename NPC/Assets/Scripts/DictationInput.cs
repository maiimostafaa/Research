using UnityEngine;
using Meta.WitAi;
using Meta.WitAi.Dictation;
using Meta.WitAi.Configuration;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Events;
using Oculus.Voice.Dictation;
using TMPro;

public class DictationInput : MonoBehaviour
{
    public AppDictationExperience dictation; // drag [BuildingBlock] Dictation here
    public TMP_InputField userInput;         // drag your text input field here
    
    private string accumulatedText = "";     // stores text across multiple recording sessions

    void Start()
    {
        // Note: To adjust pause tolerance, select [BuildingBlock] Dictation in Hierarchy
        // In Inspector, expand "Voice Service" > "Runtime Configuration"
        // Adjust "Endpoint Speech Threshold" (silence detection, try 1.5-3 seconds)
        // Adjust "Max Recording Time" (maximum recording length, try 120 seconds)
    }

    void OnEnable()
    {
        if (dictation != null)
        {
            // Register event callbacks
            dictation.DictationEvents.OnFullTranscription.AddListener(OnDictationComplete);
            dictation.DictationEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
        }
    }

    void OnDisable()
    {
        if (dictation != null)
        {
            dictation.DictationEvents.OnFullTranscription.RemoveListener(OnDictationComplete);
            dictation.DictationEvents.OnPartialTranscription.RemoveListener(OnPartialTranscription);
        }
    }

    // Called while user is speaking (live updates)
    private void OnPartialTranscription(string transcription)
    {
        Debug.Log("Partial: " + transcription);

        if (userInput != null)
        {
            // Show accumulated text + current partial transcription
            string displayText = accumulatedText;
            if (!string.IsNullOrEmpty(accumulatedText) && !string.IsNullOrEmpty(transcription))
            {
                displayText += " ";
            }
            displayText += transcription;
            
            userInput.text = displayText;
        }
    }

    // Called when user finishes speaking (final result)
    private void OnDictationComplete(string transcription)
    {
        Debug.Log("Complete: " + transcription);

        if (userInput != null && !string.IsNullOrEmpty(transcription))
        {
            // Add this transcription to accumulated text
            if (!string.IsNullOrEmpty(accumulatedText))
            {
                accumulatedText += " ";
            }
            accumulatedText += transcription;
            
            // Update input field with accumulated text
            userInput.text = accumulatedText;
        }
    }

    // Call this from your microphone button's OnClick event
    public void ToggleDictation()
    {
        if (dictation != null)
        {
            if (dictation.Active)
            {
                // Stop recording
                dictation.Deactivate();
            }
            else
            {
                // Start recording (keeps previous text)
                dictation.Activate();
            }
        }
    }

    // Optional: Call this to clear the accumulated text (e.g., after sending message)
    public void ClearAccumulatedText()
    {
        accumulatedText = "";
    }
}