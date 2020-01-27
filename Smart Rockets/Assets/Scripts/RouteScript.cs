using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RouteScript : MonoBehaviour {
    // Start is called before the first frame update
    [SerializeField]
    private Transform[] points;
    private Vector2 gizmoPosition;

    private void OnDrawGizmos() {
        for (int i = 1; i < points.Length; i++) {
            for (float t = 0; t <= 1; t += .05f) {
                gizmoPosition = (1 - t) * points[i - 1].position +
                    (t) * points[i].position;
                Gizmos.DrawSphere(gizmoPosition, .09f);
            }
        }


        //for (int i = 1; i < points.Length; i++) {
        //    Gizmos.DrawLine(new Vector2(points[i-1].position.x, points[i-1].position.y),
        //    new Vector2(points[i].position.x, points[i].position.y));
        //}
    }

}
