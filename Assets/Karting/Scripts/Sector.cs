using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sector : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    public Vector3[] corners;
    //public Bounds bounds;
    // Start is called before the first frame update
    void Start()
    {        
        mesh = GetComponent<MeshFilter>().mesh;
        
        vertices = mesh.vertices;
        int j = 0; int n = 0;
        corners = new Vector3[4];
        bool found = false;
        corners[j] = new Vector3(0.0f, 0.0f, 0.0f);
        //corners[j] = Vector3.up;
        //corners[j] = vertices[0];
        float min_y = minY(vertices);
        for (var i = 0; i < vertices.Length; i++)
        {
            found = false;
            //if (vertices[i].y < 0)
            if (vertices[i].y < (min_y + 0.01f) && vertices[i].y > (min_y - 0.01f))
            {
                for (int a = 0; a < n; a++)
                {                    
                    if (vertices[i]==corners[a])    
                    {
                        found = true;
                    }
                }
                if (!found)
                {
                    corners[j] = vertices[i];                     
                    j++;
                    n++;
                }
            }
        }
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
