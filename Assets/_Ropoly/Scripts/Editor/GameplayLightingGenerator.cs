using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Ropoly.Editor
{
    /// <summary>
    /// Keeps the gameplay scene's 3D pieces readable while the board remains visually flat.
    /// </summary>
    public static class GameplayLightingGenerator
    {
        private const string GameScenePath = "Assets/_Ropoly/Scenes/Gameplay/Game.unity";
        private const string KeyLightName = "Gameplay Key Light";

        [MenuItem("Ropoly/Development/Repair Gameplay Lighting")]
        public static void Generate()
        {
            if (UnityEngine.Application.isPlaying)
            {
                Debug.LogWarning("Gameplay lighting cannot be repaired while the game is running.");
                return;
            }

            if (!File.Exists(GameScenePath))
            {
                throw new FileNotFoundException($"Gameplay scene not found at '{GameScenePath}'.");
            }

            Scene previousScene = SceneManager.GetActiveScene();
            string previousScenePath = previousScene.path;
            if (!UnityEngine.Application.isBatchMode && previousScene.isDirty)
            {
                Debug.LogWarning("Save the current scene before repairing gameplay lighting.");
                return;
            }

            Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            Configure(gameScene);
            EditorSceneManager.SaveScene(gameScene);

            if (!UnityEngine.Application.isBatchMode &&
                !string.IsNullOrEmpty(previousScenePath) &&
                previousScenePath != GameScenePath)
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }

            Debug.Log("Ropoly gameplay lighting configured.");
        }

        internal static Light Configure(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == KeyLightName)
                {
                    Object.DestroyImmediate(root);
                }
            }

            GameObject lightObject = new GameObject(KeyLightName);
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            lightObject.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

            Light keyLight = lightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f, 0.95f, 0.9f);
            keyLight.intensity = 1.3f;
            keyLight.shadows = LightShadows.None;
            keyLight.bounceIntensity = 0f;
            keyLight.renderMode = LightRenderMode.ForcePixel;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.36f, 0.4f, 0.5f);
            RenderSettings.ambientEquatorColor = new Color(0.21f, 0.23f, 0.3f);
            RenderSettings.ambientGroundColor = new Color(0.1f, 0.11f, 0.16f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.sun = keyLight;

            EditorSceneManager.MarkSceneDirty(scene);
            return keyLight;
        }
    }
}
