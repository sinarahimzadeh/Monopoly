using NUnit.Framework;
using Ropoly.Presentation.Dice;
using UnityEditor;
using UnityEngine;

namespace Ropoly.Tests.EditMode
{
    public sealed class DiceAssetTests
    {
        [Test]
        public void RoundedDieMesh_IsDetailedThreeDimensionalGeometry()
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(
                "Assets/_Ropoly/Art/Models/Dice/RoundedDie.asset");

            Assert.That(mesh, Is.Not.Null);
            Assert.That(mesh.vertexCount, Is.GreaterThan(250));
            Assert.That(mesh.triangles.Length, Is.GreaterThan(1000));
            Assert.That(mesh.bounds.size.x, Is.EqualTo(1f).Within(0.01f));
            Assert.That(mesh.bounds.size.y, Is.EqualTo(1f).Within(0.01f));
            Assert.That(mesh.bounds.size.z, Is.EqualTo(1f).Within(0.01f));
        }

        [Test]
        public void DiePrefab_HasAllPipsAndOnlyThreeDimensionalPhysics()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Ropoly/Prefabs/Gameplay/Dice/Die.prefab");

            Assert.That(prefab, Is.Not.Null);
            DieView view = prefab.GetComponent<DieView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.PipCount, Is.EqualTo(21));
            Assert.That(view.Rigidbody, Is.Not.Null);
            Assert.That(view.Rigidbody.isKinematic, Is.True);
            Assert.That(view.BodyCollider, Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<Rigidbody2D>(true), Is.Empty);
        }

        [Test]
        public void EveryDieValue_HasAValidTopFacingOrientation()
        {
            for (int value = 1; value <= 6; value++)
            {
                Quaternion rotation = DieView.GetRotationForValue(value, yawDegrees: 90f);
                Vector3 topDirection = rotation * DieView.GetLocalFaceDirection(value);
                Assert.That(Vector3.Dot(topDirection.normalized, Vector3.up), Is.GreaterThan(0.999f));
            }
        }
    }
}
