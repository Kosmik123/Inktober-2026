using Inktober.Apple.Knife;
using Inktober.Apple.Shape;
using NUnit.Framework;
using UnityEngine;

namespace Inktober.Tests.EditMode
{
    public class KnifeBladeFitTests
    {
        private const float BladeLength = 0.12f;
        private const float BladeWidth = 0.06f;
        private const float DeltaTime = 1f / 60f;
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
        public void TryFit_OnASphere_LaysTheKnifeFlatOnTheSurface()
        {
            var collider = CreateApple(new AppleShapeSettings(0.5f, 0f, 40f, 35f, 0f, 22f, 0f, 18f, 0f, 1.3f), 1);

            bool fitted = KnifeBladeFit.TryFit(collider, new Vector3(0f, 0.5f, 0f), Vector3.up, Vector3.forward, BladeLength, BladeWidth, out var pose);

            Assert.That(fitted, Is.True);
            Assert.That(Vector3.Angle(pose.rotation * Vector3.up, Vector3.up), Is.LessThan(0.5f));
            Assert.That(pose.position.y, Is.EqualTo(0.5f).Within(0.005f));
        }

        [Test]
        public void TryFit_WhenTheBladeIsAwayFromTheSurface_ReturnsFalse()
        {
            var collider = CreateApple(new AppleShapeSettings(), 1);

            bool fitted = KnifeBladeFit.TryFit(collider, new Vector3(5f, 5f, 5f), Vector3.up, Vector3.forward, BladeLength, BladeWidth, out _);

            Assert.That(fitted, Is.False);
        }

        [Test]
        public void TryFit_KeepsTheKnifeFacingTheTravelDirection()
        {
            var collider = CreateApple(new AppleShapeSettings(), 1);
            var position = new Vector3(0f, 0.55f, 0f);

            Assert.That(Physics.Raycast(position + Vector3.up, Vector3.down, out var hit), Is.True);
            bool fitted = KnifeBladeFit.TryFit(collider, hit.point, hit.normal, Vector3.forward, BladeLength, BladeWidth, out var pose);

            Assert.That(fitted, Is.True);
            Assert.That(Vector3.Angle(Vector3.ProjectOnPlane(pose.rotation * Vector3.forward, Vector3.up), Vector3.forward), Is.LessThan(5f));
        }

        [Test]
        public void Smoother_WithNoSmoothTime_ReturnsTheTarget()
        {
            var current = new Pose(Vector3.zero, Quaternion.identity);
            var target = new Pose(Vector3.one, Quaternion.Euler(0f, 90f, 0f));

            var result = KnifePoseSmoother.Step(current, target, DeltaTime, 0f);

            Assert.That(result.position, Is.EqualTo(target.position));
            Assert.That(Quaternion.Angle(result.rotation, target.rotation), Is.LessThan(1e-3f));
        }

        [Test]
        public void Smoother_MovesPartOfTheWayToTheTarget()
        {
            var current = new Pose(Vector3.zero, Quaternion.identity);
            var target = new Pose(new Vector3(1f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));

            var result = KnifePoseSmoother.Step(current, target, DeltaTime, 0.08f);

            Assert.That(result.position.x, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(Quaternion.Angle(result.rotation, target.rotation), Is.LessThan(90f).And.GreaterThan(0f));
        }

        [Test]
        public void Smoother_GivesTheSameResultForOneLongStepAndTwoShortOnes()
        {
            var current = new Pose(Vector3.zero, Quaternion.identity);
            var target = new Pose(new Vector3(1f, 0f, 0f), Quaternion.identity);

            var once = KnifePoseSmoother.Step(current, target, DeltaTime, 0.08f);
            var half = KnifePoseSmoother.Step(current, target, DeltaTime * 0.5f, 0.08f);
            var twice = KnifePoseSmoother.Step(half, target, DeltaTime * 0.5f, 0.08f);

            Assert.That(twice.position.x, Is.EqualTo(once.position.x).Within(1e-4f));
        }

        [Test]
        public void FittedAndSmoothedKnife_ShakesLessThanTheRawWalkerPose()
        {
            var collider = CreateApple(new AppleShapeSettings(), 5);
            var walker = CreateWalker(out var surface, out var start);
            var heading = start.forward;
            var home = start.position.normalized;

            var raw = new Pose(walker.Position, Quaternion.LookRotation(walker.Forward, walker.Normal));
            var smoothed = raw;
            float rawTravel = 0f;
            float smoothedTravel = 0f;

            for (int i = 0; i < 1200; i++)
            {
                KnifeSpin.Apply(walker, surface, home, heading, PitchDegrees, Mathf.Sin(i * 0.02f) * 2f, 2f);

                var rawTarget = new Pose(walker.Position, Quaternion.LookRotation(walker.Forward, walker.Normal));
                var target = rawTarget;
                if (KnifeBladeFit.TryFit(collider, walker.Position, walker.Normal, walker.Forward, BladeLength, BladeWidth, out var fitted))
                    target = fitted;

                var nextSmoothed = KnifePoseSmoother.Step(smoothed, target, DeltaTime, 0.08f);

                rawTravel += Quaternion.Angle(raw.rotation, rawTarget.rotation);
                smoothedTravel += Quaternion.Angle(smoothed.rotation, nextSmoothed.rotation);
                raw = rawTarget;
                smoothed = nextSmoothed;
            }

            Assert.That(smoothedTravel, Is.LessThan(rawTravel * 0.7f), $"Raw {rawTravel:F0}°, smoothed {smoothedTravel:F0}°.");
        }

        private MeshCollider CreateApple(AppleShapeSettings settings, int seed)
        {
            mesh = AppleMeshBuilder.Build(settings, seed, 64, 48);

            apple = new GameObject("Apple");
            apple.AddComponent<MeshFilter>().sharedMesh = mesh;
            var collider = apple.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            Physics.SyncTransforms();
            return collider;
        }

        private ISurfaceWalker CreateWalker(out Transform surface, out Transform start)
        {
            surface = apple.transform;

            knife = new GameObject("Knife");
            var walker = knife.AddComponent<SpherecastSurfaceWalker>();
            start = knife.transform;
            start.SetPositionAndRotation(new Vector3(0f, 0.7f, 0f), Quaternion.LookRotation(Vector3.forward, Vector3.up));

            Assert.That(walker.Place(start.position, -start.up, start.forward), Is.True);
            start.SetPositionAndRotation(walker.Position, Quaternion.LookRotation(walker.Forward, walker.Normal));
            return walker;
        }
    }
}
