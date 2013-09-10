using System;

namespace Common {
    /// <summary>
    /// A singleton that makes a single Random, and uses it for all program wide requests for random numbers.
    /// (Because making multiple instances of Random is stupid and problematic)
    /// </summary>
    public static class Rng {
        private static readonly Random Random;
        static Rng() {
            Random = new Random();
        }

        // Summary:
        //     Returns a nonnegative random number.
        //
        // Returns:
        //     A 32-bit signed integer greater than or equal to zero and less than System.Int32.MaxValue.
        public static int Next() {
            return Random.Next();
        }
        //
        // Summary:
        //     Returns a nonnegative random number less than the specified maximum.
        //
        // Parameters:
        //   maxValue:
        //     The exclusive upper bound of the random number to be generated. maxValue
        //     must be greater than or equal to zero.
        //
        // Returns:
        //     A 32-bit signed integer greater than or equal to zero, and less than maxValue;
        //     that is, the range of return values ordinarily includes zero but not maxValue.
        //     However, if maxValue equals zero, maxValue is returned.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     maxValue is less than zero.
        public static int Next(int maxValue) {
            return Random.Next(maxValue);
        }
        //
        // Summary:
        //     Returns a random number within a specified range.
        //
        // Parameters:
        //   minValue:
        //     The inclusive lower bound of the random number returned.
        //
        //   maxValue:
        //     The exclusive upper bound of the random number returned. maxValue must be
        //     greater than or equal to minValue.
        //
        // Returns:
        //     A 32-bit signed integer greater than or equal to minValue and less than maxValue;
        //     that is, the range of return values includes minValue but not maxValue. If
        //     minValue equals maxValue, minValue is returned.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     minValue is greater than maxValue.
        public static int Next(int minValue, int maxValue) {
            return Random.Next(minValue, maxValue);
        }
        //
        // Summary:
        //     Fills the elements of a specified array of bytes with random numbers.
        //
        // Parameters:
        //   buffer:
        //     An array of bytes to contain random numbers.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     buffer is null.
        public static void NextBytes(byte[] buffer) {
            Random.NextBytes(buffer);
        }
        //
        // Summary:
        //     Returns a random number between 0.0 and 1.0.
        //
        // Returns:
        //     A double-precision floating point number greater than or equal to 0.0, and
        //     less than 1.0.
        public static double NextDouble() {
            return Random.NextDouble();
        }
    }
}
