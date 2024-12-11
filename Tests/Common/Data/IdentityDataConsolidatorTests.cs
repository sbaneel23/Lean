/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using NUnit.Framework;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Data;

namespace QuantConnect.Tests.Common.Data
{
    [TestFixture]
    public class IdentityDataConsolidatorTests
    {
        [Test]
        public void ThrowsOnDataOfWrongType()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var identity = new IdentityDataConsolidator<Tick>();
                identity.Update(new TradeBar());
            });
        }

        [Test]
        public void ReturnsTheSameObjectReference()
        {
            using var identity = new IdentityDataConsolidator<Tick>();

            var tick = new Tick();

            int count = 0;
            identity.DataConsolidated += (sender, data) =>
            {
                Assert.IsTrue(ReferenceEquals(tick, data));
                count++;
            };

            identity.Update(tick);
            Assert.AreEqual(1, count);
        }

        [Test]
        public void IgnoresNonTickDataWithSameTimestamps()
        {
            var reference = new DateTime(2015, 09, 23);
            using var identity = new IdentityDataConsolidator<TradeBar>();

            int count = 0;
            identity.DataConsolidated += (sender, data) =>
            {
                count++;
            };

            var tradeBar = new TradeBar { EndTime = reference };
            identity.Update(tradeBar);

            tradeBar = (TradeBar)tradeBar.Clone();
            identity.Update(tradeBar);

            Assert.AreEqual(1, count);
        }

        [Test]
        public void AcceptsTickDataWithSameTimestamps()
        {
            var reference = new DateTime(2015, 09, 23);
            using var identity = new IdentityDataConsolidator<Tick>();

            int count = 0;
            identity.DataConsolidated += (sender, data) =>
            {
                count++;
            };

            var tradeBar = new Tick { EndTime = reference };
            identity.Update(tradeBar);

            tradeBar = (Tick)tradeBar.Clone();
            identity.Update(tradeBar);

            Assert.AreEqual(2, count);
        }

        [Test]
        public void TriggersOnDataConsolidatedForFillForwardData()
        {
            // Arrange
            using (var consolidator = new IdentityDataConsolidator<TradeBar>())
            {
                // Create a TradeBar instance
                var fillForwardData = new TradeBar
                {
                    Time = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.AddMinutes(1),
                    Value = 100
                };

                // Use reflection to set the IsFillForward property
                var property = typeof(BaseData).GetProperty(
                    nameof(BaseData.IsFillForward),
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                property.SetValue(fillForwardData, true);

                bool eventTriggered = false;

                // Act
                consolidator.DataConsolidated += (sender, consolidated) =>
                {
                    eventTriggered = true;
                };
                consolidator.Update(fillForwardData);

                // Assert
                Assert.IsTrue(eventTriggered, "DataConsolidated event should trigger for fill-forward data.");
            }
        }
    }
}
