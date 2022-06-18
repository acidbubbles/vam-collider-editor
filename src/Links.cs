using System.Collections.Generic;

public class Links
{
    public struct Link
    {
        public string a, b;

        public Link(string a, string b)
        {
            this.a = a;
            this.b = b;
        }
    };

    static private List<Link> list = null;

    static public List<Link> GetList()
    {
        if (list == null)
        {
            list = new List<Link>();
            Add();
        }

        return list;
    }

    static private void Add()
    {
        AddHead();
        AddArms();
        AddHands();
        AddChest();
    }

    static private void Add(string a, string b)
    {
        list.Add(new Link(a, b));
    }

    static private void AddHead()
    {
        // face hard
        for (int i = 1; i <= 17; ++i)
        {
            Add(
                $"AutoColliders.AutoCollidersFaceHardLeft.AutoColliderAutoCollidersFaceHardLeft{i}",
                $"AutoColliders.AutoCollidersFaceHardRight.AutoColliderAutoCollidersFaceHardRight{i}");
        }

        // lower jaw
        Add(
            "lowerJaw.lowerJawStandardColliders._ColliderL1bl",
            "lowerJaw.lowerJawStandardColliders._ColliderL1br");

        for (int i = 1; i <= 4; ++i)
        {
            Add(
               $"lowerJaw.lowerJawStandardColliders._ColliderL{i}l",
               $"lowerJaw.lowerJawStandardColliders._ColliderL{i}r");
        }

        // lips
        Add(
          "lowerJaw.lowerJawStandardColliders._ColliderLipL",
          "lowerJaw.lowerJawStandardColliders._ColliderLipR");

        // neck
        for (int i = 1; i <= 8; ++i)
        {
            Add(
               $"neck.StandardColliders._Collider{i}l",
               $"neck.StandardColliders._Collider{i}r");
        }

        Add(
          "neck.StandardColliders._ColliderBL",
          "neck.StandardColliders._ColliderBR");
    }

    static private void AddArms()
    {
        // arms
        Add(
            "lCollar.lShldr.lForeArm",
            "rCollar.rShldr.rForeArm");

        for (int i = 1; i <= 3; ++i)
        {
            Add(
                $"lShldr.lForeArm._Collider{i}",
                $"rShldr.rForeArm._Collider{i}");
        }

        // shoulders
        Add(
            "chest.lCollar.lShldr",
            "chest.rCollar.rShldr");


        for (int i = 1; i <= 2; ++i)
        {
            Add(
                $"lShldr.StandardColliderslShldr._Collider{i}",
                $"rShldr.StandardCollidersrShldr._Collider{i}");
        }
    }

    static private void AddHands()
    {
        for (int i = 1; i <= 3; ++i)
        {
            Add(
                $"lHand.lCarpal1._Collider{i}",
                $"rHand.rCarpal1._Collider{i}");
        }

        for (int i = 1; i <= 3; ++i)
        {
            Add(
                $"lHand.lCarpal2._Collider{i}",
                $"rHand.rCarpal2._Collider{i}");
        }

        Add(
            "lForeArm.lHand._Collider",
            "rForeArm.rHand._Collider");

        // index
        Add(
            "lCarpal1.lIndex1._Collider",
            "rCarpal1.rIndex1._Collider");

        Add(
            "lIndex1.lIndex2._Collider",
            "rIndex1.rIndex2._Collider");

        Add(
            "lIndex2.lIndex3._Collider",
            "rIndex2.rIndex3._Collider");

        // mid
        Add(
            "lCarpal1.lMid1._Collider",
            "rCarpal1.rMid1._Collider");

        Add(
            "lMid1.lMid2._Collider",
            "rMid1.rMid2._Collider");

        Add(
            "lMid2.lMid3._Collider",
            "rMid2.rMid3._Collider");

        // pinky
        Add(
            "lCarpal2.lPinky1._Collider",
            "rCarpal2.rPinky1._Collider");

        Add(
            "lPinky1.lPinky2._Collider",
            "rPinky1.rPinky2._Collider");

        Add(
            "lPinky2.lPinky3._Collider",
            "rPinky2.rPinky3._Collider");

        // ring
        Add(
            "lCarpal2.lRing1._Collider",
            "rCarpal2.rRing1._Collider");

        Add(
            "lRing1.lRing2._Collider",
            "rRing1.rRing2._Collider");

        Add(
            "lRing2.lRing3._Collider",
            "rRing2.rRing3._Collider");

        // thumb
        Add(
            "lHand.lThumb1._Collider",
            "rHand.rThumb1._Collider");

        Add(
            "lThumb1.lThumb2._Collider",
            "rThumb1.rThumb2._Collider");

        Add(
            "lThumb2.lThumb3._Collider",
            "rThumb2.rThumb3._Collider");
    }

    static private void AddChest()
    {
        Add(
            "chest.FemaleAutoColliderschest.AutoColliderFemaleAutoColliderschest6 (1)",
            "chest.FemaleAutoColliderschest.AutoColliderFemaleAutoColliderschest6 (2)");

        Add(
            "chest.FemaleAutoColliderschest.AutoColliderFemaleAutoColliderschest6 (3)",
            "chest.FemaleAutoColliderschest.AutoColliderFemaleAutoColliderschest6 (4)");

        Add(
            "chest.FemaleAutoColliderschest.AutoColliderFemaleAutoColliderschest6 (5)",
            "chest.FemaleAutoColliderschest.AutoColliderFemaleAutoColliderschest6 (6)");

        for (int i = 1; i <= 3; ++i)
        {
            Add(
                $"chest.FemaleAutoColliderschest.AutoColliderFemaleAutoColliderschestRibL{i}",
                $"chest.FemaleAutoColliderschest.AutoColliderFemaleAutoColliderschestRibR{i}");
        }
    }
}
