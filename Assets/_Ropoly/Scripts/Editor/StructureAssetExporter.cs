using System.IO;
using Ropoly.Presentation.Board;
using UnityEditor;
using UnityEngine;

namespace Ropoly.Editor
{
    public static class StructureAssetExporter
    {
        public const string ArtFolder = "Assets/_Ropoly/Art/Structures";
        public const string PrefabsFolder = "Assets/_Ropoly/Prefabs/Board/Structures";
        public const string ResourcesFolder = "Assets/_Ropoly/Resources/Structures";

        [MenuItem("Ropoly/Development/Export Structure Assets and Prefabs")]
        public static void ExportStructureAssets()
        {
            EnsureFolder("Assets/_Ropoly/Art");
            EnsureFolder(ArtFolder);
            EnsureFolder("Assets/_Ropoly/Prefabs/Board");
            EnsureFolder(PrefabsFolder);
            EnsureFolder("Assets/_Ropoly/Resources");
            EnsureFolder(ResourcesFolder);

            // 1. Export Meshes
            Mesh houseMesh = Object.Instantiate(ProceduralStructureMeshFactory.GetHouseMesh());
            houseMesh.name = "HouseMesh";
            SaveOrUpdateAsset(houseMesh, $"{ArtFolder}/HouseMesh.asset");

            Mesh hotelMesh = Object.Instantiate(ProceduralStructureMeshFactory.GetHotelMesh());
            hotelMesh.name = "HotelMesh";
            SaveOrUpdateAsset(hotelMesh, $"{ArtFolder}/HotelMesh.asset");

            Mesh poleMesh = Object.Instantiate(ProceduralStructureMeshFactory.GetFlagpoleMesh());
            poleMesh.name = "FlagpoleMesh";
            SaveOrUpdateAsset(poleMesh, $"{ArtFolder}/FlagpoleMesh.asset");

            Mesh flagMesh = Object.Instantiate(ProceduralStructureMeshFactory.GetWavingFlagMesh());
            flagMesh.name = "WavingFlagMesh";
            SaveOrUpdateAsset(flagMesh, $"{ArtFolder}/WavingFlagMesh.asset");

            Mesh hotelPoleMesh = Object.Instantiate(ProceduralStructureMeshFactory.GetHotelFlagpoleMesh());
            hotelPoleMesh.name = "HotelFlagpoleMesh";
            SaveOrUpdateAsset(hotelPoleMesh, $"{ArtFolder}/HotelFlagpoleMesh.asset");

            Mesh hotelFlagMesh = Object.Instantiate(ProceduralStructureMeshFactory.GetHotelWavingFlagMesh());
            hotelFlagMesh.name = "HotelWavingFlagMesh";
            SaveOrUpdateAsset(hotelFlagMesh, $"{ArtFolder}/HotelWavingFlagMesh.asset");

            // 2. Export Materials
            Material wallMat = Object.Instantiate(ProceduralStructureMeshFactory.GetWallMaterial());
            wallMat.name = "HouseWallMaterial";
            SaveOrUpdateAsset(wallMat, $"{ArtFolder}/HouseWallMaterial.mat");

            Material poleMat = Object.Instantiate(ProceduralStructureMeshFactory.GetPoleMaterial());
            poleMat.name = "PoleMaterial";
            SaveOrUpdateAsset(poleMat, $"{ArtFolder}/PoleMaterial.mat");

            Material hotelBodyMat = Object.Instantiate(ProceduralStructureMeshFactory.GetHotelBodyMaterial());
            hotelBodyMat.name = "HotelBodyMaterial";
            SaveOrUpdateAsset(hotelBodyMat, $"{ArtFolder}/HotelBodyMaterial.mat");

            // Sample Player Roof & Flag Materials
            Material greenMat = CreateSampleMaterial(new Color(0.20f, 0.85f, 0.40f), "OwnerGreenMaterial");
            SaveOrUpdateAsset(greenMat, $"{ArtFolder}/OwnerGreenMaterial.mat");

            Material blueMat = CreateSampleMaterial(new Color(0.20f, 0.60f, 1.00f), "OwnerBlueMaterial");
            SaveOrUpdateAsset(blueMat, $"{ArtFolder}/OwnerBlueMaterial.mat");

            Material redMat = CreateSampleMaterial(new Color(0.95f, 0.25f, 0.35f), "OwnerRedMaterial");
            SaveOrUpdateAsset(redMat, $"{ArtFolder}/OwnerRedMaterial.mat");

            Material yellowMat = CreateSampleMaterial(new Color(1.00f, 0.85f, 0.20f), "OwnerYellowMaterial");
            SaveOrUpdateAsset(yellowMat, $"{ArtFolder}/OwnerYellowMaterial.mat");

            Material purpleMat = CreateSampleMaterial(new Color(0.70f, 0.35f, 0.95f), "OwnerPurpleMaterial");
            SaveOrUpdateAsset(purpleMat, $"{ArtFolder}/OwnerPurpleMaterial.mat");

            // 3. Export Prefabs
            // A. OwnerFlag Prefab
            GameObject flagObj = new GameObject("OwnerFlag");
            GameObject poleChild = new GameObject("Pole");
            poleChild.transform.SetParent(flagObj.transform, false);
            poleChild.AddComponent<MeshFilter>().sharedMesh = poleMesh;
            poleChild.AddComponent<MeshRenderer>().sharedMaterial = poleMat;

            GameObject pennantChild = new GameObject("Pennant");
            pennantChild.transform.SetParent(flagObj.transform, false);
            pennantChild.AddComponent<MeshFilter>().sharedMesh = flagMesh;
            pennantChild.AddComponent<MeshRenderer>().sharedMaterial = greenMat;

            SavePrefab(flagObj, $"{PrefabsFolder}/OwnerFlag.prefab");
            SavePrefab(flagObj, $"{ResourcesFolder}/OwnerFlag.prefab");
            Object.DestroyImmediate(flagObj);

            // B. House Prefab
            GameObject houseObj = new GameObject("House");
            MeshFilter houseMf = houseObj.AddComponent<MeshFilter>();
            houseMf.sharedMesh = houseMesh;
            MeshRenderer houseMr = houseObj.AddComponent<MeshRenderer>();
            houseMr.sharedMaterials = new[] { wallMat, greenMat };

            SavePrefab(houseObj, $"{PrefabsFolder}/House.prefab");
            SavePrefab(houseObj, $"{ResourcesFolder}/House.prefab");
            Object.DestroyImmediate(houseObj);

            // C. Hotel Prefab
            GameObject hotelObj = new GameObject("Hotel");
            MeshFilter hotelMf = hotelObj.AddComponent<MeshFilter>();
            hotelMf.sharedMesh = hotelMesh;
            MeshRenderer hotelMr = hotelObj.AddComponent<MeshRenderer>();
            hotelMr.sharedMaterials = new[] { hotelBodyMat, purpleMat };

            GameObject hotelPoleChild = new GameObject("HotelPole");
            hotelPoleChild.transform.SetParent(hotelObj.transform, false);
            hotelPoleChild.transform.localPosition = new Vector3(0, 0.68f, 0);
            hotelPoleChild.AddComponent<MeshFilter>().sharedMesh = hotelPoleMesh;
            hotelPoleChild.AddComponent<MeshRenderer>().sharedMaterial = poleMat;

            GameObject hotelPennantChild = new GameObject("HotelPennant");
            hotelPennantChild.transform.SetParent(hotelPoleChild.transform, false);
            hotelPennantChild.AddComponent<MeshFilter>().sharedMesh = hotelFlagMesh;
            hotelPennantChild.AddComponent<MeshRenderer>().sharedMaterial = purpleMat;

            SavePrefab(hotelObj, $"{PrefabsFolder}/Hotel.prefab");
            SavePrefab(hotelObj, $"{ResourcesFolder}/Hotel.prefab");
            Object.DestroyImmediate(hotelObj);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Successfully exported 3D structure meshes, materials, and prefabs to '{PrefabsFolder}' and '{ResourcesFolder}'.");
        }

        private static Material CreateSampleMaterial(Color color, string name)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material mat = new Material(shader) { name = name };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            return mat;
        }

        private static void SaveOrUpdateAsset(Object asset, string path)
        {
            Object existing = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
            }
            else
            {
                AssetDatabase.CreateAsset(asset, path);
            }
        }

        private static void SavePrefab(GameObject instance, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(instance, path);
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string folder = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
