using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class pinpointScript : MonoBehaviour {

    public KMAudio Audio;

    public KMSelectable[] Positions;
    public GameObject Square;

    int[] points = { -1, -1, -1, -1 }; //position in reading order; was going to use a class for this but this is what _Zero, Zero_ does
    int[] pointXs = { -1, -1, -1, -1 };
    int[] pointYs = { -1, -1, -1, -1 };
    float[] dists = { -1f, -1f, -1f };
    float WAITTIME = 2f;
    float ZIPTIME = 0.5f;
    float[] posLUT = { -0.055f, -0.042777f, -0.030555f, -0.018333f, -0.006111f, 0.006111f, 0.018333f, 0.030555f, 0.042777f, 0.055f };
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
        do {
            points[0] = Rnd.Range(0, 100);
            points[1] = Rnd.Range(0, 100);
            points[2] = Rnd.Range(0, 100);
            points[3] = Rnd.Range(0, 100);
        }
        while (points[0]==points[1] || points[0]==points[2] || points[0]==points[3] || points[1]==points[2] || points[1]==points[3] || points[2]==points[3]);

        for (int p = 0; p < 4; p++) {
            pointXs[p] = points[p] % 10;
            pointYs[p] = points[p] / 10;
        }

        for (int p = 0; p < 3; p++) {
            int xd = Math.Abs(pointXs[3] - pointXs[p]);
            int yd = Math.Abs(pointYs[3] - pointYs[p]);
            dists[p] = (float)Math.Sqrt(xd*xd+yd*yd);
        }

        Debug.Log(dists.Join(" "));
        StartCoroutine(MoveSquare());
    }

    void PositionPress (KMSelectable P) {
        if (moduleSolved) { return; }
        for (int Q = 0; Q < Positions.Length; Q++) {
            if (Positions[Q] == P) {
                Debug.Log(Q);
            }
        }
    }

    private IEnumerator MoveSquare () {
        float elapsed = 0f;
        int loc = 0;
        while (true) {
            if (elapsed < WAITTIME) {
                Square.transform.localPosition = new Vector3(posLUT[pointXs[loc]], 0.02f, -posLUT[pointYs[loc]]);
            } else {
                Square.transform.localPosition = new Vector3(lerp(posLUT[pointXs[loc]], posLUT[pointXs[(loc+1)%3]], (elapsed - WAITTIME)*(1/ZIPTIME)), 0.02f, lerp(-posLUT[pointYs[loc]], -posLUT[pointYs[(loc+1)%3]], (elapsed - WAITTIME)*(1/ZIPTIME)));
            }
            yield return null;
            elapsed += Time.deltaTime;
            if (elapsed > (WAITTIME + ZIPTIME)) {
                elapsed -= (WAITTIME + ZIPTIME);
                loc = (loc + 1) % 3;
            }
        }
    }

    float lerp(float a, float b, float t) { //this assumes t is in the range 0-1
        return a*(1f-t) + b*t;
    }

}
