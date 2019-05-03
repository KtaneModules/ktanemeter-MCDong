using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
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
        //molossus
        phraseValues.Add("Break, Break, Break", 1);
        //spondee
        phraseValues.Add("Cry! Cry!", 2);
        phraseValues.Add("Shut up!", 2);
        phraseValues.Add("Fie, fie!", 2);
        //bacchius
        phraseValues.Add("When day breaks /\nthe fish bite /\nat small flies", 3);
        //amphibrach
        phraseValues.Add("it's four in the morning,\n\tthe end of december /\nI'm writing you now \n\tjust to see if you're better", 4);
        phraseValues.Add("And, speaking of birds,\n\tthere’s the Russian Palooski /\nwhose headski is redski\n\tand belly is blueski", 4);
        phraseValues.Add("There once was a girl from Nantucket", 4);
        phraseValues.Add("How dead to my heart are\n\tthe scenes of my childhood", 4);
        //dibrach
        //antibacchius
        phraseValues.Add("Blind luck is loved more\n\tthan hard thinking", 6);
        //dactyl
        phraseValues.Add("Out of the mockingbird's throat", 7);
        phraseValues.Add("Higgledy piggledy, /\nBacon, lord Chancellor.", 7);
        phraseValues.Add("This is the forest primeval.\n\tThe murmuring pines and the hemlocks", 7);
        phraseValues.Add("Half a league, half a league, /\nHalf a league onward,", 7);
        phraseValues.Add("Down to the shores of the water,\n\tthe path by the swamp in the dimness", 7);
        //iamb
        phraseValues.Add("If you were coming in the fall /\nI'd brush the summer by", 8);
        phraseValues.Add("The guests are met,\n\tthe feast is set: /\nMay'st hear the merry din.", 8);
        phraseValues.Add("The way a crow /\nShook down on me /\nThe dust of snow", 8);
        phraseValues.Add("Rough winds do shake the\n\tdarling buds of May,\n\tAnd summer's lease hath\n\tall too short a date", 8);
        phraseValues.Add("Is this chicken, what I have,\n\tor is this fish?", 8);
        phraseValues.Add("To swell the gourd and\n\tplump the hazel shells", 8);
        //anapest
        phraseValues.Add("Twas the night before Christmas\n\tand all through the house", 9);
        phraseValues.Add("There was one who was famed\n\tfor the number of things /\nHe forgot when he\n\tentered the sip:", 9);
        phraseValues.Add("You have brains in your head. /\nYou have feet in your shoes", 9);
        phraseValues.Add("And since birth I've been cursed\n\twith this curse to just curse", 9);
        //cretic
        phraseValues.Add("La-di-dah!", 10);
        //trochee
        phraseValues.Add("Tyger! Tyger! Burning Bright", 11);
        phraseValues.Add("Once upon a midnight dreary,\n\twhile I pondered,\n\tweak and weary,", 11);
        phraseValues.Add("And the Raven, never flitting,\n\tstill is sitting,\n\tstill is sitting", 11);
        phraseValues.Add("Double, double toil and trouble; /\nFire burn and cauldron bubble.", 11);
        phraseValues.Add("One Fish Two Fish Red Fish Blue Fish", 11);
        phraseValues.Add("Got a long list of ex-lovers", 11);
        //tribrach
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
            Debug.LogFormat("[Meter #{0}] Pressed {1} button", moduleId, buttonIndex == 0 ? "u" : "-");
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
        for (int i = 0, j = serial.Length - 1; i < serial.Length; i++, j--)
        {
            string cur_i = serial.Substring(i, 1);
            if (alphabet.Contains(cur_i) && numLetters < 2)
            {
                numLetters++;
                int nato = GetNatoValue(cur_i.ToUpper());
                Debug.LogFormat("[Meter #{0}] Nato value of {1} is {2}. Added to foot modifier", moduleId, cur_i.ToUpper(), nato);
                footMod += nato;
            }

            string cur_j = serial.Substring(j, 1);
            if (digits.Contains(cur_j) && numDigits == 0)
            {
                numDigits++;
                Debug.LogFormat("[Meter #{0}] Serial number digit {1} added to foot modifier", moduleId, cur_j);
                footMod += Int32.Parse(cur_j);
            }

        }
        Debug.LogFormat("[Meter #{0}] Foot Modifier: {1}", moduleId, footMod);
        return footMod;
    }

    int GetNatoValue(string str)
    {
        Dictionary<string, int> natoValues = new Dictionary<string, int>()
        {
            //single 
            {"GM", 1},
            //spondee 
            {"VXZ", 2},
            //amphibrach 
            {"NS", 4},
            //dactyl 
            {"IRU", 7},
            //iamb 
            {"HQ", 8},
            //anapest 
            {"J", 9},
            //trochee 
            {"ABCDEFKLOPTWY", 11},

        };

        foreach (KeyValuePair<string, int> nato in natoValues)
        {
            if (nato.Key.Contains(str))
            {
                return nato.Value;
            }
        }
        Debug.LogError(string.Format("'{0}' was not recognized as an alphabet letter", str));
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
            string phrase = Lines[i].text;
            Debug.LogFormat("[Meter #{0}] **Line {1} - \"{2}\"**", moduleId, i + 1, phrase);
            int value = selectedPhrases[phrase];
            Debug.LogFormat("[Meter #{0}] Foot value before modifier: {1}", moduleId, value);
            int modifiedValue = (value + footMod) % 12;
            Debug.LogFormat("[Meter #{0}] Modified foot value: ({1} + {2}) = {3}; {3} mod 12 = {4}", moduleId, value, footMod, value+footMod, modifiedValue);
            List<int> pattern = syllablePattern[modifiedValue];
            var logPat = pattern.Select(x => x == -1 ? "" : x == 0 ? "u" : "-").ToArray();
            Debug.LogFormat("[Meter #{0}] Pattern for foot value {1}: [{2}]", moduleId, modifiedValue, String.Join(" ", logPat));
            int idx = Array.IndexOf(keyMeter, -1);
            for (int j = 0; j < pattern.Count; j++)
            {
                keyMeter[idx + j] = pattern[j];
            }
        }
        var keyPat = keyMeter.Select(x => x == -1 ? "" : x == 0 ? "u" : "-").ToArray();
        Debug.LogFormat("[Meter #{0}] Key Meter: [{1}]", moduleId, String.Join(" ", keyPat));
    }

    // Update is called once per frame
    void Update () {

    }
}
