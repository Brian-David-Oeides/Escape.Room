#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Forces the active scene to be reloaded fresh from disk immediately before every
/// Play session, discarding any in-memory scene drift accumulated from imperfect
/// Play-mode reverts (e.g. XR-tracked Transforms nested inside a PrefabInstance not
/// being restored exactly on Stop). This matches what a full Editor restart already
/// provides, without requiring the restart.
///
/// Editor-only: lives under an "Editor" folder (excluded from all Player builds by
/// Unity's compilation rules) and is additionally guarded by UNITY_EDITOR in case the
/// file is ever moved out of that folder.
/// </summary>
[InitializeOnLoad]
public static class ForceSceneReloadOnPlay
{
    static ForceSceneReloadOnPlay()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Only act right before Play actually starts - never on entering/exiting
        // edit mode otherwise, so normal scene opening/switching is untouched.
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        Scene scene = EditorSceneManager.GetActiveScene();

        // Never-saved scene (no path) - nothing on disk to reload from.
        if (string.IsNullOrEmpty(scene.path))
            return;

        if (scene.isDirty)
        {
            // Shows the standard "Save changes?" prompt (Save / Don't Save / Cancel)
            // for all open, modified scenes. Returns false only if the user cancels -
            // in that case, cancel entering Play mode too rather than silently
            // discarding their unsaved edits.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("[ForceSceneReloadOnPlay] Play canceled - scene has unsaved changes and the user chose Cancel.");
                EditorApplication.isPlaying = false;
                return;
            }
        }

        // Reload from disk. If the user chose "Save" above, this reloads exactly what
        // was just saved. If they chose "Don't Save", this is the discard they agreed to.
        EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
    }
}
#endif
