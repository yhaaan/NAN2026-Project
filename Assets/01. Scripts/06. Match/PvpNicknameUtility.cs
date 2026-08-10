using System;

namespace NAN2026.Gomoku
{
    public static class PvpNicknameUtility
    {
        public const int MaxLength = 12;

        private static readonly string[] Adjectives =
        {
            "용감한", "빛나는", "재빠른", "행운의", "고요한", "불타는"
        };

        private static readonly string[] Nouns =
        {
            "기사", "마법사", "고양이", "여우", "수호자", "별"
        };

        public static string Normalize(string nickname, Func<int, int, int> rangeProvider = null)
        {
            string trimmed = nickname?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return CreateRandom(rangeProvider);
            }

            return trimmed.Length <= MaxLength ? trimmed : trimmed.Substring(0, MaxLength);
        }

        public static string CreateRandom(Func<int, int, int> rangeProvider = null)
        {
            rangeProvider ??= UnityEngine.Random.Range;
            string adjective = Adjectives[rangeProvider(0, Adjectives.Length)];
            string noun = Nouns[rangeProvider(0, Nouns.Length)];
            int number = rangeProvider(10, 100);
            return Normalize($"{adjective}{noun}{number}", rangeProvider);
        }
    }
}
