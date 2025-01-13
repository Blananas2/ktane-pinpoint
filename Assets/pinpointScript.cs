using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class pinpointScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;

    public KMSelectable[] Positions;

    bool submissionMode = false;

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake () {
        moduleId = moduleIdCounter++;
        
        foreach (KMSelectable Position in Positions) {
            Position.OnInteract += delegate () { PositionPress(Position); return false; };
        }
    }

    // Use this for initialization
    void Start () {

    }

    /*
    void Update () {

    }
    */

    void PositionPress (KMSelectable P) {
        if (moduleSolved) { return; }
        for (int Q = 0; Q < Positions.Length; Q++) {
            if (Positions[Q] == P) {
                Debug.Log(Q);
            }
        }
    }

}
