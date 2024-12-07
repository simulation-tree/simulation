using System;
using Unmanaged;

namespace Simulation
{
    public readonly struct StatusCode : IEquatable<StatusCode>
    {
        /// <summary>
        /// Indicates that the program should continue running.
        /// </summary>
        public static readonly StatusCode Continue = new(0);

        /// <summary>
        /// Indicates that the program was terminated externally, and not on its own.
        /// </summary>
        public static readonly StatusCode Termination = Failure(byte.MaxValue);

        private readonly ushort value;

        public readonly bool IsContinue => value == 0;
        public readonly bool IsSuccess => (value & 1) == 1;
        public readonly bool IsFailure => (value & 2) == 1;
        public readonly byte Code => (byte)(value >> 2);

        private StatusCode(ushort value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            USpan<char> destination = stackalloc char[32];
            uint length = ToString(destination);
            return destination.Slice(0, length).ToString();
        }

        public readonly uint ToString(USpan<char> destination)
        {
            uint length = 0;
            if (HasSuccess(out byte code))
            {
                length += "Finished".AsUSpan().CopyTo(destination);
                destination[length++] = ' ';
                length += code.ToString(destination.Slice(length));
            }
            else if (HasFailure(out code))
            {
                length += "Failure".AsUSpan().CopyTo(destination);
                destination[length++] = ' ';
                length += code.ToString(destination.Slice(length));
            }
            else
            {
                length += "Continue".AsUSpan().CopyTo(destination);
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
            ushort value = (ushort)(code << 2 | 1);
            return new StatusCode(value);
        }

        public static StatusCode Failure(byte code)
        {
            ushort value = (ushort)(code << 2 | 2);
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
    }
}