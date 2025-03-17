using System;
using System.Diagnostics;

namespace Simulation
{
    /// <summary>
    /// Represents a status code for the result of an operation.
    /// </summary>
    public readonly struct StatusCode : IEquatable<StatusCode>
    {
        /// <summary>
        /// Maximum allowed value for status codes.
        /// </summary>
        public const byte MaxCode = 63;

        /// <summary>
        /// Indicates that the program should continue running.
        /// </summary>
        public static readonly StatusCode Continue = new(1);

        /// <summary>
        /// Indicates that the program was terminated externally, and not on its own.
        /// </summary>
        public static readonly StatusCode Termination = Failure(MaxCode);

        private readonly byte value;

        /// <summary>
        /// Checks if the status code represents continuation.
        /// </summary>
        public readonly bool IsContinue => (value & 1) != 0 && (value & 2) == 0;

        /// <summary>
        /// Checks if the status code represents a successful operation.
        /// </summary>
        public readonly bool IsSuccess => (value & 1) == 0 && (value & 2) != 0;

        /// <summary>
        /// Checks if the status code represents a failure.
        /// </summary>
        public readonly bool IsFailure => (value & 1) != 0 && (value & 2) != 0;

        /// <summary>
        /// The underlying status code.
        /// </summary>
        public readonly byte Code => (byte)(value >> 2);

#if NET
        /// <inheritdoc/>
        [Obsolete("Default constructor not supported", true)]
        public StatusCode()
        {
            throw new NotSupportedException();
        }
#endif

        private StatusCode(byte value)
        {
            this.value = value;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (HasSuccess(out byte code))
            {
                return $"Success {code}";
            }
            else if (HasFailure(out code))
            {
                return $"Failure {code}";
            }
            else if (value == 0)
            {
                return "Default";
            }
            else
            {
                return "Continue";
            }
        }

        /// <inheritdoc/>
        public readonly int ToString(Span<char> destination)
        {
            int length = 0;
            if (HasSuccess(out byte code))
            {
                destination[length++] = 'S';
                destination[length++] = 'u';
                destination[length++] = 'c';
                destination[length++] = 'c';
                destination[length++] = 'e';
                destination[length++] = 's';
                destination[length++] = 's';
                destination[length++] = ' ';
                length += code.ToString(destination.Slice(length));
            }
            else if (HasFailure(out code))
            {
                destination[length++] = 'F';
                destination[length++] = 'a';
                destination[length++] = 'i';
                destination[length++] = 'l';
                destination[length++] = 'u';
                destination[length++] = 'r';
                destination[length++] = 'e';
                destination[length++] = ' ';
                length += code.ToString(destination.Slice(length));
            }
            else if (value == 0)
            {
                destination[length++] = 'D';
                destination[length++] = 'e';
                destination[length++] = 'f';
                destination[length++] = 'a';
                destination[length++] = 'u';
                destination[length++] = 'l';
                destination[length++] = 't';
            }
            else
            {
                destination[length++] = 'C';
                destination[length++] = 'o';
                destination[length++] = 'n';
                destination[length++] = 't';
                destination[length++] = 'i';
                destination[length++] = 'n';
                destination[length++] = 'u';
                destination[length++] = 'e';
            }

            return length;
        }

        /// <summary>
        /// Tries to retrieve the code if the status code indicates success.
        /// </summary>
        public readonly bool HasSuccess(out byte code)
        {
            if (IsSuccess)
            {
                code = Code;
                return true;
            }
            else
            {
                code = default;
                return false;
            }
        }

        /// <summary>
        /// Tries to retrieve the code if the status code indicates failure.
        /// </summary>
        public readonly bool HasFailure(out byte code)
        {
            if (IsFailure)
            {
                code = Code;
                return true;
            }
            else
            {
                code = default;
                return false;
            }
        }

        /// <summary>
        /// Gets a status code indicating that the operation was successful
        /// with the given <paramref name="code"/>.
        /// </summary>
        public static StatusCode Success(byte code)
        {
            ThrowIfCodeIsOutOfRange(code);

            byte value = default;
            value |= 2;
            value |= (byte)(code << 2);
            return new StatusCode(value);
        }

        /// <summary>
        /// Gets a status code indicating that the operation was a failure
        /// with the given <paramref name="code"/>.
        /// </summary>
        public static StatusCode Failure(byte code)
        {
            ThrowIfCodeIsOutOfRange(code);

            byte value = default;
            value |= 3;
            value |= (byte)(code << 2);
            return new StatusCode(value);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is StatusCode code && Equals(code);
        }

        /// <inheritdoc/>
        public readonly bool Equals(StatusCode other)
        {
            return value == other.value;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return value;
        }

        /// <inheritdoc/>
        public static bool operator ==(StatusCode left, StatusCode right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(StatusCode left, StatusCode right)
        {
            return !(left == right);
        }

        [Conditional("DEBUG")]
        private static void ThrowIfCodeIsOutOfRange(byte code)
        {
            if (code > MaxCode)
            {
                throw new ArgumentOutOfRangeException(nameof(code), code, $"Status code must be between 0 and {MaxCode}");
            }
        }
    }
}