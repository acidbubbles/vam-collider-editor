using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class Opposites
{
    private static readonly List<KeyValuePair<string, string>> _map = new List<KeyValuePair<string, string>>();

    private class MirrorRegexReplace
    {
        public Regex Regex;
        public string Replacement;
    }

    private static readonly MirrorRegexReplace[] _mirrorRegexes = {
        new MirrorRegexReplace { Regex = new Regex(@"l(?=[A-Z])", RegexOptions.Compiled), Replacement = "r" },
        new MirrorRegexReplace { Regex = new Regex(@"Left", RegexOptions.Compiled), Replacement = "Right" },
        new MirrorRegexReplace { Regex = new Regex(@"(?<!a)l$", RegexOptions.Compiled), Replacement = "r" },
        new MirrorRegexReplace { Regex = new Regex(@"(?<!a)l(?=\.)", RegexOptions.Compiled), Replacement = "r" },
        new MirrorRegexReplace { Regex = new Regex(@"L$", RegexOptions.Compiled), Replacement = "R" },
        new MirrorRegexReplace { Regex = new Regex(@"(?<!_Collider|pelvisB)L(?=[0-9A-Z\.])", RegexOptions.Compiled), Replacement = "R" },
    };

    public static string Find(string name)
    {
        var matched = false;
        var s = name;

        foreach (var entry in _map)
        {
            if (s.EndsWith(entry.Key))
            {
                s = s.Substring(0, s.Length - entry.Key.Length) + entry.Value;
                matched = true;
                break;
            }
        }

        foreach (var r in _mirrorRegexes)
        {
            if (!r.Regex.IsMatch(s)) continue;
            s = r.Regex.Replace(s, r.Replacement);
            matched = true;
        }

        return matched ? s : null;
    }

    static Opposites()
    {
        // ReSharper disable StringLiteralTypo

        // tongue
        Add(
            "tongue03.StandardCollidersTongue03._Collider2._Collider2",
            "tongue03.StandardCollidersTongue03._Collider3._Collider3");
        Add(
            "tongue04.StandardCollidersTongue04._Collider2._Collider2",
            "tongue04.StandardCollidersTongue04._Collider3._Collider3");
        Add(
            "tongue04.StandardCollidersTongue04._Collider4._Collider4",
            "tongue04.StandardCollidersTongue04._Collider5._Collider5");
        Add(
            "tongue05.StandardCollidersTongue05._Collider2._Collider2",
            "tongue05.StandardCollidersTongue05._Collider3._Collider3");
        Add(
            "tongue05.StandardCollidersTongue05._Collider4._Collider4",
            "tongue05.StandardCollidersTongue05._Collider5._Collider5");
        Add(
            "tongueTip.StandardCollidersTongueTip._Collider2._Collider2",
            "tongueTip.StandardCollidersTongueTip._Collider3._Collider3");
        Add(
            "tongueTip.StandardCollidersTongueTip._Collider4._Collider4",
            "tongueTip.StandardCollidersTongueTip._Collider5._Collider5");

        // Chest
        Add(
            "chest.FemaleAutoColliderschest.AutoColliderFemaleAutoColliderschest6 (1)",
            "chest.FemaleAutoColliderschest.AutoColliderFemaleAutoColliderschest6 (2)");
        Add(
            "chest.FemaleAutoColliderschest.AutoColliderFemaleAutoColliderschest6 (3)",
            "chest.FemaleAutoColliderschest.AutoColliderFemaleAutoColliderschest6 (4)");
        Add(
            "chest.FemaleAutoColliderschest.AutoColliderFemaleAutoColliderschest6 (5)",
            "chest.FemaleAutoColliderschest.AutoColliderFemaleAutoColliderschest6 (6)");

        // abdomen
        Add(
            "abdomen2.FemaleAutoCollidersabdomen2_.AutoColliderFemaleAutoCollidersabdomen2_7.AutoColliderFemaleAutoCollidersabdomen2_7",
            "abdomen2.FemaleAutoCollidersabdomen2_.AutoColliderFemaleAutoCollidersabdomen2_8.AutoColliderFemaleAutoCollidersabdomen2_8");
        Add(
            "abdomen.FemaleAutoCollidersabdomen.AutoColliderFemaleAutoCollidersabdomen5.AutoColliderFemaleAutoCollidersabdomen5",
            "abdomen.FemaleAutoCollidersabdomen.AutoColliderFemaleAutoCollidersabdomen6.AutoColliderFemaleAutoCollidersabdomen6");
        for (int i = 1; i <= 5; ++i)
        {
            Add(
                $"abdomen.FemaleAutoCollidersabdomen.AutoColliderFemaleAutoCollidersabdomen.AutoColliderFemaleAutoCollidersabdomen{6+i}",
                $"abdomen.FemaleAutoCollidersabdomen.AutoColliderFemaleAutoCollidersabdomen.AutoColliderFemaleAutoCollidersabdomen{6+i+5}");
        }
        Add(
            "abdomen.FemaleAutoCollidersabdomen.AutoColliderFemaleAutoCollidersabdomen21.AutoColliderFemaleAutoCollidersabdomen21",
            "abdomen.FemaleAutoCollidersabdomen.AutoColliderFemaleAutoCollidersabdomen22.AutoColliderFemaleAutoCollidersabdomen22");
        Add(
            "abdomen.FemaleAutoCollidersabdomen.AutoColliderFemaleAutoCollidersabdomen23.AutoColliderFemaleAutoCollidersabdomen23",
            "abdomen.FemaleAutoCollidersabdomen.AutoColliderFemaleAutoCollidersabdomen24.AutoColliderFemaleAutoCollidersabdomen24");

        // shin
        for (int i = 1; i <= 16; ++i)
        {
            Add(
                $"lShin.FemaleAutoColliderslShin.AutoColliderFemaleAutoColliderslShin{i}.AutoColliderFemaleAutoColliderslShin{i}",
                $"rShin.FemaleAutoCollidersrShin.AutoColliderrShin{i}.AutoColliderrShin{i}");
        }

        // ReSharper restore StringLiteralTypo
    }

    private static void Add(string left, string right)
    {
        _map.Add(new KeyValuePair<string, string>(left, right));
    }
}
