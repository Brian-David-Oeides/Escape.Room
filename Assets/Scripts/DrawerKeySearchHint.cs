using UnityEngine;

public class DrawerKeySearchHint : MonoBehaviour
{
    [Tooltip("Bronze Key (Lock 1) - only counts as a failed attempt once frame_lock is solved and the key is active")]
    [SerializeField] private GameObject bronzeKey;

    [Tooltip("PuzzleID of the drawer that actually holds the key")]
    [SerializeField] private string targetPuzzleID = "drawer_BoilerRoom1_***STATIC***_Interiors2_Bookshelf_drawer_l_top_01";

    [Tooltip("Number of wrong-drawer opens before the hint becomes available")]
    [SerializeField] private int hintThreshold = 2;

    // Called by this drawer's own DrawerClamp.onDrawerOpened event
    public void OnWrongDrawerOpened()
    {
        if (bronzeKey != null && bronzeKey.activeInHierarchy)
        {
            ClueManager.Instance?.RegisterFailedAttempt(targetPuzzleID, hintThreshold);
        }
    }
}
