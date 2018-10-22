using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplications.HttpRecorder
{
    /// <summary>
    /// Recording options.
    /// </summary>
    public class CassetteOptions : IEquatable<CassetteOptions>
    {
        /// <summary>
        /// The default recording options.
        /// </summary>
        public static readonly CassetteOptions Default =
            new CassetteOptions(RecordMode.Auto, false, (IReadOnlyList<Parameter>)Array.Empty<Parameter>());

        /// <summary>
        /// The playback mode.
        /// </summary>
        public static readonly CassetteOptions Playback =
            new CassetteOptions(RecordMode.Playback, null, (IReadOnlyList<Parameter>)null);

        /// <summary>
        /// The record mode.
        /// </summary>
        public static readonly CassetteOptions Record =
            new CassetteOptions(RecordMode.Record, null, (IReadOnlyList<Parameter>)null);

        /// <summary>
        /// The overwrite mode.
        /// </summary>
        public static readonly CassetteOptions Overwrite =
            new CassetteOptions(RecordMode.Overwrite, null, (IReadOnlyList<Parameter>)null);

        /// <summary>
        /// Gets the mode.
        /// </summary>
        /// <value>
        /// The mode.
        /// </value>
        public RecordMode Mode { get; }

        /// <summary>
        /// Gets a value indicating whether saving should be completed before the response is returned during recording.
        /// </summary>
        /// <value>
        ///   <c>true</c> if ensures save completes or <c>false</c> to return responses before they are saved;
        /// <c>null</c> indicates the defaults options should be used.
        /// </value>
        public bool? WaitForSave { get; }

        /// <summary>
        /// Gets any parameters.
        /// </summary>
        /// <value>
        /// The parameters.
        /// </value>
        public IReadOnlyList<Parameter> Parameters { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CassetteOptions" /> class.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        public CassetteOptions(params Parameter[] parameters)
            : this(RecordMode.Default, null, NormalizeParameters(parameters))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CassetteOptions" /> class.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        public CassetteOptions(IEnumerable<Parameter> parameters)
            : this(RecordMode.Default, null, NormalizeParameters(parameters))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CassetteOptions" /> class.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="waitForSave">if set to <c>true</c> wait for save when returning responses.</param>
        /// <param name="parameters">The parameters.</param>
        public CassetteOptions(
            RecordMode mode,
            bool? waitForSave,
            params Parameter[] parameters)
            : this(mode, waitForSave, NormalizeParameters(parameters))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CassetteOptions" /> class.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="waitForSave">if set to <c>true</c> wait for save when returning responses.</param>
        /// <param name="parameters">The parameters.</param>
        public CassetteOptions(
            RecordMode mode = RecordMode.Default,
            bool? waitForSave = null,
            IEnumerable<Parameter> parameters = null)
            : this(mode, waitForSave, NormalizeParameters(parameters))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CassetteOptions" /> class. Used
        /// internally by <see cref="Combine" />, when parameters are already valid.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="waitForSave">if set to <c>true</c> wait for save when returning responses.</param>
        /// <param name="parameters">The parameters.</param>
        private CassetteOptions(
            RecordMode mode,
            bool? waitForSave,
            IReadOnlyList<Parameter> parameters)
        {
            Mode = mode;
            WaitForSave = waitForSave;
            Parameters = parameters;
        }

        /// <summary>
        /// Normalizes the parameters to a distinct ordered list.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        private static IReadOnlyList<Parameter> NormalizeParameters(IEnumerable<Parameter> parameters)
        {
            if (parameters is null)
                return Array.Empty<Parameter>();

            Parameter[] orderedDistinct = parameters.OrderBy(s => s.Value).Distinct().ToArray();
            return orderedDistinct.Length < 1 ? Array.Empty<Parameter>() : orderedDistinct;
        }


        /// <summary>
        /// Combines the <param name="options">specified options</param> with these options.
        /// </summary>
        /// <param name="options">The options to merge over these options.</param>
        /// <returns></returns>
        public CassetteOptions Combine(CassetteOptions options)
        {
            if (options is null)
                return this;

            RecordMode mode = options.Mode == RecordMode.Default ? Mode : options.Mode;
            bool? waitForSave = options.WaitForSave ?? WaitForSave;

            int osc = options.Parameters.Count;
            if (osc < 1)
            {
                // No new parameters, check for mode change
                if (mode == Mode && waitForSave == WaitForSave)
                    return this;

                return new CassetteOptions(mode, waitForSave, Parameters);
            }

            int sc = Parameters.Count;
            if (sc < 1)
            {
                // Only have new parameters, if we have a mode we can use as is
                if (mode == options.Mode && waitForSave == options.WaitForSave)
                    return options;

                return new CassetteOptions(mode, options.WaitForSave, options.Parameters);
            }

            // Need to create combination
            return new CassetteOptions(
                mode,
                waitForSave,
                options.Parameters.Concat(Parameters)
            );
        }

        /// <inheritdoc />
        public bool Equals(CassetteOptions other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Mode == other.Mode && Parameters.SequenceEqual(other.Parameters);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((CassetteOptions)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Mode;
                foreach (Parameter parameter in Parameters)
                    hashCode = (hashCode * 397) ^ parameter.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(CassetteOptions left, CassetteOptions right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(CassetteOptions left, CassetteOptions right)
        {
            return !Equals(left, right);
        }
    }
}