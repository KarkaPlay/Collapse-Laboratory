using System.Collections.Generic;
using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDeath : MonoBehaviour
{
    public Animation GameOverScreen;
    public TextMeshProUGUI GameOverText;

    public int currentRoom = 0;
    public bool isDead = false;

    public List<Transform> respawnPoints = new List<Transform>();

    public void SetRoom(int room) => currentRoom = room;

    public void Die(string deathText = "Вы погибли...")
    {
        GameOverScreen.gameObject.SetActive(true);
        GameOverScreen.Play();
        GameOverText.text = deathText;
        GetComponent<FirstPersonController>().enabled = false;
        GetComponent<StarterAssetsInputs>().enabled = false;
        GetComponent<CharacterController>().enabled = false;
        isDead = true;
    }

    public void Respawn()
    {
        transform.position = respawnPoints[currentRoom-1].position;
        transform.rotation = respawnPoints[currentRoom-1].rotation;
        GameOverScreen.gameObject.SetActive(false);
        GetComponent<FirstPersonController>().enabled = true;
        GetComponent<StarterAssetsInputs>().enabled = true;
        GetComponent<CharacterController>().enabled = true;
        isDead = false;
    }

    public void OnRespawn()
    {
        if (isDead)
        {
            Respawn();
        }
    }
}