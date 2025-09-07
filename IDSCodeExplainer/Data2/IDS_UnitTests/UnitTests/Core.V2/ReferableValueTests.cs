using IDS.Core.V2.DataModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class ReferableValueTests
    {
        [TestMethod]
        public void CopyableTest()
        {
            const double sampleDoubleValue = 123.456789;
            var refValue = new ReferableValue<double>
            {
                Value = sampleDoubleValue
            };
            var copiedRefValue = refValue;
            Assert.AreEqual(sampleDoubleValue, copiedRefValue.Value);
        }

        [TestMethod]
        public void ValueNonReferenceTest()
        {
            const double sampleDoubleValue1 = 123.456789;
            const double sampleDoubleValue2 = 987654.321;
            var value = sampleDoubleValue1;
            var copiedValue = value;

            Assert.AreEqual(sampleDoubleValue1, copiedValue);

            value = sampleDoubleValue2;

            Assert.AreEqual(sampleDoubleValue2, value);
            Assert.AreEqual(sampleDoubleValue1, copiedValue);
        }

        [TestMethod]
        public void ValueAsReferenceTest()
        {
            const double sampleDoubleValue1 = 123.456789;
            const double sampleDoubleValue2 = 987654.321;
            var refValue = new ReferableValue<double>
            {
                Value = sampleDoubleValue1
            };
            var copiedRefValue = refValue;

            Assert.AreEqual(sampleDoubleValue1, copiedRefValue.Value);

            refValue.Value = sampleDoubleValue2;

            Assert.AreEqual(sampleDoubleValue2, refValue.Value);
            Assert.AreEqual(sampleDoubleValue2, refValue.Value);
        }

        [TestMethod]
        public void LockReferableValueTest()
        {
            const double sampleDoubleValue1 = 123.456789;
            const double sampleDoubleValue2 = 987654.321;

            var refValue = new ReferableValue<double>
            {
                Value = sampleDoubleValue1
            };

            var thread = new Thread(() =>
            {
                lock (refValue)
                {
                    refValue.Value = sampleDoubleValue2;
                }
            });

            // Lock the value before start the thread
            lock (refValue)
            {
                thread.Start();
                // Give some delay to sure the thread been started
                Thread.Sleep(1);
                // Since the value being lock, the other thread will not run and value still remain
                Assert.AreEqual(sampleDoubleValue1, refValue.Value);
            }

            thread.Join();
            Assert.AreEqual(sampleDoubleValue2, refValue.Value);
        }
    }
}
