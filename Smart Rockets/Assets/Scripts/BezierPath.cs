using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BezierPath : MonoBehaviour {
    public BezierCurve[] curvePath;
    public BezierPath(BezierCurve[] curve) {
        curvePath = curve;
    }
    public class BezierCurve {
        public Vector3 point1;
        public Vector3 point2;
        public Vector3 point3;
        public Vector3 point4;
        public BezierCurve(Vector3 point1, Vector3 point2, Vector3 point3, Vector3 point4) {
            this.point1 = point1;
            this.point2 = point2;
            this.point3 = point3;
            this.point4 = point4;
        }
    }
}
