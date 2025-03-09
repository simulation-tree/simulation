using System;
using System.Diagnostics;

namespace Simulation
{
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

        public readonly bool IsContinue => (value & 1) != 0 && (value & 2) == 0;
        public readonly bool IsSuccess => (value & 1) == 0 && (value & 2) != 0;
        public readonly bool IsFailure => (value & 1) != 0 && (value & 2) != 0;
        public readonly byte Code => (byte)(value >> 2);

#if NET
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

        public override string ToString()
        {
            Span<char> destination = stackalloc char[32];
            int length = ToString(destination);
            return destination.Slice(0, length).ToString();
        }

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

        public static StatusCode Success(byte code)
        {
            ThrowIfCodeIsOutOfRange(code);

            byte value = default;
            value |= 2;
            value |= (byte)(code << 2);
            return new StatusCode(value);
        }

        public static StatusCode Failure(byte code)
        {
            ThrowIfCodeIsOutOfRange(code);

            byte value = default;
            value |= 3;
            value |= (byte)(code << 2);
            return new StatusCode(value);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is StatusCode code && Equals(code);
        }

        public readonly bool Equals(StatusCode other)
        {
            return value == other.value;
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(value);
        }

        public static bool operator ==(StatusCode left, StatusCode right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StatusCode left, StatusCode right)
        {
            return !(left == right);
        }

        public static implicit operator uint(StatusCode code)
        {
            return code.Code;
        }

        public static implicit operator int(StatusCode code)
        {
            return code.Code;
        }

        public static implicit operator byte(StatusCode code)
        {
            return code.Code;
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