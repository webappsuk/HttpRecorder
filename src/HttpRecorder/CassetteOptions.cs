using System;

namespace WebApplications.HttpRecorder
{
    /// <summary>
    /// Recording options.
    /// </summary>
    public sealed class CassetteOptions : IEquatable<CassetteOptions>
    {
        /// <summary>
        /// Indicates options that have no effect on the current defaults.
        /// </summary>
        public static readonly CassetteOptions None = new CassetteOptions();

        /// <summary>
        /// The default recording options.
        /// </summary>
        public static readonly CassetteOptions Default =
            new CassetteOptions(
                RecordMode.Auto,
                false,
                TimeSpan.Zero,
                HttpRecorder.RequestRecordMode.Ignore,
                HttpRecorder.RequestPlaybackMode.Auto);

        /// <summary>
        /// The playback mode.
        /// </summary>
        public static readonly CassetteOptions Playback =
            new CassetteOptions(RecordMode.Playback);

        /// <summary>
        /// The record mode.
        /// </summary>
        public static readonly CassetteOptions Record =
            new CassetteOptions(RecordMode.Record);

        /// <summary>
        /// The overwrite mode.
        /// </summary>
        public static readonly CassetteOptions Overwrite =
            new CassetteOptions(RecordMode.Overwrite);

        /// <summary>
        /// Will ensure recordings are saved before returning a response.
        /// </summary>
        public static readonly CassetteOptions WaitUntilSaved =
            new CassetteOptions(waitForSave: true);

        /// <summary>
        /// Will simulate the delay based on the original response duration.
        /// </summary>
        public static readonly CassetteOptions RecordedDelay =
            new CassetteOptions(simulateDelay: TimeSpan.MinValue);

        /// <summary>
        /// Will record changes made to the request object by the message handler.
        /// </summary>
        public static readonly CassetteOptions RecordedRequestChanges =
            new CassetteOptions(requestRecordMode: HttpRecorder.RequestRecordMode.RecordIfChanged);

        /// <summary>
        /// Will record the request.
        /// </summary>
        public static readonly CassetteOptions RecordedRequests =
            new CassetteOptions(requestRecordMode: HttpRecorder.RequestRecordMode.AlwaysRecord);

        /// <summary>
        /// Gets the mode.
        /// </summary>
        /// <value>
        /// The mode.
        /// </value>
        public RecordMode? Mode { get; }

        /// <summary>
        /// Gets a value indicating whether saving should be completed before the response is returned during recording.
        /// </summary>
        /// <value>
        ///   <c>true</c> if ensures save completes or <c>false</c> to return responses before they are saved;
        /// <c>null</c> indicates the defaults options should be used.
        /// </value>
        public bool? WaitForSave { get; }

        /// <summary>
        /// Gets a value indicating whether playback should simulate a delay in receiving a response (useful for testing).
        /// </summary>
        /// <value>
        ///   Positive values indicate the amount to wait; <see cref="TimeSpan.Zero"/> indicates no delay (default);
        /// negative values wil use the delay stored in the original recording.
        /// </value>
        public TimeSpan? SimulateDelay { get; }

        /// <summary>
        /// Gets a value indicating how response requests should be handled.
        /// </summary>
        /// <value>
        /// The request handling.
        /// </value>
        public RequestRecordMode? RequestRecordMode { get; }

        /// <summary>
        /// Gets a value indicating how response requests should be handled.
        /// </summary>
        /// <value>
        /// The request handling.
        /// </value>
        public RequestPlaybackMode? RequestPlaybackMode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CassetteOptions" /> class.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="waitForSave">if set to <c>true</c> wait for save when returning responses.</param>
        /// <param name="simulateDelay">The simulate delay.</param>
        /// <param name="requestRecordMode">The request recording mode.</param>
        /// <param name="requestPlaybackMode">The request playback mode.</param>
        public CassetteOptions(
            RecordMode? mode = null,
            bool? waitForSave = null,
            TimeSpan? simulateDelay = null,
            RequestRecordMode? requestRecordMode = null,
            RequestPlaybackMode? requestPlaybackMode = null)
        {
            Mode = mode;
            WaitForSave = waitForSave;
            SimulateDelay = simulateDelay;
            RequestRecordMode = requestRecordMode;
            RequestPlaybackMode = requestPlaybackMode;
        }

        /// <inheritdoc />
        public bool Equals(CassetteOptions other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Mode == other.Mode &&
                   WaitForSave == other.WaitForSave &&
                   SimulateDelay.Equals(other.SimulateDelay) &&
                   RequestRecordMode == other.RequestRecordMode &&
                   RequestPlaybackMode == other.RequestPlaybackMode;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is CassetteOptions other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Mode.GetHashCode();
                hashCode = (hashCode * 397) ^ WaitForSave.GetHashCode();
                hashCode = (hashCode * 397) ^ SimulateDelay.GetHashCode();
                hashCode = (hashCode * 397) ^ RequestRecordMode.GetHashCode();
                hashCode = (hashCode * 397) ^ RequestPlaybackMode.GetHashCode();
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
        public static bool operator ==(CassetteOptions left, CassetteOptions right) => Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(CassetteOptions left, CassetteOptions right) => !Equals(left, right);

        /// <summary>
        /// Implements the operator &amp; overwriting the <paramref name="left"/> options with
        /// any non-<c>null</c> <paramref name="right"/> options.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static CassetteOptions operator &(CassetteOptions left, CassetteOptions right)
            => right is null ? left :
                left is null ? right :
                new CassetteOptions(
                    right.Mode ?? left.Mode,
                    right.WaitForSave ?? left.WaitForSave,
                    right.SimulateDelay ?? left.SimulateDelay,
                    right.RequestRecordMode ?? left.RequestRecordMode,
                    right.RequestPlaybackMode ?? left.RequestPlaybackMode);
    }
}