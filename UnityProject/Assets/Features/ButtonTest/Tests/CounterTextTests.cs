using NUnit.Framework;

namespace Game.ButtonTest.Tests
{
    // button-spec#counter-text và #edge-n-rat-lon.
    public sealed class CounterTextTests
    {
        [Test]
        public void CounterText_StartsAtDaBam0()
        {
            Assert.AreEqual("Đã bấm: 0", CounterText.Format(new TapCounter().Count));
        }

        [Test]
        public void CounterText_ShowsWholeNumberWithoutShortening()
        {
            Assert.AreEqual("Đã bấm: 1250", CounterText.Format(1250));
        }

        [Test]
        public void CounterText_FollowsCountAfterTaps()
        {
            var counter = new TapCounter();
            counter.Release(true);
            counter.Release(true);

            Assert.AreEqual("Đã bấm: 2", CounterText.Format(counter.Count));
        }

        [Test]
        public void EdgeNRatLon_VeryLargeCount_IsWrittenInFull()
        {
            Assert.AreEqual("Đã bấm: 9876543210123", CounterText.Format(9876543210123L));
        }

        [Test]
        public void EdgeNRatLon_NoCapAtIntRange_CountPastIntMaxIsWrittenInFull()
        {
            // N là long: không có trần ở int; chữ vẫn ghi đủ số khi vượt int.MaxValue.
            long beyondInt = (long)int.MaxValue + 1;

            Assert.AreEqual("Đã bấm: 2147483648", CounterText.Format(beyondInt));
        }
    }
}
