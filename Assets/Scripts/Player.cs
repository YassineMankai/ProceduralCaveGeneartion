using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    Rigidbody rigidBody;
    Vector3 velocity;
    MapGenerator mapGenerator;
    MeshGenerator meshGenerator;
    // Start is called before the first frame update

    bool[] visitedRooms;
    int nbVisitedRooms;
    void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        mapGenerator = GameObject.Find("Map").GetComponent<MapGenerator>();
        meshGenerator = GameObject.Find("Map").GetComponent<MeshGenerator>();
        visitedRooms = new bool[mapGenerator.GetNbRooms()];
        nbVisitedRooms = 0;
        Debug.Log($"total number of rooms {visitedRooms.Length}");
    }

    // Update is called once per frame
    void Update()
    {
        velocity = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized * 10;
        int room = mapGenerator.tileToRoom(meshGenerator.WorldPosToMapCoord(rigidBody.position));
        if (room>=1 && room <= visitedRooms.Length && !visitedRooms[room - 1])
        {
            nbVisitedRooms++;
            visitedRooms[room - 1] = true;
            meshGenerator.MarkRoom(room);
        }
        if (nbVisitedRooms == visitedRooms.Length)
        {
            Debug.Log("Victory");
        }
    }

    private void FixedUpdate()
    {
        rigidBody.MovePosition(rigidBody.position + velocity * Time.fixedDeltaTime);
    }
}
