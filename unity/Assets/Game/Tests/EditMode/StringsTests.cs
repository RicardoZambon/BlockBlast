#nullable enable
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace BlockPuzzle.Game.Tests
{
    public class StringsTests
    {
        [Test]
        public void Every_key_has_english_text()
        {
            Assert.Greater(Strings.Keys.Count(), 30);
            foreach (string key in Strings.Keys)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(Strings.Get(key)), key);
                Assert.AreNotEqual(key, Strings.Get(key), key);
            }
        }

        [Test]
        public void A_missing_key_shows_itself() => Assert.AreEqual("no.such.key", Strings.Get("no.such.key"));

        [TearDown]
        public void BackToEnglish() => Strings.Current = Language.English;

        static int Placeholders(string text, int index) => text.Split(new[] { "{" + index + "}" }, System.StringSplitOptions.None).Length - 1;

        [Test]
        public void Every_language_has_every_key_with_the_same_placeholders()
        {
            foreach (var language in Strings.Languages)
            {
                foreach (string key in Strings.Keys)
                {
                    Assert.IsTrue(Strings.Has(language, key), language + " has no " + key);
                    Strings.Current = Language.English;
                    string english = Strings.Get(key);
                    Strings.Current = language;
                    string translated = Strings.Get(key);
                    for (int i = 0; i < 3; i++)
                    {
                        Assert.AreEqual(Placeholders(english, i), Placeholders(translated, i), language + " " + key + " {" + i + "}");
                    }
                }
            }
        }

        [TestCase(SystemLanguage.Portuguese, Language.BrazilianPortuguese)]
        [TestCase(SystemLanguage.Spanish, Language.LatinAmericanSpanish)]
        [TestCase(SystemLanguage.Indonesian, Language.Indonesian)]
        [TestCase(SystemLanguage.English, Language.English)]
        [TestCase(SystemLanguage.German, Language.English)]
        public void The_phone_language_picks_the_table(SystemLanguage phone, Language expected) =>
            Assert.AreEqual(expected, Strings.FromSystem(phone));

        [Test]
        public void Each_language_reads_its_own_table()
        {
            Strings.Current = Language.BrazilianPortuguese;
            Assert.AreEqual("Jogar", Strings.Get("home.play"));
            Strings.Current = Language.LatinAmericanSpanish;
            Assert.AreEqual("Jugar", Strings.Get("home.play"));
            Strings.Current = Language.Indonesian;
            Assert.AreEqual("Main", Strings.Get("home.play"));
            Assert.AreEqual("Terbaik 1240", Strings.Format("hud.best", 1240));
        }

        [Test]
        public void Numbers_are_written_the_same_way_everywhere()
        {
            Assert.AreEqual("Best 12345", Strings.Format("hud.best", 12345));
            Assert.AreEqual("Score 1240\nBest 2000", Strings.Format("game_over.scores", 1240, 2000));
        }
    }
}
