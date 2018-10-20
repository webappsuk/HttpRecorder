using System;

namespace WebApplications.HttpRecorder
{
    /// <summary>
    /// Allows for parameterization of recordings.
    /// TODO This needs a lot of thought, particularly how to extensibly and safely match in the response.
    /// </summary>
    public class Parameter : IEquatable<Parameter>
    {
        /// <summary>
        /// The parts to match on request.
        /// </summary>
        public RequestParts RequestParts { get; }

        /// <summary>
        /// The parameter name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The value to use.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Parameter" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <param name="requestParts">The request parts to search.</param>
        /// <exception cref="ArgumentOutOfRangeException">name</exception>
        /// <exception cref="ArgumentNullException">value</exception>
        public Parameter(string name, string value, RequestParts requestParts)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentOutOfRangeException(nameof(name));
            Value = value ?? throw new ArgumentNullException(nameof(value));
            RequestParts = requestParts;
        }

        /// <inheritdoc />
        public bool Equals(Parameter other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return RequestParts == other.RequestParts &&
                   string.Equals(Value, other.Value) &&
                   string.Equals(Name, other.Name);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Parameter)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)RequestParts;
                hashCode = (hashCode * 397) ^ Value.GetHashCode();
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
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
        public static bool operator ==(Parameter left, Parameter right) => Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(Parameter left, Parameter right) => !Equals(left, right);
    }
}