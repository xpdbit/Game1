namespace Game1.Core.GameLoop
{
    /// <summary>
    /// Interface for the main game runner that orchestrates game loop and player lifecycle.
    /// Implemented by GameLoopManager.
    /// </summary>
    public interface IGameRunner
    {
        /// <summary>
        /// Called once per frame to update all game systems.
        /// </summary>
        void Tick();

        /// <summary>
        /// Initialize the game runner and all dependent systems.
        /// Called during game startup.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Get the PlayerActor instance owned by this runner.
        /// </summary>
        PlayerActor GetPlayerActor();

        /// <summary>
        /// Check if the game runner has been initialized.
        /// </summary>
        bool IsInitialized { get; }
    }
}
