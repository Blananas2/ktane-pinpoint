using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class pinpointScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombModule Module;

    public KMSelectable[] Positions;
    public GameObject Square;
    public GameObject Rails;
    public SpriteRenderer HorizScissors;
    public SpriteRenderer VertiScissors;
    public Sprite[] ScissorSprites;
    public SpriteRenderer Arm;
    public GameObject DistanceObj;
    public TextMesh Distance;
    public GameObject StatusLight;
    public SpriteRenderer[] SymbolSlots;
    public Sprite[] SymbolSprites;

    readonly int[] points = { -1, -1, -1, -1 }; //position in reading order; was going to use a class for this but this is what _Zero, Zero_ does
    readonly int[] pointXs = { -1, -1, -1, -1 };
    readonly int[] pointYs = { -1, -1, -1, -1 };
    float scaleFactor = -1f;
    readonly float[] dists = { -1f, -1f, -1f };
    int shownPoint = 0;
    const float HUESCALE = 0.0005f;
    const float WAITTIME = 4f;
    const float ZIPTIME = 0.5f;
    const float SCRUBTIME = 0.15f;
    Vector3 SLDEFAULT = new Vector3(0.075167f, 0.018f, 0.076057f); //need to put it back here for TP
    float[] posLUT = { -0.055f, -0.042777f, -0.030555f, -0.018333f, -0.006111f, 0.006111f, 0.018333f, 0.030555f, 0.042777f, 0.055f };
    bool submissionMode = false;
    int hoverPosition = -1;

    Coroutine moveSquareCoroutine;
    Coroutine cycleAnimationCoroutine;

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;

        foreach (KMSelectable Position in Positions)
        {
            Position.OnInteract += delegate () { PositionPress(Position); return false; };
            Position.OnHighlight += delegate () { if (submissionMode) { UpdateHoverPosition(Position); } };
        }
    }

    void Start()
    {
        StatusLight.SetActive(false);
        scaleFactor = Rnd.Range(18, 7857) * 0.001f; //scale factors in this range ensure that 1) all the possible hypotenuses have distinct values when truncated to 3 decimals of precision and 2) the maximum scaled hypotenuse is under 100
        do
        {
            for (int p = 0; p < 4; p++)
            {
                points[p] = Rnd.Range(0, 100);
                pointXs[p] = points[p] % 10;
                pointYs[p] = points[p] / 10;
            }
        }
        while (PointsMatch() || PointsCollinear()); //fortunately, even though points matching would break PointsCollinear() via a division by 0, if PointsMatch() is true, C# will not evaulate PointsCollinear()!
        for (int p = 0; p < 3; p++)
        {
            int xd = Math.Abs(pointXs[3] - pointXs[p]);
            int yd = Math.Abs(pointYs[3] - pointYs[p]);
            dists[p] = (float) Math.Sqrt(xd * xd + yd * yd) * scaleFactor;
        }
        Debug.LogFormat("[Pinpoint #{0}] Given points:", moduleId);
        Debug.LogFormat("[Pinpoint #{0}] {1}, distance of {2}", moduleId, GridPos(points[0]), Trunc(dists[0]));
        Debug.LogFormat("[Pinpoint #{0}] {1}, distance of {2}", moduleId, GridPos(points[1]), Trunc(dists[1]));
        Debug.LogFormat("[Pinpoint #{0}] {1}, distance of {2}", moduleId, GridPos(points[2]), Trunc(dists[2]));
        Debug.LogFormat("[Pinpoint #{0}] With scale factor of {1}, the target point is {2}", moduleId, Trunc(scaleFactor), GridPos(points[3]));
        Debug.LogFormat("<Pinpoint #{0}> Raw float values: dists = {1}, scaleFactor = {2}", moduleId, dists.Join(" "), scaleFactor);
        UpdateDistanceArm();
        StartCoroutine(HueShift());
        if (cycleAnimationCoroutine != null)
            StopCoroutine(cycleAnimationCoroutine);
        cycleAnimationCoroutine = StartCoroutine(CycleAnimation());
    }

    bool PointsMatch() { return points[0] == points[1] || points[0] == points[2] || points[0] == points[3] || points[1] == points[2] || points[1] == points[3] || points[2] == points[3]; }

    bool PointsCollinear()
    {
        return (pointYs[1] - pointYs[0]) * (pointXs[2] - pointXs[0]) == (pointYs[2] - pointYs[0]) * (pointXs[1] - pointXs[0]);
    }

    private IEnumerator HueShift()
    {
        float elapsed = Rnd.Range(0f, 1f / HUESCALE);
        while (!moduleSolved)
        {
            Color c = Color.HSVToRGB(elapsed * HUESCALE, 0.5f, 1f);
            Square.GetComponent<MeshRenderer>().material.color = c;
            Rails.GetComponent<MeshRenderer>().material.color = c;
            HorizScissors.color = c;
            VertiScissors.color = c;
            yield return null;
            elapsed += Time.deltaTime;
            if (elapsed * HUESCALE > 1f)
                elapsed = 0f;
        }
        Square.GetComponent<MeshRenderer>().material.color = Color.white;
        Rails.GetComponent<MeshRenderer>().material.color = Color.white;
        HorizScissors.color = Color.white;
        VertiScissors.color = Color.white;
    }

    void PositionPress(KMSelectable P)
    {
        if (moduleSolved) { return; }
        for (int Q = 0; Q < Positions.Length; Q++)
        {
            if (Positions[Q] == P)
            {
                if (!submissionMode)
                {
                    if (cycleAnimationCoroutine != null)
                        StopCoroutine(cycleAnimationCoroutine);
                    if (moveSquareCoroutine != null)
                        StopCoroutine(moveSquareCoroutine);
                    submissionMode = true;
                    hoverPosition = Q;
                    if (moveSquareCoroutine != null)
                        StopCoroutine(moveSquareCoroutine);
                    Vector3 startPos = Square.transform.localPosition;
                    float qx = posLUT[Q % 10];
                    float qz = -posLUT[Q / 10];
                    Vector3 goalPos = new Vector3(qx, 0.02f, qz);
                    moveSquareCoroutine = StartCoroutine(MoveSquare(startPos, goalPos, SCRUBTIME));
                    Arm.gameObject.SetActive(false);
                    DistanceObj.SetActive(false);
                    Audio.PlaySoundAtTransform("smallgong", transform);
                    return;
                }
                else
                {
                    StatusLight.SetActive(true);
                    StatusLight.transform.localPosition = new Vector3(posLUT[Q % 10], 0.018f, -posLUT[Q / 10]);
                    submissionMode = false;
                    if (Q == points[3])
                    {
                        StartCoroutine(ExpandSymbol(true));
                        Module.HandlePass();
                        moduleSolved = true;
                        Audio.PlaySoundAtTransform("biggong", transform);
                        Debug.LogFormat("[Pinpoint #{0}] Submitted {1}, that is correct, module solved.", moduleId, GridPos(Q));
                    }
                    else
                    {
                        StartCoroutine(ExpandSymbol(false));
                        Module.HandleStrike();
                        Debug.LogFormat("[Pinpoint #{0}] Submitted {1}, that is incorrect, strike!", moduleId, GridPos(Q));
                        StartCoroutine(WaitASecThenContinue());
                    }
                }
            }
        }
    }

    void UpdateHoverPosition(KMSelectable P)
    {
        for (int Q = 0; Q < Positions.Length; Q++)
        {
            if (Positions[Q] == P)
            {
                hoverPosition = Q;
                if (moveSquareCoroutine != null)
                    StopCoroutine(moveSquareCoroutine);
                float qx = posLUT[Q % 10];
                float qz = -posLUT[Q / 10];
                Vector3 goalPos = new Vector3(qx, 0.02f, qz);
                moveSquareCoroutine = StartCoroutine(MoveSquare(Square.transform.localPosition, goalPos, SCRUBTIME));
            }
        }
    }

    private IEnumerator CycleAnimation()
    {
        Color opc = new Color(1f, 1f, 1f, 0f);
        Arm.color = opc;
        Distance.color = opc;
        while (!submissionMode)
        {
            if (moveSquareCoroutine != null)
                StopCoroutine(moveSquareCoroutine);
            Vector3 startPos = Square.transform.localPosition;
            Vector3 goalPos = new Vector3(posLUT[pointXs[(shownPoint + 1) % 3]], 0.02f, -posLUT[pointYs[(shownPoint + 1) % 3]]);
            moveSquareCoroutine = StartCoroutine(MoveSquare(startPos, goalPos, ZIPTIME));
            yield return new WaitForSeconds(ZIPTIME);
            shownPoint = (shownPoint + 1) % 3;
            UpdateDistanceArm();
            float elapsed = 0f;
            Arm.gameObject.SetActive(true);
            DistanceObj.SetActive(true);
            while (elapsed < WAITTIME)
            {
                opc = new Color(1f, 1f, 1f, Lerp(1f, 0f, Math.Abs(elapsed - WAITTIME / 2) / (WAITTIME / 2)));
                Arm.color = opc;
                Distance.color = opc;
                yield return null;
                elapsed += Time.deltaTime;
            }
        }
    }

    private IEnumerator ExpandSymbol(bool g)
    {
        for (int s = 0; s < 3; s++)
        {
            SymbolSlots[s].sprite = SymbolSprites[g ? 1 : 0];
        }
        float elapsed = 0f;
        float duration = 1f;
        float threshold = 0.8f;
        float[] sizes = { 0.000625f, 0.000625f, 0.000625f };
        while (elapsed < duration)
        {
            sizes[0] *= 1.06f;
            sizes[1] *= 1.08f;
            sizes[2] *= 1.1f;
            for (int s = 0; s < 3; s++)
            {
                SymbolSlots[s].transform.localScale = new Vector3(sizes[s], sizes[s], 1f);
                if (elapsed > threshold)
                {
                    SymbolSlots[s].color = new Color(1f, 1f, 1f, Lerp(0.5f, 0f, (elapsed - threshold) / (duration - threshold)));
                }
            }
            yield return null;
            elapsed += Time.deltaTime;
        }
        for (int s = 0; s < 3; s++)
        {
            if (g)
            {
                SymbolSlots[s].gameObject.SetActive(false);
            }
            else
            {
                SymbolSlots[s].color = new Color(1f, 1f, 1f, 0.5f);
            }
        }
    }

    private IEnumerator MoveSquare(Vector3 start, Vector3 goal, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            Square.transform.localPosition = new Vector3(Mathf.Lerp(start.x, goal.x, elapsed / duration), 0.02f, Mathf.Lerp(start.z, goal.z, elapsed / duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        Square.transform.localPosition = new Vector3(goal.x, 0.02f, goal.z);
    }

    private IEnumerator WaitASecThenContinue()
    {
        yield return new WaitForSeconds(1f);
        StatusLight.transform.localPosition = SLDEFAULT;
        StatusLight.SetActive(false);
        if (cycleAnimationCoroutine != null)
            StopCoroutine(cycleAnimationCoroutine);
        cycleAnimationCoroutine = StartCoroutine(CycleAnimation());
    }

    void Update() //this just updates the scissoring sprites, it doesn't *necessarily* need to be done *every* frame but it simplifies everything else so whatever
    {
        HorizScissors.transform.localPosition = new Vector3(0f, 0f, Square.transform.localPosition.z * 16.667f);
        VertiScissors.transform.localPosition = new Vector3(Square.transform.localPosition.x * 16.667f, 0f, 0f);
        HorizScissors.sprite = ScissorSprites[(int) Math.Round((Square.transform.localPosition.x + 0.055f) / 0.00305575f, 0)];
        VertiScissors.sprite = ScissorSprites[(int) Math.Round((-Square.transform.localPosition.z + 0.055f) / 0.00305575f, 0)];
    }

    void UpdateDistanceArm()
    {
        Arm.flipX = Square.transform.localPosition.x > 0f;
        Arm.flipY = Square.transform.localPosition.z < 0f;
        DistanceObj.transform.localPosition = new Vector3(Square.transform.localPosition.x > 0f ? -0.386f : 0.386f, 0.15f, Square.transform.localPosition.z < 0f ? 0.85f : -0.725f);
        Distance.text = Trunc(dists[shownPoint]);
    }

    float Lerp(float a, float b, float t)
    { //this assumes t is in the range 0-1
        return a * (1f - t) + b * t;
    }

    string Trunc(float f)
    {
        return f.ToString("0.000");
    }

    string GridPos(int p)
    {
        return "ABCDEFGHIJ"[p % 10] + (p / 10 + 1).ToString();
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} submit A1 [Submits position A1.] | Columns are labeled A-J from left to right. | Rows are labeled 1-10 from top to bottom.";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToUpperInvariant();
        Match m = Regex.Match(command, @"^\s*(submit|press|click|tap)\s+(?<col>[A-J])\s*(?<row>\d+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            yield break;
        string cg = m.Groups["col"].Value;
        string rg = m.Groups["row"].Value;
        int col = cg[0] - 'A';
        int row;
        if (!int.TryParse(rg, out row) || row < 1 || row > 10)
            yield break;
        int pos = (row - 1) * 10 + col;
        yield return null;
        if (!submissionMode)
        {
            Positions[pos].OnInteract();
            yield return new WaitForSeconds(0.5f);
        }
        Positions[pos].OnHighlight();
        yield return new WaitForSeconds(0.75f);
        Positions[pos].OnInteract();
        yield break;
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        int pos = points[3];
        if (!submissionMode)
        {
            Positions[pos].OnInteract();
            yield return new WaitForSeconds(0.5f);
        }
        Positions[pos].OnHighlight();
        yield return new WaitForSeconds(0.75f);
        Positions[pos].OnInteract();
        yield break;
    }
}
