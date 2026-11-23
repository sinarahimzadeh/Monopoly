using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ropoly.Editor
{
    [InitializeOnLoad]
    public static class BoardPreviewCleaner
    {
        static BoardPreviewCleaner()
        {
            EditorApplication.hierarchyChanged += CleanDuplicateBoardPreviews;
        }

        [MenuItem("Ropoly/Visuals/Clean Duplicate Scene Boards")]
        public static void CleanDuplicateBoardPreviews()
        {
            int previewCount = 0;
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                Scene scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;

                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject root in rootObjects)
                {
                    if (root != null && root.name == "Generated Board Preview")
                    {
                        previewCount++;
                        if (previewCount > 1)
                        {
                            Object.DestroyImmediate(root);
                        }
                    }
                }
            }
        }
    }
}
