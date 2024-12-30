using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;
using Debug = UnityEngine.Debug;


public class Cross : MiniGame
{
    // List to generate identical lines between clients
    public NetworkList<Vector3Int> networkedLinePath;
    public List<Vector3Int> singlePlayerLinePath;

    // List to keep track of players who are already done    
    public NetworkList<int> PlayersDone;

    // flags to keep track of general game lifecycle
    private bool AllPlayersDone { get => PlayersDone.Count == PlayerController.nPlayers; }
    private bool LineIsGenerated { get => networkedLinePath.Count != 0; }
    private bool singlePlayerLineIsGenerated { get => singlePlayerLinePath.Count != 0; }


    // generate all anchorpoints on every client (might have to do this on server)
    private List<(Vector3Int start, Vector3Int size)> anchorPoints = new();
    private Vector3Int startPos;
    private Vector3Int gridSize;

    // reference to the colormap we'll use to color the grid (no collisions on this grid)
    private Tilemap ColorMap;

    // Maximum Time for the game to keep playing in seconds
    private double timeLimit = 10f;
    private Stopwatch gameTimer = new();

    Color[] rColors;
    // Networklist have to be "initialized" during Awake
    private void Awake(){
        networkedLinePath ??= new();
        PlayersDone ??= new();
    }

    // Don't leave traces behind, this should only be run by the host!
    public override void OnDestroy()
    {
        // destroy the local colorGrid
        Destroy(ColorMap.gameObject);

        // leave no anchor points behind
        anchorPoints.Clear();

        if(!mgMan.SinglePlayer){
            // clean network variables/lists
            if(networkedLinePath != null){
                networkedLinePath.Dispose();
                networkedLinePath = null;
            }
            if(PlayersDone != null){
                PlayersDone.Dispose();
                PlayersDone = null;
            }
            
            // Telle the MiniGameManager that this game is done
            mgMan.currGameDone = true;
        }

    }

    // Initialize the game
    public override void Initialize(MiniGameManager mgManRef, PlayerController _playerRef){ 
        base.Initialize(mgManRef, _playerRef);
        rColors = new Color[3] { new(1, .84f, 0), Color.gray, new(0.8f, .5f, .2f, 1) };
        ColorMap = mgMan.AddAndGetTilemap("ColorMap");

        foreach(var ply in PlayerController.players){
            anchorPoints.Add( RetrievePlayerAnchors(ply.ID));
        }

        startPos = anchorPoints[player.ID].start;
        gridSize = anchorPoints[player.ID].size-startPos;

        timeLimit = 60f; 

        // only the host should modify the networklist
        if (IsHost)
        {
            if(LineIsGenerated == false){
                GenerateLine();
            }
        }
        if(mgMan.SinglePlayer){
            GenerateSinglePlayerLine();
        }
    }

    public override async Task StartGame()
    {   

        // Generate all player borderes at the same time
        await GenerateBorders(Color.white);
        
        // wait for the host to finish generating the line
        if(mgMan.SinglePlayer){
            while (singlePlayerLineIsGenerated == false){ 
                await WaitMsAsync(1);
            }
            // Gradually show the generated line
            await PaintLine(100, player.color, player.ID);
            
            // time to analyze the path in seconds
            await WaitAsync(2);

            // Gradually remove the generated line
            await PaintLine(100, null, player.ID);
        } else {
            while (LineIsGenerated == false){ 
                await WaitMsAsync(1);
            }

            // Gradually show the generated line
            await PaintLines(100/PlayerController.nPlayers);

            // time to analyze the path in seconds
            await WaitAsync(2);

            // Gradually remove the generated line
            await PaintLines(100/PlayerController.nPlayers, null);
        }

        

        // Runt the gameplay logic
        await Run();

        if(!mgMan.SinglePlayer){
            while(!AllPlayersDone){
                await WaitMsAsync(1);
            }
            
            // give all clients some time to calculate the final things. and finish the borders
            await WaitAsync(3);
        } else {
            mgMan.currGameDone = true;
        }

        // make sure we reset the players
        DisablePlayerListeners();

    }

    public override async Task Run()
    {   
        EnablePlayerListeners();

        StartGameTimer();

        if(!mgMan.SinglePlayer){
            // Quit the game if the timelimit is reached or if the player reached the last tile        
            while(gameTimer.Elapsed.TotalSeconds < timeLimit && 
                        GetLocalPlayerPosOnLine() != networkedLinePath[^1])
            {
                // if the player steps off the line, respawn the player after .5 seconds
                if(!networkedLinePath.Contains(GetLocalPlayerPosOnLine())){
                    player.Disable();
                    await WaitMsAsync(500);
                    Respawn();
                }
                await Task.Yield();
            }
            // let the server know this client is done.
            PlayerDoneServerRpc(player.ID);

            // Stop the timer;
            StopGameTimer();

            // prevent player to move after they finish
            player.Disable();
        } else {
            // Quit the game if the timelimit is reached or if the player reached the last tile        
            while(gameTimer.Elapsed.TotalSeconds < timeLimit && 
                        GetLocalPlayerPosOnLine() != singlePlayerLinePath[^1])
            {
                // if the player steps off the line, respawn the player after .5 seconds
                if(!singlePlayerLinePath.Contains(GetLocalPlayerPosOnLine())){
                    player.Disable();
                    await WaitMsAsync(500);
                    Respawn();
                }
                await Task.Yield();
            }
            

            // Stop the timer;
            StopGameTimer();

            // prevent player to move after they finish
            player.Disable();
        }
    }
    
    private Vector3Int GetPlayerPosOnLine(Vector3Int pPos, int pID){
        return pPos - anchorPoints[pID].start;
    }
    private Vector3Int GetPlayerPosOnLine(PlayerController ply){
        return ply.GetPlayerPosition() - anchorPoints[ply.ID].start;
    }
    private Vector3Int GetLocalPlayerPosOnLine(){
        return player.GetPlayerPosition() - startPos;
    }
    
    private void UpdatePlayers(PlayerController ply, Vector3Int newPlayerPos)
    {
        if(!mgMan.SinglePlayer){
            if (!networkedLinePath.Contains(GetPlayerPosOnLine(newPlayerPos, ply.ID)))
            {
                PaintColorMap(newPlayerPos, mgMan.CrossWrongTile);
            }
            else
            {
                PaintColorMap(newPlayerPos, Color.white);
                if(GetPlayerPosOnLine(newPlayerPos, ply.ID) == networkedLinePath[^1])
                {
                    ply.Disable();
                }
            }
        } else {
            if (!singlePlayerLinePath.Contains(GetPlayerPosOnLine(newPlayerPos, ply.ID)))
            {
                PaintColorMap(newPlayerPos, mgMan.CrossWrongTile);
            }
            else
            {
                PaintColorMap(newPlayerPos, Color.white);
                if(GetPlayerPosOnLine(newPlayerPos, ply.ID) == singlePlayerLinePath[^1])
                {
                    ply.Disable();
                }
            }
        }
    }

    private void Respawn()
    {
        if(mgMan.SinglePlayer){
            player.SetPosition(startPos+singlePlayerLinePath[0]);
        } else {
            player.SetPosition(startPos+networkedLinePath[0]);
        }
        player.Enable();
    }

    // Setup all the players
    private void EnablePlayerListeners()
    {
        foreach(var ply in PlayerController.players)
        {
            ply.PositionChanged += UpdatePlayers;
            if(mgMan.SinglePlayer){
                ply.SetPosition(anchorPoints[ply.ID].start+singlePlayerLinePath[0]);
            } else{
                ply.SetPosition(anchorPoints[ply.ID].start+networkedLinePath[0]);
            }
            ply.Enable();
        }
    }

    // Reset all players
    private void DisablePlayerListeners()
    {
        foreach(var ply in PlayerController.players)
        {
            ply.PositionChanged -= UpdatePlayers;
            ply.Disable();
        }
    }

    /********************************************************************
        Line Generation
            Utilities to Generate a line to create basic gameplay. 
            This should only be run by the Host, because of the NetworkList usage
            
    **********************************************************************/
    public void GenerateLine()
    {
        Vector2Int currentPosition = new Vector2Int(gridSize.x / 2, 1);
        networkedLinePath.Add((Vector3Int)currentPosition);

        while (networkedLinePath[^1].y != gridSize.y - 1)
        {
            Vector2Int nextPosition = new();
            bool validPositionFound = false;

            for (int attempt = 0; attempt < 100; attempt++)
            {
                nextPosition = currentPosition + GetRandomDirection();

                if (!networkedLinePath.Contains((Vector3Int)nextPosition) &&
                    IsWithinGrid(nextPosition) &&
                    CountAdjacentTiles(nextPosition) < 2)
                {
                    validPositionFound = true;
                    break;
                }
            }

            if (!validPositionFound)
            {
                Debug.LogWarning("Could not find a valid next position. Terminating generation early. Trying again.");
                break;
            }
            networkedLinePath.Add((Vector3Int)nextPosition);
            currentPosition = nextPosition;
        }
    }

    public void GenerateSinglePlayerLine()
    {
        Vector2Int currentPosition = new Vector2Int(gridSize.x / 2, 1);
        singlePlayerLinePath.Add((Vector3Int)currentPosition);

        while (singlePlayerLinePath[^1].y != gridSize.y - 1)
        {
            Vector2Int nextPosition = new();
            bool validPositionFound = false;

            for (int attempt = 0; attempt < 100; attempt++)
            {
                nextPosition = currentPosition + GetRandomDirection();

                if (!singlePlayerLinePath.Contains((Vector3Int)nextPosition) &&
                    IsWithinGrid(nextPosition) &&
                    CountAdjacentTilesSingle(nextPosition) < 2)
                {
                    validPositionFound = true;
                    break;
                }
            }

            if (!validPositionFound)
            {
                Debug.LogWarning("Could not find a valid next position. Terminating generation early. Trying again.");
                break;
            }
            singlePlayerLinePath.Add((Vector3Int)nextPosition);
            currentPosition = nextPosition;
        }
    }

    private Vector2Int GetRandomDirection()
    {
        return Random.Range(0, 4) switch
        {
            0 => Vector2Int.up,
            3 => Vector2Int.up,
            // 1 => Vector2Int.down,
            2 => Vector2Int.left,
            _ => Vector2Int.right
        };
    }

    private bool IsWithinGrid(Vector2Int position)
    {
        return  position.x > 0 && 
                position.x < gridSize.x && 
                position.y > 0 && 
                position.y < gridSize.y;
    }

    // Count how many adjacent tiles are already part of the line
    private int CountAdjacentTiles(Vector2Int position)
    {
        int count = 0;
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.left, Vector2Int.right, Vector2Int.down};

        foreach (var direction in directions)
        {
            if (networkedLinePath.Contains((Vector3Int)(position + direction)))
            {
                count++;
            }
        }

        return count;
    }

        // Count how many adjacent tiles are already part of the line
    private int CountAdjacentTilesSingle(Vector2Int position)
    {
        int count = 0;
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.left, Vector2Int.right, Vector2Int.down};

        foreach (var direction in directions)
        {
            if (singlePlayerLinePath.Contains((Vector3Int)(position + direction)))
            {
                count++;
            }
        }

        return count;
    }


    private async Task PaintLine(float delayPerTile, Tile t, int singlePlayerID)
    {
        await PaintLines(delayPerTile, t, singlePlayerID);
    }
    
    private async Task PaintLines(float delayPerTile, Tile t, int singlePlayerID = -1)
    {
        if(mgMan.SinglePlayer){
            foreach(var linePos in singlePlayerLinePath)
            {
                PaintColorMap(anchorPoints[singlePlayerID].start+linePos, t);
                await WaitMsAsync(delayPerTile);
            }
        } else {
            bool runForSinglePlayer = singlePlayerID != -1;
            
            foreach(var linePos in networkedLinePath)
            {
                if(runForSinglePlayer)
                {
                    PaintColorMap(anchorPoints[singlePlayerID].start+linePos, t);
                    await WaitMsAsync(delayPerTile);
                } 
                else 
                {
                    foreach(var ply in PlayerController.players)
                    {
                        PaintColorMap(anchorPoints[ply.ID].start+linePos, t);
                        await WaitMsAsync(delayPerTile);
                    }
                }
            }
        }
    }

    private async Task PaintLine(float delayPerTile, Color c, int singlePlayerID){
        await PaintLines(delayPerTile, c, singlePlayerID);
    }
    private async Task PaintLines(float delayPerTile, Color c = default, int singlePlayerID = -1){
        if(!mgMan.SinglePlayer){
            bool runForSinglePlayer = singlePlayerID != -1;

            foreach(var linePos in networkedLinePath)
            {
                if(runForSinglePlayer)
                {
                    if(c == default){
                        PaintColorMap(anchorPoints[singlePlayerID].start+linePos, player.color);
                    } else {
                        PaintColorMap(anchorPoints[singlePlayerID].start+linePos, c);
                    }
                    await WaitMsAsync(delayPerTile);
                } 
                else 
                {
                    foreach(var ply in PlayerController.players)
                    {
                        if(c == default){
                            PaintColorMap(anchorPoints[ply.ID].start+linePos, ply.color);
                        } else {
                            PaintColorMap(anchorPoints[ply.ID].start+linePos, c);
                        }
                        await WaitMsAsync(delayPerTile);
                    }
                }
            }
        } else {
            foreach(var linePos in singlePlayerLinePath)
            {
                if(c == default){
                    PaintColorMap(anchorPoints[singlePlayerID].start+linePos, player.color);
                } else {
                    PaintColorMap(anchorPoints[singlePlayerID].start+linePos, c);
                }
                await WaitMsAsync(delayPerTile);
            }
        }
    }

    /********************************************************************
        Gameplay Timer
            Utilities to start, stop and update the timer during the game

    **********************************************************************/
    private void StartGameTimer()   
    {
        gameTimer.Start();
        mgMan.TimerTextField.gameObject.SetActive(true);

        // this will keep running until the GameTimer stops
        UpdateTimer();
    }

    // Keep updating the Game Timer async (!!!)
    private async void UpdateTimer()
    {
        double remainingTime;
        while(gameTimer.IsRunning)
        {
            remainingTime = timeLimit - gameTimer.Elapsed.TotalSeconds;
            mgMan.TimerTextField.text = $"{Mathf.FloorToInt((float)remainingTime):D2}:{Mathf.FloorToInt((float)(remainingTime % 1) * 1000):D3}";
            await Task.Yield();
        }
    }
    private void StopGameTimer()
    {
        gameTimer.Stop();
        mgMan.TimerTextField.gameObject.SetActive(false);
    }


    /********************************************************************
        Visual Effects
            Functionality to gradually generate borders for each player.
            These functions should generate a box which provide collision
            
    **********************************************************************/

    // rColors[PlayersDone.Count], playerID

    public async void GenerateBorder(int prevCount, int pID)
    {
        while(PlayersDone.Count == prevCount){
            await WaitMsAsync(1);
        }
        GenerateBorders(rColors[PlayersDone.Count-1], pID);
        await PaintLine(10, rColors[PlayersDone.Count-1], pID);
    }

    public async Task GenerateBorders(Color borderColor, int singlePlayerID = -1)
    {
        bool runForSinglePlayer = singlePlayerID != -1;
        float ms = 10;
        for (int x = 0; x < gridSize.x; x++) 
        {
            if(runForSinglePlayer)
            {
                PaintGrid(anchorPoints[singlePlayerID].start+new Vector3Int(x, 0), borderColor); // bottom, left-right
                PaintGrid(new Vector3Int(anchorPoints[singlePlayerID].start.x+gridSize.x, anchorPoints[singlePlayerID].start.y+gridSize.y)-new Vector3Int(x, 0), borderColor); // top, right-left
            } 
            else 
            {
                foreach(var ply in PlayerController.players)
                {
                    PaintGrid(anchorPoints[ply.ID].start+new Vector3Int(x, 0), borderColor); // bottom, left-right
                    PaintGrid(new Vector3Int(anchorPoints[ply.ID].start.x+gridSize.x, anchorPoints[ply.ID].start.y+gridSize.y)-new Vector3Int(x, 0), borderColor); // top, right-left
                }
            }
            await WaitMsAsync(ms);
        }

        for (int y = 0; y < gridSize.y; y++) 
        {
            if(runForSinglePlayer)
            {
                PaintGrid(anchorPoints[singlePlayerID].start+new Vector3Int(0, y), borderColor); // Left border
                PaintGrid(new Vector3Int(anchorPoints[singlePlayerID].start.x+gridSize.x, anchorPoints[singlePlayerID].start.y+gridSize.y)-new Vector3Int(0,y), borderColor); // Right border
            } else {
                foreach(var ply in PlayerController.players){
                    PaintGrid(anchorPoints[ply.ID].start+new Vector3Int(0, y), borderColor); // Left border
                    PaintGrid(new Vector3Int(anchorPoints[ply.ID].start.x+gridSize.x, anchorPoints[ply.ID].start.y+gridSize.y)-new Vector3Int(0,y), borderColor); // Right border
                }
            }
            await WaitMsAsync(ms);
        }
    }

    // to correctly calculate the borders and offsets we need this.
    (Vector3Int, Vector3Int) RetrievePlayerAnchors(int playerID)
    {
        var playerObject = GameObject.Find("p"+PlayerController.nPlayers.ToString());
        if(playerObject.transform.childCount != PlayerController.nPlayers && playerObject != null)
        {
            Debug.Log("There might be a problem with the anchor points, children and nPlayers does not match.");
            return default;
        }

        // should return (startingPos, size)
        return (Vector3Int.FloorToInt(playerObject.transform.GetChild(playerID).GetChild(0).transform.position), 
                Vector3Int.FloorToInt(playerObject.transform.GetChild(playerID).GetChild(1).transform.position));
    }

    /********************************************************************
        Waitables
            Non-blocking waitables, so the Unities renderthread won't
            be blocked when we want to wait for something
            
    **********************************************************************/
    public async Task WaitAsync(int seconds)
    {
        await WaitMsAsync(seconds * 1000);
    }

    public async Task WaitMsAsync(float ms)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while ((float)stopwatch.Elapsed.TotalMilliseconds < ms)
        {
            await Task.Delay(1);
        }
    }

    /********************************************************************
        Waitables
            Tilemap Paint utilities to create "colors" on any tilemap.
            Paint either a tile or color a tile.
            
    **********************************************************************/
    void PaintGrid(Vector3Int gridPos, Color c)
    {
        PaintTilemap(mgMan.mainTileGrid, gridPos, mgMan.tile, c);
    }

    void PaintColorMap(Vector3Int gridPos, Color c)
    {
        PaintTilemap(ColorMap, gridPos, mgMan.tile, c);
    }

    void PaintColorMap(Vector3Int gridPos, Tile tile)
    {
        PaintTilemap(ColorMap, gridPos, tile);
    }

    void PaintTilemap(Tilemap grid, Vector3Int gridPos, Tile newTile = null, Color c = default)
    {
        if(newTile == null){
            grid.SetTile(gridPos, null); 
            grid.RefreshTile(gridPos);

        } else {
            if(c != default){
                newTile.color = c;
            }
            grid.SetTile(gridPos, newTile);
            grid.RefreshTile(gridPos);
        }
    }

    /********************************************************************
        Host - Client Logic
            Tell the host that the current client is done witht the game.
            
    **********************************************************************/
    [ServerRpc(RequireOwnership = false)]
    public void PlayerDoneServerRpc(int pID)
    {
        
        if(!PlayersDone.Contains(pID)){
            PlayerDoneClientRpc(PlayersDone.Count, pID); // send back that he can generate his border
            PlayersDone.Add(pID);
        }
    }

    [ClientRpc]
    public void PlayerDoneClientRpc(int prevCount, int playerID)
    {   
        GenerateBorder(prevCount, playerID);
    }
}