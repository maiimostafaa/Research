using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;
using TMPro;
using UnityEngine;
using Meta.WitAi.TTS.Utilities;  // add this

public class NPCChatter : MonoBehaviour
{
    string apiKey = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
   
    [Header("Voice Output")]
    public TTSSpeaker speaker;   // drag your TTSSpeaker GameObject here in Inspector

    [Header("UI")]
    public TMP_Text npcBubbleText;
    public TMP_InputField userInput;

    [Header("Model & Style")]
    public string model = "gpt-4o-mini";   // fast & cheaper. Try Model.GPT4o for higher quality.
    [TextArea(3,8)]
    public string systemPrompt = "You are a friendly in-world NPC. Answer briefly and helpfully.";

    readonly List<Message> history = new();

    void Start()
    {
    Debug.Log(apiKey);
    history.Clear();
    history.Add(new Message(Role.System, systemPrompt));
    if (npcBubbleText) npcBubbleText.text = "Great work exploring the five pillars. Let’s reflect through the Knowledge pillar, which centers awareness, truth-telling, and sharing wisdom. What’s one idea or moment from today that really stuck with you?";
;
    }

    public async void OnSendClicked()
    {
        Debug.Log("OnSendClicked has been clicked!");
        var text = userInput ? userInput.text.Trim() : null;
        if (string.IsNullOrEmpty(text)) return;
        if (userInput) userInput.text = "";

        history.Add(new Message(Role.User, text));

        if (npcBubbleText) npcBubbleText.text = "";
        Debug.Log("Text Removed");
        var sb = new StringBuilder();

        var request = new ChatRequest(history, model: model, temperature: 0.6f);

        // Stream words into the bubble
        var api = new OpenAIClient(new OpenAIAuthentication(apiKey));

        // Use this client for your request
        var response = await api.ChatEndpoint.StreamCompletionAsync(
            request,
            async partial =>
            {
                var delta = partial.FirstChoice?.Delta?.ToString();
                if (!string.IsNullOrEmpty(delta))
                {
                    Debug.Log("received response");
                    sb.Append(delta);
                    if (npcBubbleText) npcBubbleText.text = sb.ToString();
                    Debug.Log("npcBubbleText updated");
                }
                await Task.CompletedTask;
            });

        // Save final message and trim old history to keep cost low
        history.Add(response.FirstChoice.Message);
        // Speak the full response aloud
if (speaker != null)
{
    speaker.Stop(); // optional, stops previous speech if playing
    speaker.Speak(sb.ToString());
}

        if (history.Count > 20) history.RemoveRange(1, history.Count - 20);
    }
}
