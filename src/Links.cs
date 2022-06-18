﻿using System.Collections.Generic;

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


    protected List<Link> links_ = new List<Link>();
    protected Dictionary<string, string> mapA_ = new Dictionary<string, string>();
    protected Dictionary<string, string> mapB_ = new Dictionary<string, string>();

    protected void Add(string a, string b)
    {
        links_.Add(new Link(a, b));
    }

    public string Find(string name)
    {
        string s;

        if (mapA_.TryGetValue(name, out s))
            return s;

        if (mapB_.TryGetValue(name, out s))
            return s;

        return null;
    }

    private void CreateDictionaries()
    {
        foreach (var link in links_)
        {
            mapA_.Add(link.a, link.b);
            mapA_.Add(link.b, link.a);
        }
    }



    private static Links maleLinks_ = null;
    private static Links femaleLinks_ = null;

    public static Links Get(Atom atom)
    {
        var c = atom?.GetComponentInChildren<DAZCharacter>();

        if (c != null && c.isMale)
        {
            if (maleLinks_ == null)
            {
                maleLinks_ = new MaleLinks();
                maleLinks_.CreateDictionaries();
            }

            return maleLinks_;
        }
        else
        {
            if (femaleLinks_ == null)
            {
                femaleLinks_ = new FemaleLinks();
                femaleLinks_.CreateDictionaries();
            }

            return femaleLinks_;
        }
    }
}

public class FemaleLinks : Links
{
    public FemaleLinks()
    {
        AddHead();
        AddArms();
        AddHands();
        AddChest();
        AddBreasts();
        AddAbdomen();
        AddHips();
        AddGlutes();
        AddG();
        AddLegs();
        AddFeet();
    }

    private void AddHead()
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

    private void AddArms()
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

    private void AddHands()
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

    private void AddChest()
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

    private void AddBreasts()
    {
        Add(
            "lPectoral.FemaleAutoColliderslNipple.AutoColliderFemaleAutoColliderslNipple1",
            "rPectoral.FemaleAutoCollidersrNipple.AutoColliderFemaleAutoCollidersrNipple1");

        Add(
            "lPectoral.FemaleAutoColliderslNipple.AutoColliderFemaleAutoColliderslNippleGPU",
            "rPectoral.FemaleAutoCollidersrNipple.AutoColliderFemaleAutoCollidersrNippleGPU");

        for (int i = 1; i <= 5; ++i)
        {
            Add(
                $"lPectoral.FemaleAutoColliderslPectoral.AutoColliderFemaleAutoColliderslPectoral{i}",
                $"rPectoral.FemaleAutoCollidersrPectoral.AutoColliderFemaleAutoCollidersrPectoral{i}");
        }

        Add(
            "chest.lPectoral._Collider (1)",
            "chest.rPectoral._Collider (1)");
    }

    private void AddAbdomen()
    {
        Add(
            "abdomen2.FemaleAutoCollidersabdomen2_.AutoColliderFemaleAutoCollidersabdomen2_7",
            "abdomen2.FemaleAutoCollidersabdomen2_.AutoColliderFemaleAutoCollidersabdomen2_8");

        Add(
            "abdomen.FemaleAutoCollidersabdomen.AutoColliderFemaleAutoCollidersabdomen5",
            "abdomen.FemaleAutoCollidersabdomen.AutoColliderFemaleAutoCollidersabdomen6");

        for (int i = 1; i <= 5; ++i)
        {
            Add(
                $"abdomen.FemaleAutoCollidersabdomen.AutoColliderFemaleAutoCollidersabdomen{6+i}",
                $"abdomen.FemaleAutoCollidersabdomen.AutoColliderFemaleAutoCollidersabdomen{6+i+5}");
        }

        Add(
            "abdomen.FemaleAutoCollidersabdomen.AutoColliderFemaleAutoCollidersabdomen21",
            "abdomen.FemaleAutoCollidersabdomen.AutoColliderFemaleAutoCollidersabdomen22");

        Add(
            "abdomen.FemaleAutoCollidersabdomen.AutoColliderFemaleAutoCollidersabdomen23",
            "abdomen.FemaleAutoCollidersabdomen.AutoColliderFemaleAutoCollidersabdomen24");
    }

    private void AddHips()
    {
        for (int i = 1; i <= 8; ++i)
        {
            Add(
                $"pelvis.FemaleAutoColliderspelvis.AutoColliderpelvisFL{i}",
                $"pelvis.FemaleAutoColliderspelvis.AutoColliderpelvisFR{i}");
        }

        Add(
            $"pelvis.FemaleAutoColliderspelvis.AutoColliderpelvisFL1b",
            $"pelvis.FemaleAutoColliderspelvis.AutoColliderpelvisFR1b");

        Add(
            $"pelvis.FemaleAutoColliderspelvis.AutoColliderpelvisFL1c",
            $"pelvis.FemaleAutoColliderspelvis.AutoColliderpelvisFR1c");

        for (int i = 1; i <= 6; ++i)
        {
            Add(
                $"pelvis.FemaleAutoColliderspelvis.AutoColliderpelvisL{i}",
                $"pelvis.FemaleAutoColliderspelvis.AutoColliderpelvisR{i}");
        }

        Add(
            "hip.pelvis.HardColliderL",
            "hip.pelvis.HardColliderR");
    }

    private void AddGlutes()
    {
        Add(
            "LGlute.FemaleAutoCollidersLGluteAlt.AutoColliderFemaleAutoCollidersLGlute1",
            "RGlute.FemaleAutoCollidersRGluteAlt.AutoColliderFemaleAutoCollidersRGlute1");

        for (int i = 1; i <= 7; ++i)
        {
            Add(
                $"LGlute.FemaleAutoCollidersLGluteAlt.AutoColliderFemaleAutoCollidersLGlute1 ({i})",
                $"RGlute.FemaleAutoCollidersRGluteAlt.AutoColliderFemaleAutoCollidersRGlute1 ({i})");
        }

        Add(
            "pelvis.LGlute.HardCollider",
            "pelvis.RGlute.HardCollider");
    }

    private void AddG()
    {
        Add(
            "hip.pelvis._JointAl",
            "hip.pelvis._JointAr");

        Add(
            "hip.pelvis._JointGl",
            "hip.pelvis._JointGr");

        for (int i = 1; i <= 4; ++i)
        {
            Add(
                $"pelvis._JointGl.Collider{i}",
                $"pelvis._JointGr.Collider{i}");
        }
    }

    private void AddLegs()
    {
        for (int i = 1; i <= 16; ++i)
        {
            Add(
                $"lShin.FemaleAutoColliderslShin.AutoColliderFemaleAutoColliderslShin{i}",
                $"rShin.FemaleAutoCollidersrShin.AutoColliderrShin{i}");

        }

        for (int i = 1; i <= 23; ++i)
        {
            Add(
                $"lThigh.FemaleAutoColliderslThigh.AutoColliderFemaleAutoColliderslThigh{i}",
                $"rThigh.FemaleAutoCollidersrThigh.AutoColliderFemaleAutoCollidersrThigh{i}");
        }

        Add(
            "lThigh.lShin.HardCollider",
            "rThigh.rShin.HardCollider");

        Add(
            "pelvis.lThigh.HardCollider",
            "pelvis.rThigh.HardCollider");
    }

    private void AddFeet()
    {
        Add(
            "lToe.lBigToe._Collider",
            "rToe.rBigToe._Collider");

        for (int i = 1; i <= 9; ++i)
        {
            Add(
                $"lShin.lFoot._Collider{i}",
                $"rShin.rFoot._Collider{i}");
        }

        for (int i = 1; i <= 4; ++i)
        {
            Add(
                $"lToe.lSmallToe{i}._Collider",
                $"rToe.rSmallToe{i}._Collider");
        }

        Add(
            "lFoot.lToe._Collider",
            "rFoot.rToe._Collider");
    }
}


public class MaleLinks : Links
{
    public MaleLinks()
    {
        // todo
    }
}
