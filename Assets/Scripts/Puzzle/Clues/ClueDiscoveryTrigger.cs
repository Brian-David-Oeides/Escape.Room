using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to any clue GameObject to register discovery with ClueManager
/// Wire up to GazeInteractable or JournalPositioner events
/// </summary>
/// 
public class ClueDiscoveryTrigger : MonoBehaviour
{
    [Header("Clue Identity")]
    [Tooltip("Unique ID matching ClueManager database")]
    public string clueID = "";

    [Header("Discovery Settings")]
    [Tooltip("Automatically discover when player views the clue")]
    public bool autoDiscoverOnView = true;

    [Tooltip("For journals: require all pages to be viewed before discovery")]
    public bool requiresFullRead = false;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private bool discovered = false;
    private HashSet<int> viewedPages = new HashSet<int>();

    #region Public Methods (Called by Unity Events)

    /// <summary>
    /// Called when clue is viewed (wire to GazeInteractable.onActivated or JournalPositioner)
    /// </summary>
    public void OnClueViewed()
    {
        if (discovered)
        {
            DebugLog("Clue already discovered, ignoring view event");
            return;
        }

        if (autoDiscoverOnView && !requiresFullRead)
        {
            DiscoverClue();
        }
        else
        {
            DebugLog("Clue viewed but not auto-discovering (check settings)");
        }
    }

    /// <summary>
    /// Called when a page is viewed (wire to PageTurnInteractable or DocumentCarousel)
    /// </summary>
    public void OnPageViewed(int pageIndex)
    {
        if (discovered) return;

        viewedPages.Add(pageIndex);
        DebugLog($"Page {pageIndex} viewed. Total pages viewed: {viewedPages.Count}");

        if (requiresFullRead)
        {
            int totalPages = GetTotalPages();

            if (viewedPages.Count >= totalPages)
            {
                DebugLog($"All {totalPages} pages viewed! Discovering clue.");
                DiscoverClue();
            }
        }
    }

    /// <summary>
    /// Manual discovery trigger (for testing or special cases)
    /// </summary>
    public void ForceDiscover()
    {
        DiscoverClue();
    }

    #endregion

    #region Private Methods

    private void DiscoverClue()
    {
        if (discovered)
        {
            DebugLog("Clue already discovered");
            return;
        }

        if (string.IsNullOrEmpty(clueID))
        {
            GameLog.LogError($"[ClueDiscoveryTrigger] Clue ID is empty on {gameObject.name}!");
            return;
        }

        discovered = true;

        if (ClueManager.Instance != null)
        {
            ClueManager.Instance.DiscoverClue(clueID);
            DebugLog($"Clue '{clueID}' registered with ClueManager");
        }
        else
        {
            GameLog.LogError("[ClueDiscoveryTrigger] ClueManager.Instance is null!");
        }
    }

    private int GetTotalPages()
    {
        // Check for DocumentCarousel (used by journals)
        DocumentCarousel carousel = GetComponentInChildren<DocumentCarousel>();
        if (carousel != null)
        {
            return carousel.GetTotalPages();
        }

        return 1; // Single page clue
    }

    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            GameLog.Log($"[ClueDiscoveryTrigger:{gameObject.name}] {message}");
        }
    }

    #endregion

    #region Testing

    [ContextMenu("Test: Trigger Discovery")]
    private void TestTriggerDiscovery()
    {
        OnClueViewed();
    }

    #endregion
}
