﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace CbzMage.Shared.Buffers
{
    /// <summary>
    /// Represents a heap-based, array-backed output sink into which byte data can be written.
    /// Adapted from ArrayBufferWriter: 
    /// https://github.com/dotnet/runtime/blob/bd6709248295deefa1956da3aeb9b0e086fbaca5/src/libraries/Common/src/System/Buffers/ArrayBufferWriter.cs
    /// </summary>
    public class ByteArrayBufferWriter : IBufferWriter<byte>
    {
        // Copy of Array.MaxLength.
        // Used by projects targeting .NET Framework.
        private const int ArrayMaxLength = 0x7FFFFFC7;

        private const int DefaultInitialBufferSize = 256;

        private byte[] _buffer;
        private int _index;

        /// <summary>
        /// Creates an instance of an <see cref="ByteArrayBufferWriter"/>, in which data can be written to,
        /// with the default initial capacity.
        /// </summary>
        public ByteArrayBufferWriter()
        {
            _buffer = Array.Empty<byte>();
            _index = 0;
        }

        /// <summary>
        /// Creates an instance of an <see cref="ByteArrayBufferWriter"/>, in which data can be written to,
        /// with an initial capacity specified.
        /// </summary>
        /// <param name="initialCapacity">The minimum capacity with which to initialize the underlying buffer.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="initialCapacity"/> is not positive (i.e. less than or equal to 0).
        /// </exception>
        public ByteArrayBufferWriter(int initialCapacity)
        {
            if (initialCapacity <= 0)
                throw new ArgumentException(null, nameof(initialCapacity));

            if (initialCapacity == 0)
            {
                _buffer = Array.Empty<byte>();
            }
            else
            {
                _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
            }

            _index = 0;
        }

        public ByteArrayBufferWriter(byte[] arrayPoolBuffer)
        {
            ArgumentNullException.ThrowIfNull(arrayPoolBuffer);

            _buffer = arrayPoolBuffer;
            _index = 0;
        }

        /// <summary>
        /// Returns the underlying buffer (any data written to the buffer is not cleared).
        /// </summary>
        public byte[] Buffer => _buffer;

        /// <summary>
        /// Returns the data written to the underlying buffer so far, as a <see cref="ReadOnlyMemory{T}"/>.
        /// </summary>
        public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _index);

        /// <summary>
        /// Returns the data written to the underlying buffer so far, as a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _index);

        /// <summary>
        /// Returns the amount of data written to the underlying buffer so far.
        /// </summary>
        public int WrittenCount => _index;

        /// <summary>
        /// Returns the total amount of space within the underlying buffer.
        /// </summary>
        public int Capacity => _buffer.Length;

        /// <summary>
        /// Returns the amount of space available that can still be written into without forcing the underlying buffer to grow.
        /// </summary>
        public int FreeCapacity => _buffer.Length - _index;

        /// <summary>
        /// Returns the underlying buffer to the shared <see cref="ArrayPool{T}"/>. Do not use
        /// the <see cref="ByteArrayBufferWriter"/> after returning its buffer.
        /// </summary>
        /// <param name="clearData">Clear the data before returning the buffer to the pool.</param>
        public void ReturnBuffer(bool clearData = false)
        {
            if (_buffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(_buffer, clearData);
            }

            _index = 0;
        }

        /// <summary>
        /// Reset the <see cref="ByteArrayBufferWriter"/> so it can be reused.
        /// </summary>
        /// <param name="clearData">Clear the data before reuse.</param>
        public void Reset(bool clearData = false)
        {
            Debug.Assert(_buffer.Length >= _index);

            if (clearData)
            {
                _buffer.AsSpan(0, _index).Clear();
            }

            _index = 0;
        }

        /// <summary>
        /// Notifies <see cref="IBufferWriter{T}"/> that <paramref name="count"/> amount of data was written to the output <see cref="Span{T}"/>/<see cref="Memory{T}"/>
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to advance past the end of the underlying buffer.
        /// </exception>
        /// <remarks>
        /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
        /// </remarks>
        public void Advance(int count)
        {
            if (count < 0)
                throw new ArgumentException(null, nameof(count));

            if (_index > _buffer.Length - count)
                ThrowInvalidOperationException_AdvancedTooFar(_buffer.Length);

            _index += count;
        }

        /// <summary>
        /// Notifies <see cref="ByteArrayBufferWriter"/> that the last <paramref name="count"/> amount of data is no longer considered written.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the amount withdrawn is larger than the <see cref="Capacity"/>
        /// </exception>
        public void Withdraw(int count)
        {
            if (count < 0)
                throw new ArgumentException(null, nameof(count));

            if (_index - count < 0)
                throw new InvalidOperationException("Buffer index below 0");

            _index -= count;
        }

        /// <summary>
        /// Returns a <see cref="Memory{T}"/> to write to that is at least the requested length (specified by <paramref name="sizeHint"/>).
        /// If no <paramref name="sizeHint"/> is provided (or it's equal to <code>0</code>), some non-empty buffer is returned.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="sizeHint"/> is negative.
        /// </exception>
        /// <remarks>
        /// This will never return an empty <see cref="Memory{T}"/>.
        /// </remarks>
        /// <remarks>
        /// There is no guarantee that successive calls will return the same buffer or the same-sized buffer.
        /// </remarks>
        /// <remarks>
        /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
        /// </remarks>
        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            Debug.Assert(_buffer.Length > _index);
            return _buffer.AsMemory(_index);
        }

        /// <summary>
        /// Returns a <see cref="Span{T}"/> to write to that is at least the requested length (specified by <paramref name="sizeHint"/>).
        /// If no <paramref name="sizeHint"/> is provided (or it's equal to <code>0</code>), some non-empty buffer is returned.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="sizeHint"/> is negative.
        /// </exception>
        /// <remarks>
        /// This will never return an empty <see cref="Span{T}"/>.
        /// </remarks>
        /// <remarks>
        /// There is no guarantee that successive calls will return the same buffer or the same-sized buffer.
        /// </remarks>
        /// <remarks>
        /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
        /// </remarks>
        public Span<byte> GetSpan(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            Debug.Assert(_buffer.Length > _index);
            return _buffer.AsSpan(_index);
        }

        private void CheckAndResizeBuffer(int sizeHint)
        {
            if (sizeHint < 0)
                throw new ArgumentException(nameof(sizeHint));

            if (sizeHint == 0)
            {
                sizeHint = 1;
            }

            if (sizeHint > FreeCapacity)
            {
                int currentLength = _buffer.Length;

                // Attempt to grow by the larger of the sizeHint and double the current size.
                int growBy = Math.Max(sizeHint, currentLength);

                if (currentLength == 0)
                {
                    growBy = Math.Max(growBy, DefaultInitialBufferSize);
                }

                int newSize = currentLength + growBy;

                if ((uint)newSize > int.MaxValue)
                {
                    // Attempt to grow to ArrayMaxLength.
                    uint needed = (uint)(currentLength - FreeCapacity + sizeHint);
                    Debug.Assert(needed > currentLength);

                    if (needed > ArrayMaxLength)
                    {
                        ThrowOutOfMemoryException(needed);
                    }

                    newSize = ArrayMaxLength;
                }

                var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);

                if (_buffer.Length > 0)
                {
                    Array.Copy(_buffer, 0, newBuffer, 0, _index);
                    //System.Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _index);

                    ArrayPool<byte>.Shared.Return(_buffer);
                }

                _buffer = newBuffer;
            }

            Debug.Assert(FreeCapacity > 0 && FreeCapacity >= sizeHint);
        }

        private static void ThrowInvalidOperationException_AdvancedTooFar(int capacity)
        {
            throw new InvalidOperationException(capacity.ToString());
        }

        private static void ThrowOutOfMemoryException(uint capacity)
        {
            throw new OutOfMemoryException(capacity.ToString());
        }
    }
}