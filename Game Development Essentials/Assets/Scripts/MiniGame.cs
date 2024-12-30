using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public abstract class MiniGame : NetworkBehaviour
{
    protected MiniGameManager mgMan;
    protected PlayerController player;
    protected int[] rewards;

    // Start
    public abstract Task StartGame();

    // Update
    public abstract Task Run();
    // public abstract int GetReward();

    // Constructor remains protected, or you could use Initialize.
    protected MiniGame() { }

    // Initialization method
    public virtual void Initialize(MiniGameManager miniGameManager, PlayerController _player)
    {
        if (miniGameManager == null)
            throw new ArgumentNullException(nameof(miniGameManager) + " cannot be null");

        mgMan = miniGameManager;
        player = _player;
    }
}
