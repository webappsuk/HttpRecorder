using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplications.HttpRecorder
{
    /// <summary>
    /// Recording options.
    /// </summary>
    public class RecordingOptions : IEquatable<RecordingOptions>
    {
        /// <summary>
        /// The default recording options.
        /// </summary>
        public static readonly RecordingOptions Default =
            new RecordingOptions(RecordMode.Auto, (IReadOnlyList<Parameter>)Array.Empty<Parameter>());

        /// <summary>
        /// Gets the mode.
        /// </summary>
        /// <value>
        /// The mode.
        /// </value>
        public RecordMode Mode { get; }

        /// <summary>
        /// Gets any parameters.
        /// </summary>
        /// <value>
        /// The parameters.
        /// </value>
        public IReadOnlyList<Parameter> Parameters { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordingOptions" /> class.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        public RecordingOptions(params Parameter[] parameters)
            : this(RecordMode.Default, NormalizeParameters(parameters))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordingOptions" /> class.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        public RecordingOptions(IEnumerable<Parameter> parameters)
            : this(RecordMode.Default, NormalizeParameters(parameters))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordingOptions" /> class.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="parameters">The parameters.</param>
        public RecordingOptions(
            RecordMode mode,
            params Parameter[] parameters)
            : this(mode, NormalizeParameters(parameters))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordingOptions" /> class.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="parameters">The parameters.</param>
        public RecordingOptions(
            RecordMode mode = RecordMode.Default,
            IEnumerable<Parameter> parameters = null)
            : this(mode, NormalizeParameters(parameters))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordingOptions" /> class. Used
        /// internally by <see cref="Combine" />, when parameters are already valid.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="parameters">The parameters.</param>
        private RecordingOptions(
            RecordMode mode,
            IReadOnlyList<Parameter> parameters)
        {
            Mode = mode;
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
        public RecordingOptions Combine(RecordingOptions options)
        {
            if (options is null)
                return this;

            int osc = options.Parameters.Count;
            if (osc < 1)
            {
                // No new parameters, check for mode change
                if (options.Mode == RecordMode.Default || options.Mode == Mode)
                    return this;

                return new RecordingOptions(options.Mode, Parameters);
            }

            int sc = Parameters.Count;
            if (sc < 1)
            {
                // Only have new parameters, if we have a mode we can use as is
                if (options.Mode != RecordMode.Default)
                    return options;

                return new RecordingOptions(Mode, options.Parameters);
            }

            // Need to create combination
            return new RecordingOptions(
                options.Mode == RecordMode.Default
                    ? Mode
                    : options.Mode,
                options.Parameters.Concat(Parameters)
            );
        }

        /// <inheritdoc />
        public bool Equals(RecordingOptions other)
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
            return obj.GetType() == GetType() && Equals((RecordingOptions)obj);
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
        public static bool operator ==(RecordingOptions left, RecordingOptions right)
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
        public static bool operator !=(RecordingOptions left, RecordingOptions right)
        {
            return !Equals(left, right);
        }
    }
}