using NUnit.Framework;

namespace Game.ButtonTest.Tests
{
    // button-spec#tap-adds-one: mỗi lần chạm rồi nhả trên nút thử thì N tăng 1 ngay trong khung hình đó.
    public sealed class TapCounterTests
    {
        [Test]
        public void TapAddsOne_ReleaseOnButton_CountGoesUpByOne()
        {
            var counter = new TapCounter();

            bool counted = counter.Release(releasedOverButton: true);

            Assert.IsTrue(counted);
            Assert.AreEqual(1, counter.Count);
        }

        [Test]
        public void TapAddsOne_EachReleaseOnButton_AddsOneMore()
        {
            var counter = new TapCounter();

            counter.Release(true);
            counter.Release(true);
            counter.Release(true);

            Assert.AreEqual(3, counter.Count);
        }

        [Test]
        public void TapAddsOne_LabelIsToldTheNewCountInTheSameCall()
        {
            var counter = new TapCounter();
            long shown = -1;
            counter.Changed += n => shown = n;

            counter.Release(true);

            // Trễ 0 khung hình: N mới tới nhãn ngay trong lời gọi nhả, không chờ khung sau.
            Assert.AreEqual(1, shown);
        }

        [Test]
        public void TapAddsOne_ReleaseOutsideButton_DoesNotCount()
        {
            var counter = new TapCounter();
            bool changed = false;
            counter.Changed += _ => changed = true;

            bool counted = counter.Release(releasedOverButton: false);

            Assert.IsFalse(counted);
            Assert.AreEqual(0, counter.Count);
            Assert.IsFalse(changed);
        }
    }
}
