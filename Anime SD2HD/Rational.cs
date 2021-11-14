using System;
using System.Text.RegularExpressions;

namespace AnimeSD2HD
{
    internal class Rational
    {
        private static readonly Regex rgxRational = new(@"^(?<NUM>\d+)[:/](?<DEN>\d+)$");

        public int Numerator { get; }
        public int Denominator { get; }

        public Rational(int numerator, int denominator)
        {
            if (denominator == 0)
            {
                throw new ArgumentException("Invalid denominator, must not be 0!", nameof(denominator));
            }
            Numerator = numerator;
            Denominator = denominator;
        }

        public static Rational Parse(string format)
        {
            var match = rgxRational.Match(format);
            if (match.Success)
            {
                var numerator = Convert.ToInt32(match.Groups["NUM"].Value);
                var denominator = Convert.ToInt32(match.Groups["DEN"].Value);
                return new Rational(numerator, denominator);
            }
            throw new ArgumentException("Invalid format!", nameof(format));
        }

        private double AsFloat() => (double)Numerator / Denominator;
        public string AsString(char separator = '/') => $"{Numerator}{separator}{Denominator}";
        
        public static double operator -(Rational self, Rational other) => self.AsFloat() - other.AsFloat();
        public static double operator *(Rational self, double other) => other * self.AsFloat();
        public static double operator *(double other, Rational self) => other * self.AsFloat();
        public static double operator *(Rational self, int other) => other * self.AsFloat();
        public static double operator *(int other, Rational self) => other * self.AsFloat();
    }
}