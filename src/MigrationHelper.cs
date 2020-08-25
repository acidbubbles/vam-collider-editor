using System.Collections.Generic;

public static class MigrationHelper
{
    // Thanks to via5 for the migration code: https://github.com/acidbubbles/vam-collider-editor/issues/14
    public static string Migrate(string old)
    {
        var map = new Dictionary<string, string>();

        for (int i=1; i<=7; ++i)
        {
            int to = i;

            map.Add(
                $"AutoCollider:hip.pelvis.rThigh.FemaleAutoCollidersrThigh.AutoColliderrThighUP{i}.AutoColliderrThighUP{i}",
                $"AutoCollider:hip.pelvis.rThigh.FemaleAutoCollidersrThigh.AutoColliderFemaleAutoCollidersrThigh{to}.AutoColliderFemaleAutoCollidersrThigh{to}");

            map.Add(
                $"AutoCollider:hip.pelvis.lThigh.FemaleAutoColliderslThigh.AutoColliderlThighUP{i}.AutoColliderlThighUP{i}",
                $"AutoCollider:hip.pelvis.lThigh.FemaleAutoColliderslThigh.AutoColliderFemaleAutoColliderslThigh{to}.AutoColliderFemaleAutoColliderslThigh{to}");
        }

        for (int i=1; i<=14; ++i)
        {
            int to = i + 7;

            map.Add(
                $"AutoCollider:hip.pelvis.rThigh.FemaleAutoCollidersrThigh.AutoColliderrThigh{i}.AutoColliderrThigh{i}",
                $"AutoCollider:hip.pelvis.rThigh.FemaleAutoCollidersrThigh.AutoColliderFemaleAutoCollidersrThigh{to}.AutoColliderFemaleAutoCollidersrThigh{to}");

            map.Add(
                $"AutoCollider:hip.pelvis.lThigh.FemaleAutoColliderslThigh.AutoColliderlThigh{i}.AutoColliderlThigh{i}",
                $"AutoCollider:hip.pelvis.lThigh.FemaleAutoColliderslThigh.AutoColliderFemaleAutoColliderslThigh{to}.AutoColliderFemaleAutoColliderslThigh{to}");
        };

        for (int i=1; i<=2; ++i)
        {
            int to = i + 21;

            map.Add(
                $"AutoCollider:hip.pelvis.rThigh.FemaleAutoCollidersrThigh.AutoColliderrThighKnee{i}.AutoColliderrThighKnee{i}",
                $"AutoCollider:hip.pelvis.rThigh.FemaleAutoCollidersrThigh.AutoColliderFemaleAutoCollidersrThigh{to}.AutoColliderFemaleAutoCollidersrThigh{to}");

            map.Add(
                $"AutoCollider:hip.pelvis.lThigh.FemaleAutoColliderslThigh.AutoColliderlThighKnee{i}.AutoColliderlThighKnee{i}",
                $"AutoCollider:hip.pelvis.lThigh.FemaleAutoColliderslThigh.AutoColliderFemaleAutoColliderslThigh{to}.AutoColliderFemaleAutoColliderslThigh{to}");
        }


        map.Add(
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2.AutoColliderFemaleAutoCollidersabdomen21.AutoColliderFemaleAutoCollidersabdomen21",
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2_.AutoColliderFemaleAutoCollidersabdomen2_1.AutoColliderFemaleAutoCollidersabdomen2_1");

        map.Add(
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2.AutoColliderFemaleAutoCollidersabdomen22.AutoColliderFemaleAutoCollidersabdomen22",
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2_.AutoColliderFemaleAutoCollidersabdomen2_2.AutoColliderFemaleAutoCollidersabdomen2_2");

        map.Add(
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2.AutoColliderFemaleAutoCollidersabdomen21c.AutoColliderFemaleAutoCollidersabdomen21c",
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2_.AutoColliderFemaleAutoCollidersabdomen2_3.AutoColliderFemaleAutoCollidersabdomen2_3");

        map.Add(
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2.AutoColliderFemaleAutoCollidersabdomen23.AutoColliderFemaleAutoCollidersabdomen23",
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2_.AutoColliderFemaleAutoCollidersabdomen2_4.AutoColliderFemaleAutoCollidersabdomen2_4");

        map.Add(
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2.AutoColliderFemaleAutoCollidersabdomen21b.AutoColliderFemaleAutoCollidersabdomen21b",
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2_.AutoColliderFemaleAutoCollidersabdomen2_5.AutoColliderFemaleAutoCollidersabdomen2_5");

        map.Add(
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2.AutoColliderFemaleAutoCollidersabdomen24.AutoColliderFemaleAutoCollidersabdomen24",
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2_.AutoColliderFemaleAutoCollidersabdomen2_6.AutoColliderFemaleAutoCollidersabdomen2_6");

        map.Add(
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2.AutoColliderFemaleAutoCollidersabdomen27.AutoColliderFemaleAutoCollidersabdomen27",
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2_.AutoColliderFemaleAutoCollidersabdomen2_7.AutoColliderFemaleAutoCollidersabdomen2_7");

        map.Add(
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2.AutoColliderFemaleAutoCollidersabdomen26.AutoColliderFemaleAutoCollidersabdomen26",
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2_.AutoColliderFemaleAutoCollidersabdomen2_8.AutoColliderFemaleAutoCollidersabdomen2_8");

        map.Add(
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2.AutoColliderFemaleAutoCollidersabdomen28.AutoColliderFemaleAutoCollidersabdomen28",
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2_.AutoColliderFemaleAutoCollidersabdomen2_9.AutoColliderFemaleAutoCollidersabdomen2_9");

        map.Add(
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2.AutoColliderFemaleAutoCollidersabdomen29.AutoColliderFemaleAutoCollidersabdomen29",
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2_.AutoColliderFemaleAutoCollidersabdomen2_10.AutoColliderFemaleAutoCollidersabdomen2_10");

        map.Add(
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2.AutoColliderFemaleAutoCollidersabdomen210.AutoColliderFemaleAutoCollidersabdomen210",
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2_.AutoColliderFemaleAutoCollidersabdomen2_11.AutoColliderFemaleAutoCollidersabdomen2_11");

        map.Add(
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2.AutoColliderFemaleAutoCollidersabdomen25.AutoColliderFemaleAutoCollidersabdomen25",
            "AutoCollider:hip.abdomen.abdomen2.FemaleAutoCollidersabdomen2_.AutoColliderFemaleAutoCollidersabdomen2_12.AutoColliderFemaleAutoCollidersabdomen2_12");


        string s;
        if (map.TryGetValue(old, out s))
            return s;

        return old;
    }
}
