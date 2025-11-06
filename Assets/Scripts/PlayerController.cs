using UnityEngine;

public class PlayerController : Singleton<PlayerController>
{
    [SerializeField] private float speed = 5f;
    private Vector3 currentDir = Vector3.forward;
    // private bool canTurn = false;
    // [SerializeField] private PathSegment nextTurnSegment;
    // private MapGenerator mapGen;

    void Start()
    {
        // mapGen = FindObjectOfType<MapGenerator>();
    }

    void Update()
    {
        // transform.position += currentDir * speed * Time.deltaTime;

        // if (Input.GetMouseButtonDown(0))
        // {
        //     Debug.Log("======Turn");
        //     currentDir = nextTurnSegment.nextDirection;
        //     mapGen.ChangeDirection(currentDir);
        //     transform.rotation = Quaternion.LookRotation(currentDir);
        //     canTurn = false;
        // }
    }
    void OnTriggerEnter(Collider other)
    {
        // var seg = other.GetComponent<PathSegment>();
        // if (seg != null && seg.turnType != PathSegment.TurnType.None)
        // {
        //     canTurn = true;
        //     nextTurnSegment = seg;
        // }
    }

    void OnTriggerExit(Collider other)
    {
        // if (other.GetComponent<PathSegment>() == nextTurnSegment) canTurn = false;
        // Debug.Log("End Game");
    }
}
