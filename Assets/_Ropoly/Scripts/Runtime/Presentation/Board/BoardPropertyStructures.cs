using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ropoly.Presentation.Board
{
    public static class ProceduralStructureMeshFactory
    {
        private static Mesh _houseMesh;
        private static Mesh _hotelMesh;
        private static Mesh _flagpoleMesh;
        private static Mesh _wavingFlagMesh;
        private static Mesh _hotelFlagpoleMesh;
        private static Mesh _hotelWavingFlagMesh;

        private static Material _wallMaterial;
        private static Material _poleMaterial;
        private static Material _hotelBodyMaterial;

        public static Mesh GetHouseMesh()
        {
            if (_houseMesh == null)
            {
                _houseMesh = CreateHouseMesh();
            }

            return _houseMesh;
        }

        public static Mesh GetHotelMesh()
        {
            if (_hotelMesh == null)
            {
                _hotelMesh = CreateHotelMesh();
            }

            return _hotelMesh;
        }

        public static Mesh GetFlagpoleMesh()
        {
            if (_flagpoleMesh == null)
            {
                _flagpoleMesh = CreateFlagpoleMesh(0.55f, 0.024f);
            }

            return _flagpoleMesh;
        }

        public static Mesh GetWavingFlagMesh()
        {
            if (_wavingFlagMesh == null)
            {
                _wavingFlagMesh = CreateWavingFlagMesh(0.52f, 0.34f, 0.32f);
            }

            return _wavingFlagMesh;
        }

        public static Mesh GetHotelFlagpoleMesh()
        {
            if (_hotelFlagpoleMesh == null)
            {
                _hotelFlagpoleMesh = CreateFlagpoleMesh(0.42f, 0.022f);
            }

            return _hotelFlagpoleMesh;
        }

        public static Mesh GetHotelWavingFlagMesh()
        {
            if (_hotelWavingFlagMesh == null)
            {
                _hotelWavingFlagMesh = CreateWavingFlagMesh(0.42f, 0.26f, 0.20f);
            }

            return _hotelWavingFlagMesh;
        }

        public static Material GetWallMaterial()
        {
            if (_wallMaterial == null)
            {
                _wallMaterial = CreateMaterial(new Color(0.95f, 0.94f, 0.91f, 1f), "HouseWallMaterial");
            }

            return _wallMaterial;
        }

        public static Material GetPoleMaterial()
        {
            if (_poleMaterial == null)
            {
                _poleMaterial = CreateMaterial(new Color(0.92f, 0.78f, 0.32f, 1f), "PoleMaterial");
            }

            return _poleMaterial;
        }

        public static Material GetHotelBodyMaterial()
        {
            if (_hotelBodyMaterial == null)
            {
                _hotelBodyMaterial = CreateMaterial(new Color(0.88f, 0.86f, 0.82f, 1f), "HotelBodyMaterial");
            }

            return _hotelBodyMaterial;
        }

        private static Material CreateMaterial(Color color, string name)
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

            Material material = new Material(shader)
            {
                name = name,
                enableInstancing = true
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            return material;
        }

        private static Mesh CreateHouseMesh()
        {
            Mesh mesh = new Mesh { name = "HouseMesh" };

            // Substantial, high-visibility 3D miniature cottage
            float w = 0.38f;        // Width along X
            float d = 0.38f;        // Depth along Z
            float hw = 0.22f;       // Wall height
            float hr = 0.36f;       // Roof peak height
            float overhang = 0.04f; // Eaves overhang

            // Submesh 0: Walls, Gables, and Chimney
            List<Vector3> verts0 = new List<Vector3>();
            List<int> tris0 = new List<int>();

            // Front wall (z = -d/2)
            AddQuad(verts0, tris0,
                new Vector3(-w / 2, 0, -d / 2),
                new Vector3(w / 2, 0, -d / 2),
                new Vector3(w / 2, hw, -d / 2),
                new Vector3(-w / 2, hw, -d / 2));

            // Back wall (z = d/2)
            AddQuad(verts0, tris0,
                new Vector3(w / 2, 0, d / 2),
                new Vector3(-w / 2, 0, d / 2),
                new Vector3(-w / 2, hw, d / 2),
                new Vector3(w / 2, hw, d / 2));

            // Left wall (x = -w/2)
            AddQuad(verts0, tris0,
                new Vector3(-w / 2, 0, d / 2),
                new Vector3(-w / 2, 0, -d / 2),
                new Vector3(-w / 2, hw, -d / 2),
                new Vector3(-w / 2, hw, d / 2));

            // Right wall (x = w/2)
            AddQuad(verts0, tris0,
                new Vector3(w / 2, 0, -d / 2),
                new Vector3(w / 2, 0, d / 2),
                new Vector3(w / 2, hw, d / 2),
                new Vector3(w / 2, hw, -d / 2));

            // Front gable triangle
            AddTri(verts0, tris0,
                new Vector3(-w / 2, hw, -d / 2),
                new Vector3(w / 2, hw, -d / 2),
                new Vector3(0, hr, -d / 2));

            // Back gable triangle
            AddTri(verts0, tris0,
                new Vector3(w / 2, hw, d / 2),
                new Vector3(-w / 2, hw, d / 2),
                new Vector3(0, hr, d / 2));

            // Chimney on right roof slope
            AddBox(verts0, tris0,
                new Vector3(w * 0.28f, hr * 0.88f, d * 0.12f),
                new Vector3(0.08f, 0.16f, 0.08f));

            // Submesh 1: Pitched Roof (Player Owner Color)
            List<Vector3> verts1 = new List<Vector3>();
            List<int> tris1 = new List<int>();

            float roX = (w / 2) + overhang;
            float roZ = (d / 2) + overhang;
            float roY = hw - 0.02f;
            float peakY = hr + 0.02f;

            // Left roof slope
            AddQuad(verts1, tris1,
                new Vector3(-roX, roY, -roZ),
                new Vector3(0, peakY, -roZ),
                new Vector3(0, peakY, roZ),
                new Vector3(-roX, roY, roZ));

            // Right roof slope
            AddQuad(verts1, tris1,
                new Vector3(0, peakY, -roZ),
                new Vector3(roX, roY, -roZ),
                new Vector3(roX, roY, roZ),
                new Vector3(0, peakY, roZ));

            // Combine submeshes
            List<Vector3> allVerts = new List<Vector3>(verts0);
            int offset = allVerts.Count;
            allVerts.AddRange(verts1);

            for (int i = 0; i < tris1.Count; i++)
            {
                tris1[i] += offset;
            }

            mesh.SetVertices(allVerts);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(tris0, 0);
            mesh.SetTriangles(tris1, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateHotelMesh()
        {
            Mesh mesh = new Mesh { name = "HotelMesh" };

            // Grand multi-tier luxury landmark skyscraper
            float bW = 0.90f; // Base width
            float bD = 0.80f; // Base depth
            float bH = 0.14f; // Base height

            float tW = 0.72f; // Tower width
            float tD = 0.62f; // Tower depth
            float tH = 0.40f; // Tower height

            float pH = 0.54f; // Penthouse floor top
            float rH = 0.68f; // Crown spire peak

            List<Vector3> verts0 = new List<Vector3>(); // Architectural body
            List<int> tris0 = new List<int>();

            // Layer 1: Base podium with grand entrance
            AddBox(verts0, tris0, new Vector3(0, bH * 0.5f, 0), new Vector3(bW, bH, bD));

            // Layer 2: Main hotel tower body
            AddBox(verts0, tris0, new Vector3(0, bH + ((tH - bH) * 0.5f), 0), new Vector3(tW, tH - bH, tD));

            // Layer 3: Penthouse tier
            AddBox(verts0, tris0, new Vector3(0, tH + ((pH - tH) * 0.5f), 0), new Vector3(tW * 0.85f, pH - tH, tD * 0.85f));

            // Submesh 1: Luxury Penthouse Crown & Stepped Mansard Roof (Player color)
            List<Vector3> verts1 = new List<Vector3>();
            List<int> tris1 = new List<int>();

            float pW = tW * 0.86f;
            float pD = tD * 0.86f;

            // 4 steep mansard crown slopes meeting at peak
            AddTri(verts1, tris1, new Vector3(-pW / 2, pH, -pD / 2), new Vector3(pW / 2, pH, -pD / 2), new Vector3(0, rH, 0));
            AddTri(verts1, tris1, new Vector3(pW / 2, pH, -pD / 2), new Vector3(pW / 2, pH, pD / 2), new Vector3(0, rH, 0));
            AddTri(verts1, tris1, new Vector3(pW / 2, pH, pD / 2), new Vector3(-pW / 2, pH, pD / 2), new Vector3(0, rH, 0));
            AddTri(verts1, tris1, new Vector3(-pW / 2, pH, pD / 2), new Vector3(-pW / 2, pH, -pD / 2), new Vector3(0, rH, 0));

            // Combine submeshes
            List<Vector3> allVerts = new List<Vector3>(verts0);
            int offset = allVerts.Count;
            allVerts.AddRange(verts1);

            for (int i = 0; i < tris1.Count; i++)
            {
                tris1[i] += offset;
            }

            mesh.SetVertices(allVerts);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(tris0, 0);
            mesh.SetTriangles(tris1, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateFlagpoleMesh(float height, float radius)
        {
            Mesh mesh = new Mesh { name = "FlagpoleMesh" };
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            // Anchoring pedestal base on ground
            AddBox(verts, tris, new Vector3(0, 0.02f, 0), new Vector3(0.12f, 0.04f, 0.12f));

            // 8-sided metallic pole
            int sides = 8;
            for (int i = 0; i < sides; i++)
            {
                float a1 = (float)i / sides * Mathf.PI * 2f;
                float a2 = (float)(i + 1) / sides * Mathf.PI * 2f;

                Vector3 b1 = new Vector3(Mathf.Cos(a1) * radius, 0.04f, Mathf.Sin(a1) * radius);
                Vector3 b2 = new Vector3(Mathf.Cos(a2) * radius, 0.04f, Mathf.Sin(a2) * radius);
                Vector3 t1 = new Vector3(b1.x, height, b1.z);
                Vector3 t2 = new Vector3(b2.x, height, b2.z);

                AddQuad(verts, tris, b1, b2, t2, t1);
            }

            // Golden finial orb on top
            AddBox(verts, tris, new Vector3(0, height + 0.03f, 0), new Vector3(0.07f, 0.07f, 0.07f));

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateWavingFlagMesh(float length, float height, float yElevation)
        {
            Mesh mesh = new Mesh { name = "WavingFlagMesh" };
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            // The pennant cloth billows out horizontally with an angled tilt and sinusoidal ripples.
            // This ensures 100% full visibility and rich 3D shading from both overhead and angled cameras!
            int segments = 6;
            float tiltAngle = 38f * Mathf.Deg2Rad; // 38 degree tilt towards camera
            float sinTilt = Mathf.Sin(tiltAngle);
            float cosTilt = Mathf.Cos(tiltAngle);

            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments;
                float t1 = (float)(i + 1) / segments;

                float x0 = t0 * length;
                float x1 = t1 * length;

                // Tapering pennant: wide at pole, elegant swallowtail tip
                float h0 = Mathf.Lerp(height, height * 0.40f, t0);
                float h1 = Mathf.Lerp(height, height * 0.40f, t1);

                // Natural sinusoidal cloth wave
                float wave0 = Mathf.Sin(t0 * Mathf.PI * 2.2f) * 0.045f;
                float wave1 = Mathf.Sin(t1 * Mathf.PI * 2.2f) * 0.045f;

                // Top and bottom edges with 3D tilt
                Vector3 p0Top = new Vector3(x0, yElevation + (h0 * 0.5f * sinTilt) + wave0, -(h0 * 0.5f * cosTilt));
                Vector3 p0Bot = new Vector3(x0, yElevation - (h0 * 0.5f * sinTilt) + wave0, +(h0 * 0.5f * cosTilt));
                Vector3 p1Top = new Vector3(x1, yElevation + (h1 * 0.5f * sinTilt) + wave1, -(h1 * 0.5f * cosTilt));
                Vector3 p1Bot = new Vector3(x1, yElevation - (h1 * 0.5f * sinTilt) + wave1, +(h1 * 0.5f * cosTilt));

                // Double-sided cloth rendering
                AddQuad(verts, tris, p0Bot, p1Bot, p1Top, p0Top);
                AddQuad(verts, tris, p0Top, p1Top, p1Bot, p0Bot);
            }

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddTri(List<Vector3> verts, List<int> tris, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            int baseIdx = verts.Count;
            verts.Add(v0);
            verts.Add(v1);
            verts.Add(v2);

            tris.Add(baseIdx);
            tris.Add(baseIdx + 1);
            tris.Add(baseIdx + 2);
        }

        private static void AddQuad(List<Vector3> verts, List<int> tris, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            int baseIdx = verts.Count;
            verts.Add(v0);
            verts.Add(v1);
            verts.Add(v2);
            verts.Add(v3);

            tris.Add(baseIdx);
            tris.Add(baseIdx + 1);
            tris.Add(baseIdx + 2);

            tris.Add(baseIdx);
            tris.Add(baseIdx + 2);
            tris.Add(baseIdx + 3);
        }

        private static void AddBox(List<Vector3> verts, List<int> tris, Vector3 center, Vector3 size)
        {
            Vector3 ext = size * 0.5f;

            Vector3 v0 = center + new Vector3(-ext.x, -ext.y, -ext.z);
            Vector3 v1 = center + new Vector3(ext.x, -ext.y, -ext.z);
            Vector3 v2 = center + new Vector3(ext.x, ext.y, -ext.z);
            Vector3 v3 = center + new Vector3(-ext.x, ext.y, -ext.z);

            Vector3 v4 = center + new Vector3(ext.x, -ext.y, ext.z);
            Vector3 v5 = center + new Vector3(-ext.x, -ext.y, ext.z);
            Vector3 v6 = center + new Vector3(-ext.x, ext.y, ext.z);
            Vector3 v7 = center + new Vector3(ext.x, ext.y, ext.z);

            // Front
            AddQuad(verts, tris, v0, v1, v2, v3);
            // Back
            AddQuad(verts, tris, v4, v5, v6, v7);
            // Top
            AddQuad(verts, tris, v3, v2, v7, v6);
            // Bottom
            AddQuad(verts, tris, v5, v4, v1, v0);
            // Left
            AddQuad(verts, tris, v5, v0, v3, v6);
            // Right
            AddQuad(verts, tris, v1, v4, v7, v2);
        }
    }

    [DisallowMultipleComponent]
    public sealed class PropertyStructureView : MonoBehaviour
    {
        private GameObject _flagObject;
        private MeshRenderer _flagPennantRenderer;

        private readonly List<GameObject> _houses = new List<GameObject>();
        private readonly List<MeshRenderer> _houseRenderers = new List<MeshRenderer>();

        private GameObject _hotelObject;
        private MeshRenderer _hotelRenderer;
        private MeshRenderer _hotelFlagRenderer;

        private Material _ownerMaterial;
        private int _upgradeLevel;
        private bool _isOwned;
        private bool _isHorizontal;

        public int UpgradeLevel => _upgradeLevel;
        public bool IsOwned => _isOwned;

        public void Initialize(bool isHorizontal, Transform parent, Vector3 localPosition)
        {
            _isHorizontal = isHorizontal;
            transform.SetParent(parent, false);
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.identity;

            BuildStructures();
            Clear();
        }

        public void SetOwnerMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            _ownerMaterial = material;
            _isOwned = true;

            ApplyOwnerMaterial();
            RefreshActiveStructures();
        }

        public void SetUpgradeLevel(int level)
        {
            _upgradeLevel = Mathf.Clamp(level, 0, 5);
            if (_isOwned)
            {
                RefreshActiveStructures();
            }
        }

        public void Clear()
        {
            _isOwned = false;
            _upgradeLevel = 0;
            _ownerMaterial = null;

            if (_flagObject != null)
            {
                _flagObject.SetActive(false);
            }

            foreach (GameObject house in _houses)
            {
                if (house != null)
                {
                    house.SetActive(false);
                }
            }

            if (_hotelObject != null)
            {
                _hotelObject.SetActive(false);
            }
        }

        private void BuildStructures()
        {
            // 1. Base Flagpole & Waving Owner Pennant
            _flagObject = new GameObject("OwnerFlag");
            _flagObject.transform.SetParent(transform, false);
            _flagObject.transform.localPosition = Vector3.zero;

            GameObject pole = new GameObject("Pole");
            pole.transform.SetParent(_flagObject.transform, false);
            MeshFilter poleMf = pole.AddComponent<MeshFilter>();
            poleMf.sharedMesh = ProceduralStructureMeshFactory.GetFlagpoleMesh();
            MeshRenderer poleMr = pole.AddComponent<MeshRenderer>();
            poleMr.sharedMaterial = ProceduralStructureMeshFactory.GetPoleMaterial();

            GameObject pennant = new GameObject("Pennant");
            pennant.transform.SetParent(_flagObject.transform, false);
            MeshFilter pennantMf = pennant.AddComponent<MeshFilter>();
            pennantMf.sharedMesh = ProceduralStructureMeshFactory.GetWavingFlagMesh();
            _flagPennantRenderer = pennant.AddComponent<MeshRenderer>();

            // 2. Four Miniature 3D Cottage Houses
            Mesh houseMesh = ProceduralStructureMeshFactory.GetHouseMesh();
            Material wallMat = ProceduralStructureMeshFactory.GetWallMaterial();

            for (int i = 0; i < 4; i++)
            {
                GameObject house = new GameObject($"House_{i + 1}");
                house.transform.SetParent(transform, false);

                MeshFilter mf = house.AddComponent<MeshFilter>();
                mf.sharedMesh = houseMesh;

                MeshRenderer mr = house.AddComponent<MeshRenderer>();
                mr.sharedMaterials = new[] { wallMat, wallMat };

                _houses.Add(house);
                _houseRenderers.Add(mr);
            }

            // 3. Grand Hotel Landmark
            _hotelObject = new GameObject("Hotel");
            _hotelObject.transform.SetParent(transform, false);
            _hotelObject.transform.localPosition = Vector3.zero;

            MeshFilter hotelMf = _hotelObject.AddComponent<MeshFilter>();
            hotelMf.sharedMesh = ProceduralStructureMeshFactory.GetHotelMesh();

            _hotelRenderer = _hotelObject.AddComponent<MeshRenderer>();
            _hotelRenderer.sharedMaterials = new[] { ProceduralStructureMeshFactory.GetHotelBodyMaterial(), wallMat };

            // Rooftop flagpole & flag
            GameObject hotelPole = new GameObject("HotelPole");
            hotelPole.transform.SetParent(_hotelObject.transform, false);
            hotelPole.transform.localPosition = new Vector3(0, 0.68f, 0);

            MeshFilter hpMf = hotelPole.AddComponent<MeshFilter>();
            hpMf.sharedMesh = ProceduralStructureMeshFactory.GetHotelFlagpoleMesh();
            MeshRenderer hpMr = hotelPole.AddComponent<MeshRenderer>();
            hpMr.sharedMaterial = ProceduralStructureMeshFactory.GetPoleMaterial();

            GameObject hotelPennant = new GameObject("HotelPennant");
            hotelPennant.transform.SetParent(hotelPole.transform, false);
            MeshFilter hpenMf = hotelPennant.AddComponent<MeshFilter>();
            hpenMf.sharedMesh = ProceduralStructureMeshFactory.GetHotelWavingFlagMesh();
            _hotelFlagRenderer = hotelPennant.AddComponent<MeshRenderer>();
        }

        private void ApplyOwnerMaterial()
        {
            if (_ownerMaterial == null)
            {
                return;
            }

            if (_flagPennantRenderer != null)
            {
                _flagPennantRenderer.sharedMaterial = _ownerMaterial;
            }

            Material wallMat = ProceduralStructureMeshFactory.GetWallMaterial();
            Material hotelBodyMat = ProceduralStructureMeshFactory.GetHotelBodyMaterial();

            for (int i = 0; i < _houseRenderers.Count; i++)
            {
                if (_houseRenderers[i] != null)
                {
                    _houseRenderers[i].sharedMaterials = new[] { wallMat, _ownerMaterial };
                }
            }

            if (_hotelRenderer != null)
            {
                _hotelRenderer.sharedMaterials = new[] { hotelBodyMat, _ownerMaterial };
            }

            if (_hotelFlagRenderer != null)
            {
                _hotelFlagRenderer.sharedMaterial = _ownerMaterial;
            }
        }

        private void RefreshActiveStructures()
        {
            if (!_isOwned)
            {
                Clear();
                return;
            }

            if (_upgradeLevel <= 0)
            {
                // Level 0: Metallic Flagpole & Waving Owner Pennant (Base ownership claim)
                _flagObject.SetActive(true);
                _flagObject.transform.localPosition = _isHorizontal ? new Vector3(-0.20f, 0, 0) : new Vector3(0, 0, -0.20f);
                _flagObject.transform.localRotation = _isHorizontal ? Quaternion.identity : Quaternion.Euler(0, 90f, 0);

                foreach (GameObject house in _houses)
                {
                    house.SetActive(false);
                }

                _hotelObject.SetActive(false);
                return;
            }

            if (_upgradeLevel >= 5)
            {
                // Level 5: Grand Luxury Hotel Landmark + Rooftop Waving Flag
                _flagObject.SetActive(false);
                foreach (GameObject house in _houses)
                {
                    house.SetActive(false);
                }

                _hotelObject.SetActive(true);
                return;
            }

            // Levels 1 to 4: 1 to 4 3D Miniature Houses + Owner Pennant Flag
            _hotelObject.SetActive(false);
            _flagObject.SetActive(true);
            _flagObject.transform.localRotation = _isHorizontal ? Quaternion.identity : Quaternion.Euler(0, 90f, 0);

            int houseCount = _upgradeLevel;
            for (int i = 0; i < 4; i++)
            {
                bool active = i < houseCount;
                _houses[i].SetActive(active);
                if (active)
                {
                    _houses[i].transform.localPosition = GetHouseOffset(i, houseCount);
                }
            }

            _flagObject.transform.localPosition = GetFlagOffsetForHouses(houseCount);
        }

        private Vector3 GetHouseOffset(int index, int totalCount)
        {
            if (totalCount == 1)
            {
                return Vector3.zero;
            }

            if (totalCount == 2)
            {
                float spacing = 0.44f;
                float pos = (index == 0) ? -spacing * 0.5f : spacing * 0.5f;
                return _isHorizontal ? new Vector3(pos, 0, 0) : new Vector3(0, 0, pos);
            }

            if (totalCount == 3)
            {
                float spacing = 0.40f;
                float pos = (index - 1) * spacing;
                return _isHorizontal ? new Vector3(pos, 0, 0) : new Vector3(0, 0, pos);
            }

            // 4 houses: 2x2 village block
            float ox = (index % 2 == 0) ? -0.22f : 0.22f;
            float oz = (index < 2) ? -0.20f : 0.20f;
            return _isHorizontal ? new Vector3(ox, 0, oz) : new Vector3(oz, 0, ox);
        }

        private Vector3 GetFlagOffsetForHouses(int houseCount)
        {
            if (houseCount == 1)
            {
                return _isHorizontal ? new Vector3(0.38f, 0, 0) : new Vector3(0, 0, -0.38f);
            }

            if (houseCount <= 3)
            {
                float endPos = (houseCount * 0.22f) + 0.18f;
                return _isHorizontal ? new Vector3(endPos, 0, 0) : new Vector3(0, 0, -endPos);
            }

            // 4 houses: flag on the side of the 2x2 village
            return _isHorizontal ? new Vector3(0.52f, 0, 0) : new Vector3(0, 0, -0.44f);
        }
    }
}