using System;
using System.Reflection;
using NUnit.Framework;

namespace SillyPirates.Tests.EditMode
{
    /// <summary>
    /// Runs a MonoBehaviour's Unity message (Awake, OnEnable, OnDisable, Start) by reflection.
    ///
    /// EditMode sends no lifecycle messages to a MonoBehaviour without [ExecuteAlways] (see GridElementTests),
    /// and a test subclass cannot be attached because the test assembly is editor-only. The method is looked up
    /// on <paramref name="declaringType"/> because private messages are not visible from a subclass type; the
    /// call itself dispatches virtually, so a protected override still runs its most derived version. A
    /// missing method fails the test loudly instead of letting a rename pass in silence.
    /// </summary>
    internal static class LifecycleInvoker
    {
        internal static void Invoke(object target, Type declaringType, string message)
        {
            MethodInfo method = declaringType.GetMethod(message,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);
            Assert.That(method, Is.Not.Null, $"{declaringType.Name}.{message} not found: was it renamed?");
            method.Invoke(target, null);
        }
    }
}
