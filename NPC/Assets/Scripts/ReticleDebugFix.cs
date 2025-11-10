using UnityEngine;

public class ReticleDebugFix : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var rend = GetComponent<Renderer>();
        if (rend != null) {
            gameObject.layer = LayerMask.NameToLayer("Default");
            rend.material.renderQueue = 4000;
        }
    }

}
