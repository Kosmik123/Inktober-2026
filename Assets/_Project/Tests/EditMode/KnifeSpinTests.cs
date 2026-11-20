using Inktober.Apple.Knife;
using Inktober.Apple.Shape;
using NUnit.Framework;
using UnityEngine;

namespace Inktober.Tests.EditMode
{
    public class KnifeSpinTests
    {
        private const float PitchDegrees = 0.8f / 0.5f * Mathf.Rad2Deg / 60f;

        private GameObject apple;
        private GameObject knife;
        private Mesh mesh;

        [TearDown]
        public void TearDown()
        {
            if (apple)
                Object.DestroyImmediate(apple);

            if (knife)
                Object.DestroyImmediate(knife);

            if (mesh)
                Object.DestroyImmediate(mesh);
        }

        [Test]
        public void Rotation_WithoutYaw_TurnsByExactlyThePitch()
        {
            var rotation = KnifeSpin.Rotation(Vector3.up, Vector3.forward, 2f, 0f);

            Assert.That(Quaternion.Angle(rotation, Quaternion.identity), Is.EqualTo(2f).Within(1e-3f));
        }

        [Test]
        public void Rotation_MovesTheSkinUnderTheKnifeBackwards()
        {
            var rotation = KnifeSpin.Rotation(Vector3.up, Vector3.forward, 5f, 0f);

            var moved = rotation * Vector3.up - Vector3.up;

            Assert.That(Vector3.Dot(moved.normalized, Vector3.back), Is.GreaterThan(0.99f));
        }

        [Test]
        public void Rotation_WithPositiveYaw_TurnsThePathRight()
        {
            var rotation = KnifeSpin.Rotation(Vector3.up, Vector3.forward, 0f, 10f);

            // The path direction as the surface sees it, once the surface has turned.
            var pathOnSurface = Quaternion.Inverse(rotation) * Vector3.forward;

            Assert.That(Vector3.Angle(pathOnSurface, Quaternion.AngleAxis(10f, Vector3.up) * Vector3.forward), Is.LessThan(0.01f));
        }

        [Test]
        public void Rotation_WhenTheHeadingPointsAtTheCenter_StillTurnsByTheYaw()
        {
            var rotation = KnifeSpin.Rotation(Vector3.up, Vector3.up, 2f, 3f);

            Assert.That(Quaternion.Angle(rotation, Quaternion.identity), Is.EqualTo(3f).Within(1e-3f));
        }

        [Test]
        public void Apply_KeepsTheContactAboveTheHomeDirectionAndTheAppleInPlace()
        {
            var walker = CreateScene(out var surface, out var start);
            var heading = start.forward;
            var home = start.position.normalized;

            for (int i = 0; i < 3000; i++)
            {
                bool onSurface = KnifeSpin.Apply(walker, surface, home, heading, PitchDegrees, Mathf.Sin(i * 0.013f) * 4f, 2f);

                Assert.That(onSurface, Is.True, $"Lost the surface in step {i}.");
                Assert.That(Vector3.Angle(walker.Position, home), Is.LessThan(0.01f), $"The contact left the home direction in step {i}.");
                Assert.That(surface.position, Is.EqualTo(Vector3.zero), $"The apple moved in step {i}.");
            }
        }

        [Test]
        public void Apply_MovesTheContactAlongTheSkinByAboutTheNominalStep()
        {
            var walker = CreateScene(out var surface, out var start);
            var heading = start.forward;
            var home = start.position.normalized;
            var previous = surface.InverseTransformPoint(walker.Position);

            for (int i = 0; i < 600; i++)
            {
                KnifeSpin.Apply(walker, surface, home, heading, PitchDegrees, 0f, 2f);

                var current = surface.InverseTransformPoint(walker.Position);
                float step = Vector3.Distance(previous, current);
                Assert.That(step, Is.InRange(0.005f, 0.03f), $"Step {i} was {step}.");
                previous = current;
            }
        }

        [Test]
        public void Apply_ReadsTheNormalAndTheDirectionOfTravelAtTheContact()
        {
            var walker = CreateScene(out var surface, out var start);
            var heading = start.forward;
            var home = start.position.normalized;

            KnifeSpin.Apply(walker, surface, home, heading, PitchDegrees, 0f, 2f);

            Assert.That(Vector3.Dot(walker.Normal, home), Is.GreaterThan(0.8f));
            // The direction of travel lies in the plane of the real surface, the normal is smoothed, so they are only nearly perpendicular.
            Assert.That(Vector3.Dot(walker.Forward, walker.Normal), Is.EqualTo(0f).Within(0.1f));
            Assert.That(Vector3.Dot(walker.Forward, heading), Is.GreaterThan(0.8f));
        }

        private ISurfaceWalker CreateScene(out Transform surface, out Transform start)
        {
            mesh = AppleMeshBuilder.Build(new AppleShapeSettings(), 5, 64, 48);

            apple = new GameObject("Apple");
            apple.AddComponent<MeshFilter>().sharedMesh = mesh;
            apple.AddComponent<MeshCollider>().sharedMesh = mesh;
            surface = apple.transform;

            knife = new GameObject("Knife");
            var walker = knife.AddComponent<SpherecastSurfaceWalker>();
            start = knife.transform;
            start.SetPositionAndRotation(new Vector3(0f, 0.7f, 0f), Quaternion.LookRotation(Vector3.forward, Vector3.up));
            Physics.SyncTransforms();

            Assert.That(walker.Place(start.position, -start.up, start.forward), Is.True);
            start.SetPositionAndRotation(walker.Position, Quaternion.LookRotation(walker.Forward, walker.Normal));
            return walker;
        }
    }
}
