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
using QuantConnect.Data;
using System.Diagnostics;
using QuantConnect.Logging;
using System.Collections.Generic;
using QuantConnect.Data.Consolidators;
using System.Collections;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Abstract Indicator base, meant to contain non-generic fields of indicator base to support non-typed inputs
    /// </summary>
    public abstract partial class IndicatorBase : IIndicator, IEnumerable<IndicatorDataPoint>
    {
        /// <summary>
        /// The data consolidators associated with this indicator if any
        /// </summary>
        /// <remarks>These references allow us to unregister an indicator from getting future data updates through it's consolidators.
        /// We need multiple consolitadors because some indicators consume data from multiple different symbols</remarks>
        public ISet<IDataConsolidator> Consolidators { get; } = new HashSet<IDataConsolidator>();

        /// <summary>
        /// Gets the current state of this indicator. If the state has not been updated
        /// then the time on the value will equal DateTime.MinValue.
        /// </summary>
        public IndicatorDataPoint Current
        {
            get
            {
                return Window[0];
            }
            protected set
            {
                Window.Add(value);
            }
        }

        /// <summary>
        /// Gets the previous state of this indicator. If the state has not been updated
        /// then the time on the value will equal DateTime.MinValue.
        /// </summary>
        public IndicatorDataPoint Previous
        {
            get
            {
                return Window.Count > 1 ? Window[1] : new IndicatorDataPoint(DateTime.MinValue, 0);
            }
        }

        /// <summary>
        /// Gets a name for this indicator
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets the number of samples processed by this indicator
        /// </summary>
        public long Samples { get; internal set; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public abstract bool IsReady { get; }

        /// <summary>
        /// Event handler that fires after this indicator is updated
        /// </summary>
        public event IndicatorUpdatedHandler Updated;

        /// <summary>
        /// A rolling window keeping a history of the indicator values of a given period
        /// </summary>
        public RollingWindow<IndicatorDataPoint> Window { get; }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public abstract void Reset();

        /// <summary>
        /// Initializes a new instance of the Indicator class.
        /// </summary>
        protected IndicatorBase()
        {
            Window = new RollingWindow<IndicatorDataPoint>(Indicator.DefaultWindowSize);
            Current = new IndicatorDataPoint(DateTime.MinValue, 0m);
        }

        /// <summary>
        /// Initializes a new instance of the Indicator class using the specified name.
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        protected IndicatorBase(string name)
            : this()
        {
            Name = name;
        }

        /// <summary>
        /// Event invocator for the Updated event
        /// </summary>
        /// <param name="consolidated">This is the new piece of data produced by this indicator</param>
        protected virtual void OnUpdated(IndicatorDataPoint consolidated)
        {
            Updated?.Invoke(this, consolidated);
        }

        /// <summary>
        /// Updates the state of this indicator with the given value and returns true
        /// if this indicator is ready, false otherwise
        /// </summary>
        /// <param name="input">The value to use to update this indicator</param>
        /// <returns>True if this indicator is ready, false otherwise</returns>
        public abstract bool Update(IBaseData input);

        /// <summary>
        /// Indexes the history windows, where index 0 is the most recent indicator value.
        /// If index is greater or equal than the current count, it returns null.
        /// If the index is greater or equal than the window size, it returns null and resizes the windows to i + 1.
        /// </summary>
        /// <param name="index">The index of the value to retrieve (0 = most recent).</param>
        /// <returns>The value at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index exceeds the window size.</exception>
        public IndicatorDataPoint GetHistoricalValue(int index)
        {
            if (index >= Window.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    $"Index {index} is out of range. The window size is {Window.Count}."
                );
            }

            return Window[index];
        }

        /// <summary>
        /// Indexes the history windows, where index 0 is the most recent indicator value.
        /// </summary>
        public IndicatorDataPoint this[int i] => GetHistoricalValue(i);

        /// <summary>
        /// Compares the current instance with another object of the same type.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared:
        /// Less than zero: This instance is less than <paramref name="obj"/>.
        /// Zero: This instance is equal to <paramref name="obj"/>.
        /// Greater than zero: This instance is greater than <paramref name="obj"/>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when the provided object is not of the same type.</exception>
        public int CompareTo(object obj)
        {
            if (obj is IndicatorBase other)
            {
                return CompareTo(other);
            }

            throw new ArgumentException($"Object must be of type {GetType().Name}");
        }

        /// <summary>
        /// Compares the current object with another indicator.
        /// </summary>
        /// <param name="other">The other indicator to compare with.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared:
        /// Less than zero: This object is less than <paramref name="other"/>.
        /// Zero: This object is equal to <paramref name="other"/>.
        /// Greater than zero: This object is greater than <paramref name="other"/>.
        /// </returns>
        public int CompareTo(IIndicator other)
        {
            if (ReferenceEquals(other, null))
            {
                return 1; // Everything is greater than null
            }

            return Current.CompareTo(other.Current);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the history window.
        /// </summary>
        public IEnumerator<IndicatorDataPoint> GetEnumerator() => Window.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Returns a string representation of the current indicator value.
        /// </summary>
        /// <returns>A string representation of the current value.</returns>
        public override string ToString() => Current.Value.ToStringInvariant("#######0.0####");

        /// <summary>
        /// Provides a detailed string representation of the indicator's current state, including its name and value.
        /// </summary>
        /// <returns>A detailed string representation of the indicator's current state.</returns>
        public string ToDetailedString() => $"{Name} - {this}";
    }
}
