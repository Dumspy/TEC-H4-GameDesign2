public static class GameModeManager
{
    public enum GameMode
    {
        Singleplayer,
        Host,
        Client
    }

    public static GameMode SelectedMode = GameMode.Singleplayer;
}
