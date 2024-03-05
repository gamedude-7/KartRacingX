using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sector : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    public Vector3[] normals;
    float sectorDist;
    float totalPriorDist;
    public Vector3[] corners;
    public Vector3 backLeftCorner;
    public Vector3 backRightCorner;
    public Vector3 frontLeftCorner;
    public Vector3 frontRightCorner;

    public Vector3 leftEdge,rightEdge,trailEdge,leadEdge;
    public Vector3 normOfLeftEdge, normOfRightEdge, normOfTrailEdge, normOfLeadEdge;


    //public Bounds bounds;
    // Start is called before the first frame update
    void Start()
    {        
        mesh = GetComponent<MeshFilter>().mesh;        
        vertices = mesh.vertices;
        corners = new Vector3[24];
        normals = new Vector3[24];
        int index = 0;
        //Debug.Log("name: " + name + " count: " + mesh.normals.Length);
        //foreach (Vector3 norm in mesh.normals)
        //{
        //    Debug.Log("normal: (" + norm.x + "," + norm.y + "," + norm.z + ")");
        //    //if (norm.y > 0 || norm.y < 0)
        //}
        
        int j = 0; int n = 0;
        
        bool found = false;
        //corners[j] = new Vector3(0.0f, 0.0f, 0.0f);
        //corners[j] = Vector3.up;
        //corners[j] = vertices[0];
        // float min_y = minY(vertices);
        Debug.Log("name: " + this.name);
        Debug.Log("# of vertices: " + vertices.Length);
        Debug.Log("Parent: " + this.transform.parent.name);
        n = 24;
        for (var i = 0; i < vertices.Length; i++)
        {
            found = false;
            //if (vertices[i].y < 0)
            //if (vertices[i].y < (min_y + 0.01f) && vertices[i].y > (min_y - 0.01f))
            //if (vertices[i].y < 0)
            //{
                 Debug.Log("vertex " + i + ": " + vertices[i]);


                //for (int a = 0; a < n; a++)
                //{
                //    Debug.Log("sector name: " + this.name);
                    Vector3 vertex = vertices[i];
            //BoxCollider boxCollider = GetComponent<BoxCollider>();
            //vertex.x = boxCollider.bounds.min.x * boxCollider.transform.localScale.x;
            //vertex.y = boxCollider.bounds.min.y * boxCollider.transform.localScale.y;
            //vertex.z = boxCollider.bounds.min.z * boxCollider.transform.localScale.z;
            vertex = TransformVertex( vertex, transform.rotation, transform.localScale);
            //vertex.x = vertices[i].x * transform.localScale.x;
            //vertex.y = vertices[i].y * transform.localScale.y;
            //vertex.z = vertices[i].z * transform.localScale.z;
            corners[i] = transform.position + vertex;
                    //if (transform.position + vertex == corners[a])
                    //{
                    //    found = true;
                    //}
               // }
                //if (!found)
                //{
                //    Vector3 vertex = vertices[i];
                //    //BoxCollider boxCollider = (BoxCollider)transform.GetComponent<BoxCollider>();
                //    //vertex.x = boxCollider.bounds.min.x * boxCollider.transform.localScale.x;
                //    //vertex.y = boxCollider.bounds.min.y * boxCollider.transform.localScale.y;
                //    //vertex.z = boxCollider.bounds.min.z * boxCollider.transform.localScale.z;
                //    vertex.x = vertices[i].x * transform.localScale.x;
                //    vertex.y = vertices[i].y * transform.localScale.y;
                //    vertex.z = vertices[i].z * transform.localScale.z;
                    
                //    corners[j] = transform.position + vertex;
                //    //Debug.Log("corner " + j + ": " + corners[j]);
                //    //corners[j] = vertices[i];

                //    j++;
                //   // n++;
                //}
            //}
        }
        //Debug.Log("corners: " + corners);
    }

    public void setEdgeNorms()
    {
        leftEdge = frontLeftCorner - backLeftCorner;
        rightEdge = frontRightCorner - backRightCorner;
        trailEdge = frontRightCorner - frontLeftCorner;
        leadEdge = backRightCorner - backLeftCorner;

        Vector3 halfLeftEdge = leftEdge / 2.0f;
        Vector3 halfRightEdge = rightEdge / 2.0f;
        Vector3 halfTrailEdge = trailEdge / 2.0f;
        Vector3 halfLeadEdge = leadEdge / 2.0f;
        normOfLeftEdge = (Quaternion.AngleAxis(90, Vector3.up) * halfLeftEdge).normalized;
        normOfRightEdge = (Quaternion.AngleAxis(-90, Vector3.up) * halfRightEdge).normalized;
        normOfTrailEdge = (Quaternion.AngleAxis(90, Vector3.up) * halfTrailEdge).normalized;
        normOfLeadEdge = (Quaternion.AngleAxis(-90, Vector3.up) * halfLeadEdge).normalized;

        //Debug.DrawLine(backLeftCorner, frontLeftCorner, Color.red);
        Debug.DrawLine(backLeftCorner + halfLeftEdge, backLeftCorner + halfLeftEdge + normOfLeftEdge, Color.red);
        Debug.DrawLine(backRightCorner + halfRightEdge, backRightCorner + halfRightEdge + normOfRightEdge, Color.yellow);
        Debug.DrawLine(frontLeftCorner + halfTrailEdge, frontLeftCorner + halfTrailEdge + normOfTrailEdge, Color.blue);
        Debug.DrawLine(backLeftCorner + halfLeadEdge, backLeftCorner + halfLeadEdge + normOfLeadEdge, Color.green);
    }

    public bool checkPointInsideSector(Vector3 pointOfInterest)
    {
        setEdgeNorms();
        Vector3 pointOnLeadingEdge = backLeftCorner;
        Vector3 pointOnTrailingEdge = frontRightCorner;
        Vector3 pointOnLeftEdge = frontLeftCorner;
        Vector3 pointOnRightEdge = backRightCorner;
        Vector3 leadingEdgeToPt = pointOfInterest - pointOnLeadingEdge;
        Vector3 trailingEdgeToPt = pointOfInterest - pointOnTrailingEdge;
        Vector3 pointOnLeftEdgeToPt = pointOfInterest - pointOnLeftEdge;
        Vector3 pointOnRightEdgeToPt = pointOfInterest - pointOnRightEdge;
        if (Vector3.Dot(pointOnRightEdgeToPt, normOfRightEdge) > 0 &&
                               Vector3.Dot(pointOnLeftEdgeToPt, normOfLeftEdge) > 0 &&
                               Vector3.Dot(leadingEdgeToPt, normOfLeadEdge) > 0 &&
                               Vector3.Dot(trailingEdgeToPt, normOfTrailEdge) > 0)
        {
            return true;
        }

        return false;
    }

    public static Vector3 TransformVertex( Vector3 position, Quaternion rotation, Vector3 scale)
    {

        Vector3 vertex = position;
        Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);
       
        vertex = matrix.MultiplyPoint(position);
        
        return vertex;
    }

    private void OnTriggerEnter(Collider other)
    {
        BoxCollider boxCollider = (BoxCollider)other;
    }

    float minY(Vector3[] vertices)
    {   
        float min = 9999;
        for (int i = 0; i < vertices.Length; i++)
        {
            if (vertices[i].y < min)
                min = vertices[i].y;
        }
        return min;
    }

    // Update is called once per frame
    void Update()
    {
      
    }
}
