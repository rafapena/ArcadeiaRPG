using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;


public class SpriteProperties : MonoBehaviour
{
    [SerializeField]
    private Transform MainSprite;
    public Animator Animation;

    private Dictionary<string, SpriteComponent> SpriteMap;
    private const string SPRITES_NAME = "Sprite";
    private const string PROPERTIES_NAME = "SpriteProperties";
    private const int SPRITE_LAYER_DISTANCE = 100;

    public Transform PropertiesList { get; private set; }

    public Transform SpritesList { get; private set; }
    
    public Transform BaseHitBox { get; private set; }
    
    public ActionHitBox ActionHitBox { get; private set; }
    
    public ScopeHitBox ScopeHitBox { get; private set; }
    
    public Transform ApproachPoints { get; private set; }
    
    public Transform StateEffects { get; private set; }

    public Transform ActionEffects { get; private set; }

    public GameObject TurnIndicator { get; private set; }

    public GameObject SpeechBubble { get; private set; }

    public struct SpriteComponent
    {
        public GameObject Object;
        public SpriteResolver Resolver;
        public int MissIndex;

        public void SetMissIndex(int index) => MissIndex = index;
        public bool IgnoreOnMiss => MissIndex == 0;
        public bool SelectDefaultOnMiss => MissIndex == 1;
        public bool EnableOrDisable => MissIndex == 2;
    }

    private void Awake()
    {
        try
        {
            PropertiesList = transform.GetChild(0).Find(PROPERTIES_NAME).GetComponent<Transform>();
            SpritesList = transform.GetChild(0).Find(SPRITES_NAME).GetComponent<Transform>();
            BaseHitBox = PropertiesList.GetChild(0).GetComponent<Transform>();
            ActionHitBox = PropertiesList.GetChild(1).GetComponent<ActionHitBox>();
            ScopeHitBox = PropertiesList.GetChild(2).GetComponent<ScopeHitBox>();
            ApproachPoints = PropertiesList.GetChild(3).GetComponent<Transform>();
            StateEffects = PropertiesList.GetChild(4).GetComponent<Transform>();
            ActionEffects = PropertiesList.GetChild(5).GetComponent<Transform>();
            TurnIndicator = PropertiesList.GetChild(6).gameObject;
            SpeechBubble = PropertiesList.GetChild(7).gameObject;
        }
        catch
        {
            Debug.LogError("Failed to initialize sprite properties");
            return;
        }

        SpriteMap = new Dictionary<string, SpriteComponent>();
        foreach (Transform t in SpritesList)
        {
            SpriteComponent sc;
            sc.Object = t.gameObject;
            sc.Resolver = t.GetComponent<SpriteResolver>();
            if (sc.Resolver)
            {
                sc.MissIndex = 0;
                SpriteMap.Add(t.name, sc);
            }
        }
        SetMissIndex(1, "Hair_Front");
        SetMissIndex(1, "Hair_Back");
        SetMissIndex(1, "Blush");
        SetMissIndex(2, "R_Arm_Item");
        SetMissIndex(2, "L_Arm_Item");
        SetMissIndex(2, "Hat");
        SetMissIndex(2, "Glasses");
        SetMissIndex(2, "Neckwear");
        SetMissIndex(2, "Back_Accessory");
    }

    private void SetMissIndex(int index, string label)
    {
        if (SpriteMap.ContainsKey(label)) SpriteMap[label].SetMissIndex(index);
    }


    public bool UnEmote() => Emote(SpriteMaster.Expressions.Default.ToString());

    public bool ChangeEyes(string label) => ChangeSpriteComponents(label, new string[] { "R_Eye", "L_Eye" });

    public bool RightArmHold(string label) => ChangeSpriteComponents(label, new string[] { "R_Arm_Item", "Weapon" });

    public bool LeftArmHold(string label) => ChangeSpriteComponents(label, new string[] { "L_Arm_Item" });

    public bool Emote(string label) => ChangeSpriteComponents(label, new string[] { "Brows", "R_Eye", "L_Eye", "Facial_Hair", "Facial_Feature", "Blush", "Nose", "Mouth" });

    public bool WearAttire(string label) => ChangeSpriteComponents(label, new string[] { "R_Arm", "L_Arm_Item", "Hat", "Glasses", "Neckwear", "Torso", "L_Arm", "L_Leg", "R_Leg", "Back_Accessory", "Hair_Back" });


    private bool ChangeSpriteComponents(string label, string[] components)
    {
        if (SpriteMap == null) return false;
        bool swappedContent = false;
        foreach (string s in components)
        {
            if (!SpriteMap.ContainsKey(s)) continue;
            var c = SpriteMap[s];
            c.Resolver.SetCategoryAndLabel(s, label);
            if (c.Resolver.GetLabel() != null)
            {
                swappedContent = true;
                c.Object.SetActive(true);
            }
            else if (c.SelectDefaultOnMiss) c.Resolver.SetCategoryAndLabel(s, SpriteMaster.DEFAULT_LABEL);
            else if (!c.IgnoreOnMiss) c.Object.SetActive(false);
        }
        return swappedContent;
    }


    public void MoveForwardInOrder(int indexAdd)
    {
        foreach (Transform s in SpritesList)
        {
            var sprite = s.gameObject.GetComponent<SpriteRenderer>();
            if (sprite) sprite.sortingOrder += SPRITE_LAYER_DISTANCE * indexAdd;
        }
    }
}