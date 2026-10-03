using SnowyLib;
using Unity.Netcode;
using UnityEngine;
using static LethalDiseases.Plugin;

namespace LethalDiseases.Items
{
    internal class Sanitizer : PhysicsProp
    {
        [SerializeField] SkinnedMeshRenderer fluidRenderer = null!;
        Animator animator = null!;
        AudioSource audioSource = null!;

        public static float DispenseAmount => PluginInstance.Config.Bind("Sanitizer Options", "Sanitizer | Dispense Amount", 0.05f, "How much of the Sanitizer should be used up when dispensing").Value;
        public static float SanitizeTime => PluginInstance.Config.Bind("Sanitizer Options", "Sanitizer | Sanitize Time", 300f, "How long in second the player will be protected from contact transmission diseases when used").Value;
        public static float Multiplier => PluginInstance.Config.Bind("Sanitizer Options", "Sanitizer | Multiplier", 0.5f, "When a player would be infected by a contact disease, its infection chance is multiplied by this number if the player is sanitized").Value;

        public float fluidLeft = 1f;

        [StaticInit]
        public static void Init()
        {
            _ = DispenseAmount;
            _ = SanitizeTime;
            _ = Multiplier;
        }

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(-0.1f, 0.14f, -0.01f);
            itemProperties.rotationOffset = new Vector3(90, 100, 0);
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
        }

        public void OnInteract() // InteractTrigger
        {
            SanitizeRpc(localPlayer.actualClientId);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SanitizeRpc(ulong clientId)
        {
            audioSource.Play();
            animator.SetTrigger("pump");
            
            if (fluidLeft <= 0) { return; }

            fluidLeft = Mathf.Clamp01(fluidLeft - DispenseAmount);
            fluidRenderer.SetBlendShapeWeight(0, fluidLeft * 100);
            fluidRenderer.enabled = fluidLeft > 0;

            if (localPlayer.actualClientId == clientId)
            {
                localPlayer.GetHost().SanitizeRpc(SanitizeTime);
            }
        }
    }
}
