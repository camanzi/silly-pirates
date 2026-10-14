using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SillyPirates.Tests.EditMode.Grid.Footprint
{
    /// <summary>
    /// "Which colliders make up this object": a hierarchy of plain GameObjects with BoxColliders. No physics query
    /// is run here (Collect only walks components); TryGetBounds reads Collider.bounds after SyncTransforms.
    /// Nested elements are a real GridElement and a real IClickable (ClickableCube). In EditMode neither gets
    /// Awake/OnEnable, so the GridElement does not go looking for a floor.
    /// </summary>
    public class ElementCollidersTests : ScriptableObjectFixture
    {
        private GameObject NewRoot() => Track(new GameObject("ElementCollidersTests Root"));

        private static GameObject NewChild(GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static List<Collider> Collect(GameObject root)
        {
            List<Collider> results = new List<Collider>();
            ElementColliders.Collect(root.transform, results);
            return results;
        }

        // ---------------------------------------------------------------- Collect

        [Test]
        public void Collect_RootAndChildColliders_ReturnsBoth()
        {
            GameObject root = NewRoot();
            BoxCollider rootBox = root.AddComponent<BoxCollider>();
            BoxCollider childBox = NewChild(root, "child").AddComponent<BoxCollider>();

            List<Collider> results = Collect(root);

            Assert.That(results, Is.EquivalentTo(new Collider[] { rootBox, childBox }));
        }

        [Test]
        public void Collect_GrandchildCollider_IsIncluded()
        {
            GameObject root = NewRoot();
            BoxCollider deep = NewChild(NewChild(root, "child"), "grandchild").AddComponent<BoxCollider>();

            List<Collider> results = Collect(root);

            Assert.That(results, Is.EqualTo(new Collider[] { deep }));
        }

        [Test]
        public void Collect_SeveralCollidersOnOneObject_ReturnsAll()
        {
            GameObject root = NewRoot();
            BoxCollider a = root.AddComponent<BoxCollider>();
            SphereCollider b = root.AddComponent<SphereCollider>();

            List<Collider> results = Collect(root);

            Assert.That(results, Is.EquivalentTo(new Collider[] { a, b }));
        }

        [Test]
        public void Collect_DisabledCollider_IsIgnored()
        {
            GameObject root = NewRoot();
            BoxCollider enabled = root.AddComponent<BoxCollider>();
            BoxCollider disabled = NewChild(root, "disabled").AddComponent<BoxCollider>();
            disabled.enabled = false;

            List<Collider> results = Collect(root);

            Assert.That(results, Is.EqualTo(new Collider[] { enabled }));
        }

        [Test]
        public void Collect_TriggerCollider_IsIgnored()
        {
            GameObject root = NewRoot();
            BoxCollider solid = root.AddComponent<BoxCollider>();
            BoxCollider trigger = NewChild(root, "trigger").AddComponent<BoxCollider>();
            trigger.isTrigger = true;

            List<Collider> results = Collect(root);

            Assert.That(results, Is.EqualTo(new Collider[] { solid }));
        }

        [Test]
        public void Collect_InactiveChild_IsIgnoredWithItsDescendants()
        {
            GameObject root = NewRoot();
            BoxCollider solid = root.AddComponent<BoxCollider>();
            GameObject inactive = NewChild(root, "inactive");
            inactive.AddComponent<BoxCollider>();
            NewChild(inactive, "under inactive").AddComponent<BoxCollider>();
            inactive.SetActive(false);

            List<Collider> results = Collect(root);

            Assert.That(results, Is.EqualTo(new Collider[] { solid }));
        }

        [Test]
        public void Collect_InactiveRoot_ReturnsNothing()
        {
            GameObject root = NewRoot();
            root.AddComponent<BoxCollider>();
            root.SetActive(false);

            List<Collider> results = Collect(root);

            Assert.That(results, Is.Empty);
        }

        [Test]
        public void Collect_NestedGridElement_ItsSubtreeIsNotCollected()
        {
            GameObject root = NewRoot();
            BoxCollider own = NewChild(root, "own").AddComponent<BoxCollider>();
            GameObject nested = NewChild(root, "nested element");
            nested.AddComponent<GridElement>();
            nested.AddComponent<BoxCollider>();
            NewChild(nested, "nested child").AddComponent<BoxCollider>();

            List<Collider> results = Collect(root);

            Assert.That(results, Is.EqualTo(new Collider[] { own }));
        }

        [Test]
        public void Collect_NestedClickable_ItsSubtreeIsNotCollected()
        {
            GameObject root = NewRoot();
            BoxCollider own = NewChild(root, "own").AddComponent<BoxCollider>();
            GameObject nested = NewChild(root, "nested clickable");
            nested.AddComponent<ClickableCube>();
            nested.AddComponent<BoxCollider>();
            NewChild(nested, "nested child").AddComponent<BoxCollider>();

            List<Collider> results = Collect(root);

            Assert.That(results, Is.EqualTo(new Collider[] { own }));
        }

        /// <summary>The root is the element being asked about: its own GridElement must not exclude it.</summary>
        [Test]
        public void Collect_RootIsAGridElement_RootCollidersAreStillCollected()
        {
            GameObject root = NewRoot();
            root.AddComponent<GridElement>();
            BoxCollider rootBox = root.AddComponent<BoxCollider>();

            List<Collider> results = Collect(root);

            Assert.That(results, Is.EqualTo(new Collider[] { rootBox }));
        }

        [Test]
        public void Collect_NonEmptyResults_ClearsThemFirst()
        {
            GameObject root = NewRoot();
            GameObject other = NewRoot();
            BoxCollider stale = other.AddComponent<BoxCollider>();
            List<Collider> results = new List<Collider> { stale };

            ElementColliders.Collect(root.transform, results);

            Assert.That(results, Is.Empty);
        }

        [Test]
        public void Collect_NullRoot_ReturnsNothing()
        {
            List<Collider> results = new List<Collider> { null };

            ElementColliders.Collect(null, results);

            Assert.That(results, Is.Empty);
        }

        // ---------------------------------------------------------------- TryGetBounds

        [Test]
        public void TryGetBounds_NoColliders_ReturnsFalse()
        {
            bool found = ElementColliders.TryGetBounds(new List<Collider>(), out _);

            Assert.That(found, Is.False);
        }

        [Test]
        public void TryGetBounds_NullList_ReturnsFalse()
        {
            bool found = ElementColliders.TryGetBounds(null, out _);

            Assert.That(found, Is.False);
        }

        [Test]
        public void TryGetBounds_TwoSeparateBoxes_ReturnsTheirUnion()
        {
            GameObject root = NewRoot();
            GameObject left = NewChild(root, "left");
            left.transform.position = new Vector3(-3f, 0f, 0f);
            left.AddComponent<BoxCollider>().size = new Vector3(1f, 2f, 1f);
            GameObject right = NewChild(root, "right");
            right.transform.position = new Vector3(5f, 1f, 0f);
            right.AddComponent<BoxCollider>().size = new Vector3(1f, 1f, 4f);
            Physics.SyncTransforms();

            bool found = ElementColliders.TryGetBounds(Collect(root), out Bounds bounds);

            // x: -3.5 .. 5.5, y: -1 .. 1.5, z: -2 .. 2
            Assert.That(found, Is.True);
            Assert.That(Vector3.Distance(bounds.min, new Vector3(-3.5f, -1f, -2f)), Is.LessThan(1e-4f), $"min {bounds.min}");
            Assert.That(Vector3.Distance(bounds.max, new Vector3(5.5f, 1.5f, 2f)), Is.LessThan(1e-4f), $"max {bounds.max}");
        }
    }
}
