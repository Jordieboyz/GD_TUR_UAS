using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : NetworkBehaviour
{
    public static Tilemap borders = null; // Reference to the tilemap
    public static int nPlayers { get => players.Count; }
    public static List<PlayerController> players = new();

    // kepe track of the total amount of PlayerController objects in the scene
    private static float moveInterval = 0.01f; // Time between moves
    private static Color[] pColor = new Color[4] { Color.magenta, Color.green, Color.blue, Color.yellow };

    public Vector3Int currentGridPosition; // Player's current grid position
    public Color color;
    public int ID = -1;

    private Coroutine smoothMovementCoroutine = null; // Reference to the smooth movement coroutine
    private Vector3Int direction;

    private Vector3Int oldPos;
    public event Action<PlayerController, Vector3Int> PositionChanged;
    
    public override void OnNetworkSpawn()
    {
        ID = nPlayers;
        name = "Player_"+ID;
        color =pColor[(int)ID];

        players.Add(this);
        DontDestroyOnLoad(this);
        oldPos = currentGridPosition; // Initialize oldPosition
        Disable();
    }

    public override void OnNetworkDespawn(){
        if(players.Contains(this)){
            players.Remove(this);
        }
    }

    public override void OnDestroy(){
        if(players.Contains(this)){
            players.Remove(this);
        }
    }

    private void Start(){
        if(ID == -1)
            ID = nPlayers;
        name = "Player_"+ID;
        color =pColor[ID];

        if(!players.Contains(this)) players.Add(this);
        oldPos = currentGridPosition; // Initialize oldPosition
        Disable();
    }

    private void Update()
    {   
        // Check for position changes
        Vector3Int currGridPos = Vector3Int.FloorToInt(transform.position);
        if (currGridPos != oldPos)
        {
            PositionChanged?.Invoke(this, currGridPos);
            oldPos = currGridPos;
        }

        if(nPlayers == 1){ 
            if(IsLocked) return;
        } else {
            if(!IsOwner || IsLocked) return;
        }

        direction = Vector3Int.zero;

        // Get input and set direction
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            direction = Vector3Int.up;
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            direction = Vector3Int.down;
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            direction = Vector3Int.left;
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            direction = Vector3Int.right;
        }

        // Start moving when there is input
        if (direction != Vector3Int.zero )
        {
            AttemptMove(direction);
        }
    }

    public Vector3Int GetPlayerPosition(){
        return currentGridPosition;
    }

    private void AttemptMove(Vector3Int direction)
    {
        if (IsLocked) return; // Do nothing if the player is locked

        // Calculate target position
        Vector3Int targetGridPosition = currentGridPosition + direction;
        Vector3 targetWorldPosition = borders.GetCellCenterWorld(targetGridPosition);
        
        // Check if the tile at the target position is not null (i.e., there is a tile at that position)
        if (borders.GetTile(targetGridPosition) == null)
        {
            // Smoothly move the player to the target position
            smoothMovementCoroutine ??= StartCoroutine(SmoothMove(targetWorldPosition, targetGridPosition));
        }
    }

    public void SetPosition(Vector3Int newPos){
        transform.position = new Vector3(newPos.x*transform.localScale.x,newPos.y*transform.localScale.y);
        currentGridPosition = new Vector3Int((int)transform.position.x, (int)transform.position.y);
        UpdateGridPosition();
        PositionChanged?.Invoke(this, newPos);
    }

    private IEnumerator SmoothMove(Vector3 targetWorldPosition, Vector3Int targetGridPosition)
    {
        LockPlayer();

        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < moveInterval)
        {
            LockPlayer();

            transform.position = Vector3.Lerp(startPosition, targetWorldPosition, elapsedTime / moveInterval);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Snap to the exact target position at the end
        transform.position = targetWorldPosition;

        // Update grid position
        currentGridPosition = targetGridPosition;

        PositionChanged?.Invoke(this, currentGridPosition);

        smoothMovementCoroutine = null;

        UnlockPlayer();
    }

    private void UpdateGridPosition(){
        currentGridPosition = borders.WorldToCell(transform.position);
        transform.position = borders.GetCellCenterWorld(currentGridPosition);
    }

    public void Enable(){
        UnlockPlayer();
        GetComponent<SpriteRenderer>().color = color;
    }

    public void Disable(){
        LockPlayer();
        GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0);
    }

    public bool IsLocked { get; private set; } = false;

    public void LockPlayer()
    {
        IsLocked = true;
    }

    public void UnlockPlayer()
    {
        IsLocked = false;
    }
}
