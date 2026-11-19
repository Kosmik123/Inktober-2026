using Inktober.InputProcessors;
using NUnit.Framework;
using UnityEngine;

namespace Inktober.Tests.EditMode
{
    public class DeltaTimeProcessorTests
    {
        [Test]
        public void Process_MultipliesTheValueByDeltaTime()
        {
            var processor = new DeltaTimeProcessor();
            var value = new Vector2(180f, -90f);

            var result = processor.Process(value, null);

            Assert.AreEqual(value.x * Time.deltaTime, result.x, 1e-5f);
            Assert.AreEqual(value.y * Time.deltaTime, result.y, 1e-5f);
        }

        [Test]
        public void Process_KeepsTheZeroVector()
        {
            var processor = new DeltaTimeProcessor();

            Assert.AreEqual(Vector2.zero, processor.Process(Vector2.zero, null));
        }
    }
}
