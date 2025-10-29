using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Utility to generate a vignette texture for damage overlay
/// Usage: In Unity Editor, go to Tools → Generate Vignette Texture
/// </summary>
/// 

public class VignetteTextureGenerator : MonoBehaviour
{
    [MenuItem("Tools/Generate Vignette Texture")]
    public static void GenerateVignetteTexture()
    {
        // Texture settings
        int textureSize = 512;
        float vignetteStrength = 1.5f; // How far the vignette extends
        float vignetteSmoothness = 0.8f; // How smooth the falloff is

        // Create texture
        Texture2D vignetteTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);

        // Center point
        Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);
        float maxDistance = Vector2.Distance(Vector2.zero, center);

        // Generate pixels
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                // Calculate distance from center
                Vector2 pixelPos = new Vector2(x, y);
                float distance = Vector2.Distance(pixelPos, center);

                // Normalize distance (0 at center, 1 at corners)
                float normalizedDistance = distance / maxDistance;

                // Apply vignette curve (transparent center, opaque edges)
                float vignette = Mathf.Pow(normalizedDistance, vignetteStrength) * vignetteSmoothness;
                vignette = Mathf.Clamp01(vignette);

                // Create pixel (white color, alpha based on distance)
                Color pixelColor = new Color(1f, 1f, 1f, vignette);
                vignetteTexture.SetPixel(x, y, pixelColor);
            }
        }

        // Apply changes
        vignetteTexture.Apply();

        // Save as asset
        string path = "Assets/VignetteTexture.png";
        byte[] bytes = vignetteTexture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);

        AssetDatabase.Refresh();

        // Import settings
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        Debug.Log($"Vignette texture generated at: {path}");

        // Select the created asset
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }
}
