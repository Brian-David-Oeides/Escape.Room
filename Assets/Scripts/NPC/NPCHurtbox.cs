using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCHurtbox : MonoBehaviour
{
    private NPCCombatController combatController;

    void Start()
    {
        combatController = GetComponentInParent<NPCCombatController>();

        if (combatController == null)
            Debug.LogError("[NPCHurtbox] Could not find NPCCombatController on parent NPC!");
    }

    void OnTriggerEnter(Collider other)
    {
        // Only respond to XR Origin layer (Layer 6)
        if (other.gameObject.layer != 6) return;

        Debug.Log($"[NPCHurtbox] Hand collision detected with: {other.gameObject.name}");

        // Trigger success haptic on the hitting hand
        HandHaptics haptics = other.GetComponentInParent<HandHaptics>();
        haptics?.TriggerSuccessHaptic();

        combatController?.OnHurtboxHit();
    }
}
