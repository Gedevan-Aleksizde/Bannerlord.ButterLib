﻿using TaleWorlds.ObjectSystem;

// ReSharper disable once CheckNamespace
namespace Bannerlord.ButterLib.Common.Helpers
{
    /// <summary>Helper class that implements the pairing function created by Matthew Szudzik.</summary>
    /// <remarks>
    /// A pairing function on a set A associates each pair of members from A with a single member of B,
    /// so that any two distinct pairs of members from A are associated with two distinct members of B.
    /// </remarks>
    public static class ElegantPairHelper
    {
        /// <summary>Pairs two 32-bit signed integers based on their position.</summary>
        /// <param name="a">First 32-bit signed integer in a pair.</param>
        /// <param name="b">Second 32-bit signed integer in a pair.</param>
        /// <returns>A 64-bit signed integer representing the initial pair.</returns>
        public static long Pair(int a, int b)
        {
            var A = (ulong) (a >= 0 ? 2 * (long) a : -2 * (long) a - 1);
            var B = (ulong) (b >= 0 ? 2 * (long) b : -2 * (long) b - 1);
            var C = (long) ((A >= B ? A * A + A + B : A + B * B) / 2);
            return a < 0 && b < 0 || a >= 0 && b >= 0
                ? C
                : -C - 1;
        }

        /// <summary>Pairs two 32-bit unsigned integers based on their position.</summary>
        /// <param name="a">First 32-bit unsigned integer in a pair.</param>
        /// <param name="b">Second 32-bit unsigned integer in a pair.</param>
        /// <returns>A 64-bit unsigned integer representing the initial pair.</returns>
        public static ulong Pair(uint a, uint b)
        {
            var A = (ulong) a;
            var B = (ulong) b;
            return A >= B
                ? A * A + A + B
                : A + B * B;
        }

        /// <summary>Pairs two 16-bit signed integers based on their position.</summary>
        /// <param name="a">First 16-bit signed integer in a pair.</param>
        /// <param name="b">Second 16-bit signed integer in a pair.</param>
        /// <returns>A 32-bit signed integer representing the initial pair.</returns>
        public static int Pair(short a, short b)
        {
            var A = (uint) (a >= 0 ? 2 * a : -2 * a - 1);
            var B = (uint) (b >= 0 ? 2 * b : -2 * b - 1);
            var C = (int) ((A >= B ? A * A + A + B : A + B * B) / 2);
            return a < 0 && b < 0 || a >= 0 && b >= 0
                ? C
                : -C - 1;
        }

        /// <summary>Pairs two 16-bit unsigned integers based on their position.</summary>
        /// <param name="a">First 16-bit unsigned integer in a pair.</param>
        /// <param name="b">Second 16-bit unsigned integer in a pair.</param>
        /// <returns>A 32-bit unsigned integer representing the initial pair.</returns>
        public static uint Pair(ushort a, ushort b)
        {
            var A = (uint) a;
            var B = (uint) b;
            return A >= B
                ? A * A + A + B
                : A + B * B;
        }

        /// <summary>Pairs two <see cref="MBGUID" /> objects based on their position.</summary>
        /// <param name="a">First <see cref="MBGUID" /> object in a pair.</param>
        /// <param name="b">Second <see cref="MBGUID" /> object in a pair.</param>
        /// <returns>A 64-bit unsigned integer representing the initial pair.</returns>
        public static ulong Pair(MBGUID a, MBGUID b) => Pair(a.InternalValue, b.InternalValue);

        /// <summary>Unpairs an 64-bit unsigned integer representing the pair into two 32-bit unsigned integers with regard to their initial positions before pairing.</summary>
        /// <param name="pairValue">A 64-bit unsigned integer representing the pair.</param>
        /// <returns>A tuple of 32-bit unsigned integers that were paired.</returns>
        public static (uint A, uint B) UnPair(ulong pairValue)
        {
            var sqrt = IntegerSqrt(pairValue);
            var remainder = pairValue - sqrt * sqrt;
            return remainder < sqrt
                ? ((uint) remainder, (uint) sqrt)
                : ((uint) sqrt, (uint) (remainder - sqrt));
        }

        /// <summary>Unpairs an 64-bit signed integer representing the pair into two 32-bit signed integers with regard to their initial positions before pairing.</summary>
        /// <param name="pairValue">A 64-bit signed integer representing the pair.</param>
        /// <returns>A tuple of 32-bit signed integers that were paired.</returns>
        /// <remarks>Due to some imperfections of the negative pairing algorithm, this method could be rather slow.</remarks>
        public static (int A, int B) UnPair(long pairValue)
        {
            var z = pairValue >= 0 ? pairValue : -pairValue - 1;
            var Z = 2 * (ulong) z;

            var uRes = UnPair(Z);
            var firstPossibleResult = GetSignedTuple(uRes);

            return Pair(firstPossibleResult.A, firstPossibleResult.B) == pairValue
                ? firstPossibleResult
                : GetSignedTuple(UnPair(Z + 1));
        }

        /// <summary>Unpairs an 32-bit unsigned integer representing the pair into two 16-bit unsigned integers with regard to their initial positions before pairing.</summary>
        /// <param name="pairValue">A 32-bit unsigned integer representing the pair.</param>
        /// <returns>A tuple of 16-bit unsigned integers that were paired.</returns>
        public static (ushort A, ushort B) UnPair(uint pairValue)
        {
            var sqrt = IntegerSqrt(pairValue);
            var remainder = pairValue - sqrt * sqrt;
            return remainder < sqrt
                ? ((ushort) remainder, (ushort) sqrt)
                : ((ushort) sqrt, (ushort) (remainder - sqrt));
        }

        /// <summary>Unpairs an 32-bit signed integer representing the pair into two 16-bit signed integers with regard to their initial positions before pairing.</summary>
        /// <param name="pairValue">A 32-bit signed integer representing the pair.</param>
        /// <returns>A tuple of 16-bit signed integers that were paired.</returns>
        /// <remarks>Due to some imperfections of the negative pairing algorithm, this method could be rather slow.</remarks>
        public static (short A, short B) UnPair(int pairValue)
        {
            var z = pairValue >= 0 ? pairValue : -pairValue - 1;
            var Z = 2 * (uint) z;

            var uRes = UnPair(Z);
            var firstPossibleResult = ((short A, short B)) GetSignedTuple(uRes);

            return Pair(firstPossibleResult.A, firstPossibleResult.B) == pairValue
                ? firstPossibleResult
                : ((short A, short B)) GetSignedTuple(UnPair(Z + 1));
        }

        /// <summary>
        /// Unpairs an 64-bit unsigned integer representing the pair into two <see cref="MBGUID" /> objects with regard to their initial positions before pairing.</summary>
        /// <param name="pairValue">A 64-bit unsigned integer representing the pair.</param>
        /// <returns>A tuple of <see cref="MBGUID" /> objects that were paired.</returns>
        public static (MBGUID A, MBGUID B) UnPairMBGUID(ulong pairValue)
        {
            var (a, b) = UnPair(pairValue);
            return (new MBGUID(a), new MBGUID(b));
        }

        private static ulong IntegerSqrt(ulong a)
        {
            var min = (ulong) 0;
            var max = ((ulong) 1) << 32;
            while (true)
            {
                if (max <= 1 + min)
                {
                    return min;
                }

                var sqt = min + (max - min) / 2;
                var sq = sqt * sqt;

                if (sq == a)
                {
                    return sqt;
                }

                if (sq > a)
                {
                    max = sqt;
                }
                else
                {
                    min = sqt;
                }
            }
        }

        private static uint IntegerSqrt(uint a)
        {
            var min = (uint) 0;
            var max = ((uint) 1) << 16;
            while (true)
            {
                if (max <= 1 + min)
                {
                    return min;
                }

                var sqt = min + (max - min) / 2;
                var sq = sqt * sqt;

                if (sq == a)
                {
                    return sqt;
                }

                if (sq > a)
                {
                    max = sqt;
                }
                else
                {
                    min = sqt;
                }
            }
        }

        private static (int A, int B) GetSignedTuple((uint A, uint B) unsignedTuple)
        {
            return (
                A: unsignedTuple.A % 2 == 0
                    ? (int) unsignedTuple.A / 2
                    : (int) ((unsignedTuple.A + 1) / -2),
                B: unsignedTuple.B % 2 == 0
                    ? (int) unsignedTuple.B / 2
                    : (int) ((unsignedTuple.B + 1) / -2));
        }
    }
}