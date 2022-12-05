using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathUtil 
{
    public static Vector3 GetNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        // Find vectors corresponding to two of the sides of the triangle.
        Vector3 side1 = b - a;
        Vector3 side2 = c - a;

        // Cross the vectors to get a perpendicular vector, then normalize it.
        return Vector3.Cross(side1, side2).normalized;
    }

    // https://wiki.unity3d.com/index.php/3d_Math_functions
    //This function returns a point which is a projection from a point to a line.
    //The line is regarded infinite. If the line is finite, use ProjectPointOnLineSegment() instead.
    public static Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point)
    {

        //get vector from point on line to point in space
        Vector3 linePointToPoint = point - linePoint;

        float t = Vector3.Dot(linePointToPoint, lineVec);

        return linePoint + lineVec * t;
    }

    //Get the shortest distance between a point and a plane. The output is signed so it holds information
    //as to which side of the plane normal the point is.
    public static float SignedDistancePlanePoint(Vector3 planeNormal, Vector3 planePoint, Vector3 point)
    {

        return Vector3.Dot(planeNormal, (point - planePoint));
    }

    // https://wiki.unity3d.com/index.php/3d_Math_functions
    //This function returns a point which is a projection from a point to a plane.
    public static Vector3 ProjectPointOnPlane(Vector3 planeNormal, Vector3 planePoint, Vector3 point)
    {

        float distance;
        Vector3 translationVector;

        //First calculate the distance from the point to the plane:
        distance = SignedDistancePlanePoint(planeNormal, planePoint, point);

        //Reverse the sign of the distance
        distance *= -1;

        //Get a translation vector
        translationVector = SetVectorLength(planeNormal, distance);

        //Translate the point to form a projection
        return point + translationVector;
    }

    //create a vector of direction "vector" with length "size"
    public static Vector3 SetVectorLength(Vector3 vector, float size)
    {

        //normalize the vector
        Vector3 vectorNormalized = Vector3.Normalize(vector);

        //scale the vector
        return vectorNormalized *= size;
    }

    // https://wiki.unity3d.com/index.php/Barycentric
    public static Vector3 Barycentric(Vector3 aV1, Vector3 aV2, Vector3 aV3, Vector3 aP)
    {
        Vector3 a = aV2 - aV3, b = aV1 - aV3, c = aP - aV3;
        float aLen = a.x * a.x + a.y * a.y + a.z * a.z;
        float bLen = b.x * b.x + b.y * b.y + b.z * b.z;
        float ab = a.x * b.x + a.y * b.y + a.z * b.z;
        float ac = a.x * c.x + a.y * c.y + a.z * c.z;
        float bc = b.x * c.x + b.y * c.y + b.z * c.z;
        float d = aLen * bLen - ab * ab;
        var u = (aLen * bc - ab * ac) / d;
        var v = (bLen * ac - ab * bc) / d;
        var w = 1.0f - u - v;
        return new Vector3(u, v, w);
    }

    public static bool BarycentricInside(float u, float v, float w)
    {
        return (u >= 0.0f) && (u <= 1.0f) && (v >= 0.0f) && (v <= 1.0f) && (w >= 0.0f); //(w <= 1.0f)
    }

    // three points, first has a weight of 1, others have weights of 0, 
    // returns the weight of the position aP
    public static float BaryCentricWeight(Vector3 w1, Vector3 w0A, Vector3 w0B, Vector3 aP)
    {
        Vector3 a = w0A - w0B, b = w1 - w0B, c = aP - w0B;
        float aLen = a.x * a.x + a.y * a.y + a.z * a.z;
        float bLen = b.x * b.x + b.y * b.y + b.z * b.z;
        float ab = a.x * b.x + a.y * b.y + a.z * b.z;
        float ac = a.x * c.x + a.y * c.y + a.z * c.z;
        float bc = b.x * c.x + b.y * c.y + b.z * c.z;
        float d = aLen * bLen - ab * ab;
        var u = (aLen * bc - ab * ac) / d;
        var v = (bLen * ac - ab * bc) / d;
        var w = 1.0f - u - v;

        //return u * Color.white + v * Color.black + w * Color.black;
        // OR: u * 1 + v * 0 + w * 1;

        // clamp if outside triangle e.g near control => white, past => black? 
        // could happen e.g grid cell accuracy
        // snap to nearest edge, recalc bary? doesn't work if point pojection missing triangle
        // TRY: just clamp u, v, w? didn't work e.g left => darker, right => lighter
        // https://answers.unity.com/questions/424974/nearest-point-on-mesh.html suggests this should work
        u = Mathf.Clamp01(u);
        //v = Mathf.Clamp01(v);
        //w = Mathf.Clamp01(w);

        return u;// u;
        /*if (BarycentricInside(u, v, w)) {
			return u;
		} else if (u < 0.5f) {
			return 0;
		} else {
			return 1;
		}*/

    }

    public static Color GetBaryCentricColor(Vector3 w1, Vector3 w0A, Vector3 w0B, Vector3 aP)
    {
        var u = BaryCentricWeight(w1, w0A, w0B, aP);
        return new Color(u, u, u, 1);
    }
}
