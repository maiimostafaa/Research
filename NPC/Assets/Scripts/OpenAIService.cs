using OpenAI;
using UnityEngine;

public class OpenAIService : MonoBehaviour
{
    public static OpenAIClient Client { get; private set; }

    void Awake()
    {
        if (Client != null) { Destroy(gameObject); return; }

        // 1) .openai at project root
        var auth = new OpenAIAuthentication().LoadFromDirectory(Application.dataPath + "/..");

        // 2) Environment variables (OPENAI_API_KEY, etc.)
        auth = new OpenAIAuthentication().LoadFromEnvironment();

        Client = new OpenAIClient(auth);
        DontDestroyOnLoad(gameObject);
    }
}
