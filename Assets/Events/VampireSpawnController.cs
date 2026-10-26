using UnityEngine;

public class VampireSpawnController : MonoBehaviour
{
    [SerializeField] private GameObject vampireSpawn;
    [SerializeField] private GameObject vampirePrefab;
    [SerializeField] private GameObject doorToOpen;
    private void Start()
    {
       
    }

    private void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Spawn the vampire when the player enters the trigger
            SpawnVampire();
            // Disable the trigger after spawning to prevent multiple spawns
            GetComponent<Collider>().enabled = false;
            // Open the locked door
            OpenLockedDoor();
        }
    }

    private void SpawnVampire()
    {
        if (vampirePrefab != null && vampireSpawn != null)
        {
            Instantiate(vampirePrefab, vampireSpawn.transform.position, vampireSpawn.transform.rotation);
        }
    }

    private void OpenLockedDoor()
    {
        if (doorToOpen != null)
        {
            // Assuming the door has a script with an OpenDoor method
            DoorController doorController = doorToOpen.GetComponent<DoorController>();
            if (doorController != null)
            {
                doorController.isLocked = false; // Unlock the door

                doorController.OpenDoor();
            }
        }
    }
}
