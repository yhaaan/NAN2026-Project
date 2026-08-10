using NUnit.Framework;

namespace NAN2026.Gomoku.Tests
{
    public sealed class PvpNicknameUtilityTests
    {
        [Test]
        public void Normalize_TrimsWhitespace()
        {
            Assert.That(PvpNicknameUtility.Normalize("  바람기사  "), Is.EqualTo("바람기사"));
        }

        [Test]
        public void Normalize_TruncatesLongNickname()
        {
            string nickname = new string('가', PvpNicknameUtility.MaxLength + 5);

            Assert.That(PvpNicknameUtility.Normalize(nickname).Length, Is.EqualTo(PvpNicknameUtility.MaxLength));
        }

        [Test]
        public void Normalize_BlankNicknameCreatesDeterministicRandomDefault()
        {
            int[] values = { 0, 0, 42 };
            int index = 0;

            string result = PvpNicknameUtility.Normalize("   ", (_, _) => values[index++]);

            Assert.That(result, Is.EqualTo("용감한기사42"));
        }
    }
}
