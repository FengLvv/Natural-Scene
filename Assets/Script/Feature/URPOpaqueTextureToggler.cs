
using UnityEngine;

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
[ExecuteAlways]
public class URPOpaqueTextureToggler : MonoBehaviour
{
    private void Start()
    {
        UniversalRenderPipelineAsset urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        urp.supportsCameraOpaqueTexture = Application.isPlaying;
        // Debug.Log(urp.name + " Opaque Texture " + (urp.supportsCameraOpaqueTexture ? "Enabled" : "Disabled"));
    }
}