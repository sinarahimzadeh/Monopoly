using NUnit.Framework;
using Ropoly.Presentation.Board;
using UnityEngine;

namespace Ropoly.Tests.EditMode
{
    public sealed class PropertyStructureVisualTests
    {
        private GameObject _root;
        private PropertyStructureView _view;
        private Material _playerMaterial;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("TestRoot");
            _view = _root.AddComponent<PropertyStructureView>();
            _view.Initialize(isHorizontal: true, _root.transform, Vector3.zero);

            _playerMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"))
            {
                color = Color.green
            };
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerMaterial != null)
            {
                Object.DestroyImmediate(_playerMaterial);
            }

            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        [Test]
        public void InitialState_IsUnownedAndInactive()
        {
            Assert.That(_view.IsOwned, Is.False);
            Assert.That(_view.UpgradeLevel, Is.EqualTo(0));
        }

        [Test]
        public void SetOwnerMaterial_ActivatesBaseOwnershipFlag()
        {
            _view.SetOwnerMaterial(_playerMaterial);

            Assert.That(_view.IsOwned, Is.True);
            Assert.That(_view.UpgradeLevel, Is.EqualTo(0));

            Transform flag = _root.transform.Find("OwnerFlag");
            Assert.That(flag, Is.Not.Null);
            Assert.That(flag.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void SetUpgradeLevel_LevelsOneThroughFour_ActivatesHousesAndFlag()
        {
            _view.SetOwnerMaterial(_playerMaterial);

            for (int level = 1; level <= 4; level++)
            {
                _view.SetUpgradeLevel(level);
                Assert.That(_view.UpgradeLevel, Is.EqualTo(level));

                for (int i = 1; i <= 4; i++)
                {
                    Transform house = _root.transform.Find($"House_{i}");
                    Assert.That(house, Is.Not.Null);
                    Assert.That(house.gameObject.activeSelf, Is.EqualTo(i <= level), $"House_{i} active state mismatch at level {level}");
                }

                Transform hotel = _root.transform.Find("Hotel");
                Assert.That(hotel.gameObject.activeSelf, Is.False);

                Transform flag = _root.transform.Find("OwnerFlag");
                Assert.That(flag.gameObject.activeSelf, Is.True);
            }
        }

        [Test]
        public void SetUpgradeLevel_LevelFive_ActivatesHotelAndDeactivatesHouses()
        {
            _view.SetOwnerMaterial(_playerMaterial);
            _view.SetUpgradeLevel(5);

            Assert.That(_view.UpgradeLevel, Is.EqualTo(5));

            for (int i = 1; i <= 4; i++)
            {
                Transform house = _root.transform.Find($"House_{i}");
                Assert.That(house.gameObject.activeSelf, Is.False);
            }

            Transform hotel = _root.transform.Find("Hotel");
            Assert.That(hotel, Is.Not.Null);
            Assert.That(hotel.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void Clear_HidesAllStructuresAndResetsState()
        {
            _view.SetOwnerMaterial(_playerMaterial);
            _view.SetUpgradeLevel(3);
            _view.Clear();

            Assert.That(_view.IsOwned, Is.False);
            Assert.That(_view.UpgradeLevel, Is.EqualTo(0));

            Transform flag = _root.transform.Find("OwnerFlag");
            Assert.That(flag.gameObject.activeSelf, Is.False);

            for (int i = 1; i <= 4; i++)
            {
                Transform house = _root.transform.Find($"House_{i}");
                Assert.That(house.gameObject.activeSelf, Is.False);
            }

            Transform hotel = _root.transform.Find("Hotel");
            Assert.That(hotel.gameObject.activeSelf, Is.False);
        }
    }
}
