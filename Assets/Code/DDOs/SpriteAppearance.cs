using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[System.Serializable]
public struct SpriteAppearance
{
    public Sprite R_Arm;
    public Sprite R_Arm_Item;
    public Sprite L_Arm_Item;

    public Sprite Brows;
    public Sprite Hat;
    public Sprite Glasses;
    public Sprite Hair_Front;
    public Sprite R_Eye;
    public Sprite L_Eye;
    public Sprite Facial_Hair;
    public Sprite Facial_Feature;
    public Sprite Blush;
    public Sprite Nose;
    public Sprite Mouth;

    public Sprite Neckwear;
    public Sprite Head;
    public Sprite Torso;
    public Sprite L_Arm;
    public Sprite L_Leg;
    public Sprite R_Leg;
    public Sprite Tail;
    public Sprite Back_Accessory;
    public Sprite Hair_Back;

    public Sprite[] SpriteParts;

    public void GenerateSpritesList()
    {
        var list = typeof(SpriteAppearance).GetProperties();
        SpriteParts = new Sprite[list.Length];
        int i = 0;
        foreach (var entry in list)
            SpriteParts[i++] = entry.GetValue(this, null) as Sprite;
    }
}