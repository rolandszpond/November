using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Renders each ItemDefinition's worldPrefab into a transparent 256x256 sprite (Assets/Items/Icons/{id}.png) and
// assigns it as the item's icon. Each icon is rendered twice, on black and on white, and alpha is recovered from the
// difference, which works whatever the render pipeline does with the camera's alpha.
public static class IconBaker
{
    private const string IconFolder = StarterContentBuilder.ItemsFolder + "/Icons";
    private const int Size = 256;

    [MenuItem("Tools/Inventory/3. Bake Item Icons")]
    public static void BakeAll()
    {
        var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(StarterContentBuilder.ItemDatabasePath);
        if (db == null)
        {
            Debug.LogError("No ItemDatabase; run Tools/Inventory/1. Create Starter Content first.");
            return;
        }

        StarterContentBuilder.EnsureFolder(IconFolder);
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var cameraGo = new GameObject("IconCamera");
            SceneManager.MoveGameObjectToScene(cameraGo, scene);
            var cam = cameraGo.AddComponent<Camera>();
            cam.scene = scene;
            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.fieldOfView = 25f;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 100f;
            AddLight(scene, Quaternion.Euler(45f, -35f, 0f), 1.3f);
            AddLight(scene, Quaternion.Euler(-20f, 150f, 0f), 0.6f);

            int baked = 0;
            foreach (var item in db.items)
            {
                if (item == null || item.worldPrefab == null) continue;
                var sprite = Bake(item, cam, scene);
                if (sprite != null)
                {
                    item.icon = sprite;
                    EditorUtility.SetDirty(item);
                    baked++;
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Baked {baked} item icons into {IconFolder}.");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static void AddLight(Scene scene, Quaternion rotation, float intensity)
    {
        var go = new GameObject("IconLight");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.rotation = rotation;
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = intensity;
    }

    private static Sprite Bake(ItemDefinition item, Camera cam, Scene scene)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(item.worldPrefab, scene);
        try
        {
            var bounds = StarterContentBuilder.RendererBounds(instance);
            float radius = bounds.extents.magnitude;
            float distance = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var direction = Quaternion.Euler(25f, -140f, 0f) * Vector3.forward;
            cam.transform.position = bounds.center - direction * distance;
            cam.transform.rotation = Quaternion.LookRotation(direction);

            var black = Render(cam, Color.black);
            var white = Render(cam, Color.white);
            var result = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var b = black.GetPixels();
            var w = white.GetPixels();
            var pixels = new Color[b.Length];
            for (int i = 0; i < b.Length; i++)
            {
                float alpha = 1f - ((w[i].r - b[i].r) + (w[i].g - b[i].g) + (w[i].b - b[i].b)) / 3f;
                alpha = Mathf.Clamp01(alpha);
                pixels[i] = alpha > 0.001f
                    ? new Color(b[i].r / alpha, b[i].g / alpha, b[i].b / alpha, alpha)
                    : Color.clear;
            }
            result.SetPixels(pixels);
            result.Apply();
            Object.DestroyImmediate(black);
            Object.DestroyImmediate(white);

            string path = $"{IconFolder}/{item.id}.png";
            File.WriteAllBytes(path, result.EncodeToPNG());
            Object.DestroyImmediate(result);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static Texture2D Render(Camera cam, Color background)
    {
        var rt = RenderTexture.GetTemporary(Size, Size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var previous = RenderTexture.active;
        try
        {
            cam.backgroundColor = background;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply();
            return tex;
        }
        finally
        {
            cam.targetTexture = null;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
        }
    }
}
