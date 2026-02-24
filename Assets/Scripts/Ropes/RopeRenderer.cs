using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Replaces Shapes (paid asset) dependency with Unity's built-in LineRenderer
// and the project's own BezierSpline component.
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(BezierSpline))]
public class RopeRenderer : MonoBehaviour {
    public Transform projectile;
    private LineRenderer lineRenderer;
    private BezierSpline bezierSpline;

    [Header("Spline Rendering")]
    [Range(4, 64)]
    public int numDrawnPoints = 4;

    [Range(0f, 2f)]
    public float ropeThickness = .1f;
    public Gradient ropeColor;
    [Range(0f, 1f)]
    public float ropeColorT;


    [Header("Spline Offset")]
    [Range(0f, 1f)]
    public float offsetTime;
    public AnimationCurve offsetCurve;

    [Range(0f, 1f)]
    public float controlPointForwardOffset1, controlPointForwardOffset2;
    [Range(-2f, 2f)]
    public float controlPointRightOffset1, controlPointRightOffset2;

    private Vector3 controlPoint1, controlPoint2;


    private void OnValidate() {
        lineRenderer = GetComponent<LineRenderer>();
        bezierSpline = GetComponent<BezierSpline>();
        ApplyLineRendererSettings();
    }

    private void Awake() {
        lineRenderer = GetComponent<LineRenderer>();
        bezierSpline = GetComponent<BezierSpline>();
        ApplyLineRendererSettings();
    }

    private void ApplyLineRendererSettings() {
        if (lineRenderer == null) return;

        // Use world space so positions set in Update() are treated as world coords
        lineRenderer.useWorldSpace = false;

        // Set width from ropeThickness
        lineRenderer.startWidth = ropeThickness;
        lineRenderer.endWidth = ropeThickness;

        // Apply the gradient colour
        lineRenderer.colorGradient = BuildSolidGradient(ropeColor.Evaluate(ropeColorT));

        // Smooth the line
        lineRenderer.numCornerVertices = 4;
        lineRenderer.numCapVertices = 4;
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.rotation * (transform.position + controlPoint1), .05f);
        Gizmos.DrawWireSphere(transform.rotation * (transform.position + controlPoint2), .05f);

        if (lineRenderer == null || bezierSpline == null) {
            Awake();
        }
    }

    private void Update() {
        UpdateControlPoints();
        UpdateRopeRender();
    }

    public void UpdateRopeRender() {
        UpdatePolyline();

        // Update colour every frame (ropeColorT can change at runtime)
        lineRenderer.colorGradient = BuildSolidGradient(ropeColor.Evaluate(ropeColorT));

        // Update width in case it was tweaked at runtime
        lineRenderer.startWidth = ropeThickness;
        lineRenderer.endWidth = ropeThickness;
    }

    // TODO: Is jitter related to offset time being a thing?
    public void UpdateControlPoints() {
        // Find our basis vector
        Vector3 forward = Quaternion.Inverse(transform.rotation) * (projectile.position - transform.position);
        float totalLength = forward.magnitude;

        forward.Normalize();
        Vector3 right = Vector3.Cross(transform.up, forward);

        // Calculate control point 1, plus the manually defined offset
        controlPoint1 = forward * totalLength * controlPointForwardOffset1;
        controlPoint1 += right * controlPointRightOffset1 * offsetCurve.Evaluate(offsetTime);

        // Calculate control point 2, plus the manually defined offset
        controlPoint2 = forward * totalLength * controlPointForwardOffset2;
        controlPoint2 += right * controlPointRightOffset2 * offsetCurve.Evaluate(offsetTime);

        // -- Set the control points in the spline (using the project's own BezierSpline)
        bezierSpline.SetControlPoint(0, Vector3.zero);
        bezierSpline.SetControlPoint(1, controlPoint1);
        bezierSpline.SetControlPoint(2, controlPoint2);
        bezierSpline.SetControlPoint(3, forward * totalLength);
    }

    private void UpdatePolyline() {
        lineRenderer.positionCount = numDrawnPoints;

        for (int i = 0; i < numDrawnPoints; i++) {
            float t = (float)i / (numDrawnPoints - 1);
            Vector3 point = bezierSpline.GetPointLocalSpace(t);
            lineRenderer.SetPosition(i, point);
        }
    }

    // LineRenderer.colorGradient expects an actual Gradient object.
    // This helper builds a flat (solid) gradient from a single colour.
    private Gradient BuildSolidGradient(Color color) {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(color.a, 0f),
                new GradientAlphaKey(color.a, 1f)
            }
        );
        return gradient;
    }
}
