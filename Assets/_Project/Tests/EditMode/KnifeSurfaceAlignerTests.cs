using Inktober.Apple.Knife;
using Inktober.Apple.Shape;
using NUnit.Framework;
using UnityEngine;

namespace Inktober.Tests.EditMode
{
    public class KnifeSurfaceAlignerTests
    {
        private const float Tolerance = 1e-4f;

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
        public void SolveRotation_MovesTheSurfacePointOntoTheHomeDirection()
        {
            var rotation = KnifeSurfaceAligner.SolveRotation(
                Vector3.zero, Vector3.up, Vector3.forward, new Vector3(1f, 0f, 0f), Vector3.up);

            var moved = rotation * new Vector3(1f, 0f, 0f);

            Assert.That(Vector3.Distance(moved, Vector3.up), Is.LessThan(Tolerance));
        }

        [Test]
        public void SolveRotation_TurnsTheTravelDirectionTowardsTheHeading()
        {
            var rotation = KnifeSurfaceAligner.SolveRotation(
                Vector3.zero, Vector3.up, Vector3.forward, new Vector3(1f, 0f, 0f), Vector3.up);

            var moved = rotation * Vector3.up;

            Assert.That(Vector3.Distance(moved, Vector3.forward), Is.LessThan(Tolerance));
        }

        [Test]
        public void SolveRotation_KeepsTheDistanceFromTheCenter()
        {
            var center = new Vector3(0.3f, -0.2f, 0.1f);
            var surfacePoint = center + new Vector3(0.4f, 0.3f, 0.2f);

            var rotation = KnifeSurfaceAligner.SolveRotation(center, Vector3.up, Vector3.forward, surfacePoint, Vector3.up);

            var moved = center + rotation * (surfacePoint - center);

            Assert.That(Vector3.Distance(moved, center), Is.EqualTo(Vector3.Distance(surfacePoint, center)).Within(Tolerance));
        }

        [Test]
        public void SolveRotation_PutsThePointOnTheRayFromTheCenterAlongTheHomeDirection()
        {
            var center = new Vector3(0.3f, -0.2f, 0.1f);
            var home = new Vector3(0.2f, 1f, -0.3f).normalized;
            var surfacePoint = center + new Vector3(0.4f, 0.3f, 0.2f);

            var rotation = KnifeSurfaceAligner.SolveRotation(center, home, Vector3.forward, surfacePoint, Vector3.up);

            var moved = rotation * (surfacePoint - center);

            Assert.That(Vector3.Angle(moved, home), Is.LessThan(0.01f));
        }

        [Test]
        public void SolveRotation_WhenThePointIsAtTheCenter_ReturnsIdentity()
        {
            var rotation = KnifeSurfaceAligner.SolveRotation(
                Vector3.zero, Vector3.up, Vector3.forward, Vector3.zero, Vector3.up);

            Assert.That(Quaternion.Angle(rotation, Quaternion.identity), Is.LessThan(Tolerance));
        }

        [Test]
        public void Apply_KeepsTheAppleInPlaceAndTheKnifeOnTheSurface()
        {
            var walker = CreateScene(out var surface, out var knifeTransform);
            var heading = knifeTransform.forward;
            var home = knifeTransform.position.normalized;

            for (int i = 0; i < 600; i++)
            {
                walker.Turn(Mathf.Sin(i * 0.05f) * 3f);
                walker.Advance(0.013f);
                KnifeSurfaceAligner.Apply(walker, knifeTransform, surface, home, heading);

                Assert.That(walker.IsOnSurface, Is.True, $"Left the surface in step {i}.");
                Assert.That(surface.position, Is.EqualTo(Vector3.zero), $"The apple moved in step {i}.");
            }
        }

        [Test]
        public void Apply_KeepsTheKnifeAboveTheHomePointAndFacingTheHeading()
        {
            var walker = CreateScene(out var surface, out var knifeTransform);
            var heading = knifeTransform.forward;
            var home = knifeTransform.position.normalized;

            // 12000 steps of 1.3 cm are dozens of turns around the apple, with some steering on top.
            for (int i = 0; i < 12000; i++)
            {
                walker.Turn(Mathf.Sin(i * 0.013f) * 4f);
                walker.Advance(0.013f);
                KnifeSurfaceAligner.Apply(walker, knifeTransform, surface, home, heading);

                Assert.That(walker.IsOnSurface, Is.True, $"Left the surface in step {i}.");

                float directionError = Vector3.Angle(knifeTransform.position, home);
                Assert.That(directionError, Is.LessThan(0.01f), $"The knife drifted around the apple in step {i}.");

                float headingError = Vector3.Angle(Vector3.ProjectOnPlane(knifeTransform.forward, home), Vector3.ProjectOnPlane(heading, home));
                Assert.That(headingError, Is.LessThan(25f), $"The knife turned away in step {i}.");
            }
        }

        [Test]
        public void Apply_MovesTheContactPointAlongTheSurfaceByTheStep()
        {
            var walker = CreateScene(out var surface, out var knifeTransform);
            var heading = knifeTransform.forward;
            var home = knifeTransform.position.normalized;
            var previous = surface.InverseTransformPoint(walker.Position);

            for (int i = 0; i < 300; i++)
            {
                walker.Advance(0.013f);
                KnifeSurfaceAligner.Apply(walker, knifeTransform, surface, home, heading);

                var current = surface.InverseTransformPoint(walker.Position);
                Assert.That(Vector3.Distance(previous, current), Is.EqualTo(0.013f).Within(0.004f), $"Step {i} was not a step.");
                previous = current;
            }
        }

        private ISurfaceWalker CreateScene(out Transform surface, out Transform knifeTransform)
        {
            mesh = AppleMeshBuilder.Build(new AppleShapeSettings(), 5, 64, 48);

            apple = new GameObject("Apple");
            apple.AddComponent<MeshFilter>().sharedMesh = mesh;
            apple.AddComponent<MeshCollider>().sharedMesh = mesh;
            surface = apple.transform;

            knife = new GameObject("Knife");
            var walker = knife.AddComponent<SpherecastSurfaceWalker>();
            knifeTransform = knife.transform;
            knifeTransform.SetPositionAndRotation(new Vector3(0f, 0.7f, 0f), Quaternion.LookRotation(Vector3.forward, Vector3.up));
            Physics.SyncTransforms();

            Assert.That(walker.Place(knifeTransform.position, -knifeTransform.up, knifeTransform.forward), Is.True);
            knifeTransform.SetPositionAndRotation(walker.Position, Quaternion.LookRotation(walker.Forward, walker.Normal));
            return walker;
        }
    }
}
