using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SillyPirates.Tests.EditMode
{
    /// <summary>
    /// Base fixture for tests that need ScriptableObject instances or GameObjects.
    ///
    /// Half the SOs in this project (TurnOrderDataSO, GridStateDataSO, the event channels) carry state that
    /// would otherwise outlive a test and leak into the next one. Everything created through the helpers
    /// here is destroyed in TearDown, so no test can depend on another's leftovers.
    /// </summary>
    public abstract class ScriptableObjectFixture
    {
        private readonly List<Object> _created = new();

        /// <summary>Creates a tracked ScriptableObject instance, destroyed after the test.</summary>
        protected T NewSO<T>() where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();
            _created.Add(instance);
            return instance;
        }

        /// <summary>
        /// Creates a tracked GameObject that is inactive from the start. Deliberately inactive: components
        /// added to it (GridElement above all) never run Awake/OnEnable, so no raycast against a scene that
        /// does not exist and no Debug.LogError to swallow.
        /// </summary>
        protected GameObject NewInactiveGameObject(string name)
        {
            GameObject go = new GameObject(name);
            go.SetActive(false);
            _created.Add(go);
            return go;
        }

        /// <summary>Registers an externally created object for destruction after the test.</summary>
        protected T Track<T>(T obj) where T : Object
        {
            _created.Add(obj);
            return obj;
        }

        [TearDown]
        public void DestroyTrackedObjects()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                    Object.DestroyImmediate(_created[i]);
            }

            _created.Clear();
        }
    }
}
