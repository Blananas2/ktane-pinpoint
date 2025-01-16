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
    public SpriteRenderer Arm;
    public GameObject DistanceObj;
    public TextMesh Distance;
    public Material ColorMat;

    int[] points = { -1, -1, -1, -1 }; //position in reading order; was going to use a class for this but this is what _Zero, Zero_ does
    int[] pointXs = { -1, -1, -1, -1 };
    int[] pointYs = { -1, -1, -1, -1 };
    float scaleFactor = -1f;
    float[] dists = { -1f, -1f, -1f };
    float HUESCALE = 0.0005f;
    int shownPoint = 0;
    float WAITTIME = 4f;
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

    void Start () {
        scaleFactor = Rnd.Range(18, 7857) * 0.001f; //scale factors in this range ensure that 1) all the possible hypotenuses have distinct values when truncated to 3 decimals of precision and 2) the maximum a scaled hypotenuse is under 100
        Debug.Log("Scale factor: " + scaleFactor);

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
            dists[p] = (float)Math.Sqrt(xd*xd+yd*yd) * scaleFactor;
        }

        Debug.Log("Distances: " + dists.Join(", "));
        UpdateDistanceArm();
        StartCoroutine(HueShift());
        StartCoroutine(MoveSquare());
    }

    private IEnumerator HueShift () {
        float elapsed = Rnd.Range(0f, 1f/HUESCALE);
        while (true) {
            ColorMat.color = Color.HSVToRGB(elapsed * HUESCALE, 0.5f, 1f);
            yield return null;
            elapsed += Time.deltaTime;
            if (elapsed * HUESCALE > 1f) { elapsed = 0f; }
        }
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
        while (true) {
            if (elapsed < WAITTIME) {
                Arm.gameObject.SetActive(true);
                DistanceObj.SetActive(true);
                Color opc = new Vector4(1f, 1f, 1f, lerp(1f, 0f, Math.Abs(elapsed - WAITTIME/2)/(WAITTIME/2)));
                Arm.color = opc;
                Distance.color = opc;
            } else {
                Arm.gameObject.SetActive(false);
                DistanceObj.SetActive(false);
                Square.transform.localPosition = new Vector3(lerp(posLUT[pointXs[shownPoint]], posLUT[pointXs[(shownPoint+1)%3]], (elapsed - WAITTIME)*(1/ZIPTIME)), 0.02f, lerp(-posLUT[pointYs[shownPoint]], -posLUT[pointYs[(shownPoint+1)%3]], (elapsed - WAITTIME)*(1/ZIPTIME)));
            }
            yield return null;
            elapsed += Time.deltaTime;
            if (elapsed > (WAITTIME + ZIPTIME)) {
                shownPoint = (shownPoint + 1) % 3;
                UpdateDistanceArm();
                elapsed = 0f;
            }
        }
    }

    void UpdateDistanceArm() {
        Square.transform.localPosition = new Vector3(posLUT[pointXs[shownPoint]], 0.02f, -posLUT[pointYs[shownPoint]]);
        Arm.flipX = Square.transform.localPosition.x > 0f;
        Arm.flipY = Square.transform.localPosition.z < 0f;
        DistanceObj.transform.localPosition = new Vector3(Square.transform.localPosition.x > 0f ? -0.386f : 0.386f, 0.15f, Square.transform.localPosition.z < 0f ? 0.85f : -0.725f);
        Distance.text = trunc(dists[shownPoint]);
    }

    float lerp(float a, float b, float t) { //this assumes t is in the range 0-1
        return a*(1f-t) + b*t;
    }

    string trunc(float f) {
        string s = f.ToString();
        if (s.IndexOf('.') == -1) {
            return s + ".000";
        } else {
            string[] c = s.Split('.');
            c[1] = c[1].PadRight(3, '0').Substring(0, 3);
            return c[0] + "." + c[1];
        }
    }
}
