using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sector : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    Vector3[] normals = new Vector3[4];
    float sectorDist;
    float totalPriorDist;
    public Vector3[] corners;    

    //public Bounds bounds;
    // Start is called before the first frame update
    void Start()
    {        
        mesh = GetComponent<MeshFilter>().mesh;        
        vertices = mesh.vertices;
        
        int index = 0;
        //Debug.Log("name: " + name + " count: " + mesh.normals.Length);
        //foreach (Vector3 norm in mesh.normals)
        //{
        //    Debug.Log("normal: (" + norm.x + "," + norm.y + "," + norm.z + ")");
        //    //if (norm.y > 0 || norm.y < 0)
        //}
        
        int j = 0; int n = 0;
        corners = new Vector3[24];
        bool found = false;
        corners[j] = new Vector3(0.0f, 0.0f, 0.0f);
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
                // Debug.Log("vertex " + i + ": " + vertices[i]);


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
