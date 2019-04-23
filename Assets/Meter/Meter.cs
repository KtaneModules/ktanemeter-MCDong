using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Random = System.Random;

public class Meter : MonoBehaviour {
    public KMSelectable[] Buttons;
    public TextMesh[] Lines;
    public KMBombInfo BombInfo;

    bool isActivated = false;
    int[] buttonPressSequence = new int[] {-1,-1,-1,-1,-1,-1};
    Dictionary<string, int> phraseValues = new Dictionary<string, int>();
    Dictionary<string, int> selectedPhrases = new Dictionary<string, int>();
    private int moduleId = 0;
    static int moduleIdCounter = 0;
    private int[] keyMeter;
    // Use this for initialization
    void Start ()
    {
        moduleId = moduleIdCounter++;
        phraseValues.Add("molossus", 1);
        phraseValues.Add("spondee", 2);
        phraseValues.Add("bacchius", 3);
        phraseValues.Add("amphibrach", 4);
        phraseValues.Add("dibrach", 5);
        phraseValues.Add("antibacchius", 6);
        phraseValues.Add("dactyl", 7);
        phraseValues.Add("iamb", 8);
        phraseValues.Add("cretic", 9);
        phraseValues.Add("trochee", 10);
        phraseValues.Add("tribrach", 11);
        Init();

        GetComponent<KMBombModule>().OnActivate += ActivateModule;
    }


    void Init()
    {
        SelectPhrases(Lines.Length);

        for (int i = 0; i < Buttons.Length; i++)
        {
            int j = i;
            Buttons[i].OnInteract += delegate () { OnPress(j); return false; };
        }

    }

    void OnPress(int buttonIndex)
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();

        if (!isActivated)
        {
            Debug.LogFormat("[Meter #{0}] Pressed button before module has been activated!", moduleId);
            GetComponent<KMBombModule>().HandleStrike();
        }
        else
        {
            Debug.LogFormat("[Meter #{0}] Pressed {1} button", moduleId, buttonIndex);
            int kp = Array.IndexOf(buttonPressSequence, -1);
            buttonPressSequence[kp] = buttonIndex;
            int km = Array.IndexOf(keyMeter, -1);
            for (int i = 0; i <= kp; i++)
            {
                if (buttonPressSequence[i] != keyMeter[i])
                {
                    buttonPressSequence = new int[] {-1,-1,-1,-1,-1,-1};
                    GetComponent<KMBombModule>().HandleStrike();
                    return;
                }
            }
            if (kp+1 == km || kp+1 == keyMeter.Length)
                GetComponent<KMBombModule>().HandlePass();
        }
    }

    void ActivateModule()
    {

        UpdateDisplays();
        GetKeyMeter(GetFootModifier());
        isActivated = true;
    }

    int GetFootModifier()
    {
        string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string digits = "0123456789";
        string serial = "";
        List<string> responses = GetComponent<KMBombInfo>().QueryWidgets(KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER, null);
        foreach (string response in responses)
        {
            Dictionary<string, string> responseDict =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            serial = responseDict["serial"];
        }
        
        int footMod = 0;
        int numLetters = 0;
        int numDigits = 0;
        for (int i = 0; i < serial.Length && (numLetters < 2 || numDigits < 1); i++)
        {
            string cur = serial.Substring(i, 1);
            if (alphabet.Contains(cur))
            {
                numLetters++;
                int nato = GetNatoValue(cur.ToUpper());
                Debug.LogFormat("[Meter #{0}] Nato value of {1} is {2}. Added to foot modifier", moduleId, cur.ToUpper(), nato);
                footMod += nato;
            }

            if (digits.Contains(cur) && numDigits == 0)
            {
                numDigits++;
                Debug.LogFormat("[Meter #{0}] Serial number digit {1} added to foot modifier", moduleId, cur);
                footMod += Int32.Parse(cur);
            }

        }
        Debug.LogFormat("[Meter #{0}] Foot Modifier: {1}", moduleId, footMod);
        return footMod;
    }

    int GetNatoValue(string str)
    {
        string trochee = "ABCDEFKLOPTVWYZ";
        string iamb = "HQ";
        string single = "GM";
        string dactyl = "IRU";
        string cretic = "J";
        string amphibrach = "NS";
        string spondee = "X";


        if (single.Contains(str))
            return 1;
        else if (spondee.Contains(str))
            return 2;
        else if (amphibrach.Contains(str))
            return 4;
        else if (dactyl.Contains(str))
            return 7;
        else if (iamb.Contains(str))
            return 8;
        else if (cretic.Contains(str))
            return 10;
        else if (trochee.Contains(str))
            return 11;
        else
            return 1;
    }

    void UpdateDisplays()
    {
        List<string> phrases = new List<string>(selectedPhrases.Keys);
        for (int i = 0; i < Lines.Length; i++)
        {
            Lines[i].text = phrases[i];
        }
    }

    void SelectPhrases(int num)
    {
        //Initialize random seed
        //System.Random rand = new System.Random('k' + 'o' + 's' + 'c' + 'i' + 'e');
        int seed = (int)(UnityEngine.Random.Range(1,1000));
        System.Random rand = new System.Random(seed);
        List<string> keyList = new List<string>(phraseValues.Keys);
        while (selectedPhrases.Count < num)
        {
            string phrase = keyList[rand.Next(phraseValues.Count)];
            if (!selectedPhrases.Keys.Contains(phrase))
            {
                int val = phraseValues[phrase];
                selectedPhrases.Add(phrase, val);
            }
        }
    }

    void GetKeyMeter(int footMod)
    {
        keyMeter = new int[] { -1,-1,-1,-1,-1,-1 };
        // Unstressed syllables have a value of 0, stressed syllables have a value of 1
        // Each foot pattern is represented as an array of 0s and 1s
        // In this dictionary, it's keyed off the values in table 1.2 mod 12
        Dictionary<int, List<int>> syllablePattern = new Dictionary<int, List<int>>()
        {
            //Molossus
            {1, new List<int>(){1,1,1}},
            //Spondee
            {2, new List<int>(){1,1}},
            //Bacchius
            {3, new List<int>(){0,1,1}},
            //Amphibrach
            {4, new List<int>(){0,1,0}},
            //Dibrach
            {5, new List<int>(){0,0}},
            //Antibacchius
            {6, new List<int>(){1,1,0}},
            //Dactyl
            {7, new List<int>(){1,0,0}},
            //Iamb
            {8, new List<int>(){0,1}},
            //Anapest
            {9, new List<int>(){0,0,1}},
            //Cretic
            {10, new List<int>(){1,0,1}},
            //Trochee
            {11, new List<int>(){1,0}},
            //Tribrach
            {0, new List<int>(){0,0,0}},
        };
        for (int i = 0; i < Lines.Length; i++)
        {
            Debug.LogFormat("[Meter #{0}] Line {1}", moduleId, i+1);
            string phrase = Lines[i].text;
            int value = selectedPhrases[phrase];
            Debug.LogFormat("[Meter #{0}] Foot value before modifier: {1}", moduleId, value);
            int modifiedValue = (value + footMod) % 12;
            Debug.LogFormat("[Meter #{0}] Modified foot value: {1}", moduleId, modifiedValue);
            List<int> pattern = syllablePattern[modifiedValue];
            var patStr = from x in pattern select x.ToString();
            Debug.LogFormat("[Meter #{0}] Pattern: [{1}]", moduleId, String.Join(",", patStr.ToArray()));
            int idx = Array.IndexOf(keyMeter, -1);
            for (int j = 0; j < pattern.Count; j++)
            {
                keyMeter[idx + j] = pattern[j];
            }
        }
        var keyStr = from x in keyMeter select x.ToString();
        Debug.LogFormat("[Meter #{0}] KeyMeter: [{1}]", moduleId, String.Join(",", keyStr.ToArray()));
    }

    // Update is called once per frame
    void Update () {

    }
}
