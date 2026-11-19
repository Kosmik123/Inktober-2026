using Inktober.Game;
using NUnit.Framework;
using UnityEngine;

namespace Inktober.Tests.EditMode
{
    public class BestScoreTests
    {
        // The same key as in BestScore. The tests save and restore the real value, so the developer's record is not lost.
        private const string Key = "Inktober.Apple.BestLength";

        private bool hadRecord;
        private float savedRecord;

        [SetUp]
        public void SetUp()
        {
            hadRecord = PlayerPrefs.HasKey(Key);
            savedRecord = PlayerPrefs.GetFloat(Key, 0f);
            PlayerPrefs.DeleteKey(Key);
        }

        [TearDown]
        public void TearDown()
        {
            if (hadRecord)
                PlayerPrefs.SetFloat(Key, savedRecord);
            else
                PlayerPrefs.DeleteKey(Key);
        }

        [Test]
        public void WithoutRecord_LengthIsZero()
        {
            Assert.AreEqual(0f, BestScore.Length);
        }

        [Test]
        public void FirstPositiveLength_IsARecord()
        {
            Assert.IsTrue(BestScore.TrySet(1.5f));
            Assert.AreEqual(1.5f, BestScore.Length);
        }

        [Test]
        public void ShorterLength_KeepsTheRecord()
        {
            BestScore.TrySet(2f);

            Assert.IsFalse(BestScore.TrySet(1f));
            Assert.AreEqual(2f, BestScore.Length);
        }

        [Test]
        public void EqualLength_IsNotANewRecord()
        {
            BestScore.TrySet(2f);

            Assert.IsFalse(BestScore.TrySet(2f));
        }

        [Test]
        public void LongerLength_ReplacesTheRecord()
        {
            BestScore.TrySet(2f);

            Assert.IsTrue(BestScore.TrySet(3f));
            Assert.AreEqual(3f, BestScore.Length);
        }

        [Test]
        public void ZeroLength_IsNeverARecord()
        {
            Assert.IsFalse(BestScore.TrySet(0f));
        }
    }
}
