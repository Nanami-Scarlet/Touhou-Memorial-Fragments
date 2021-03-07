using UnityEngine;

public class Paths
{
    /// <summary>
    /// Prefab路径
    /// </summary>
    private const string PREFAB_FOLDER = "Prefab/";
    private const string PREFAB_UI_FOLDER = PREFAB_FOLDER + "UI/";

    public const string PREFAB_START_VIEW = PREFAB_UI_FOLDER + "StartView";
    public const string PREFAB_DEGREE_VIEW = PREFAB_UI_FOLDER + "DegreeView";
    public const string PREFAB_PLAYER_VIEW = PREFAB_UI_FOLDER + "PlayerView";
    public const string PREFAB_GAME_VIEW = PREFAB_UI_FOLDER + "GameView";
    public const string PREFAB_PAUSE_VIEW = PREFAB_UI_FOLDER + "PauseView";
    public const string PREFAB_STAGE_LABEL = PREFAB_UI_FOLDER + "Stage";

    private const string PREFAB_GAME_FOLDER = PREFAB_FOLDER + "Game/";
    private const string PREFAB_PLAYER_FOLDER = PREFAB_GAME_FOLDER + "Player/";
    private const string PREFAB_ENEMY_FOLDER = PREFAB_GAME_FOLDER + "Enemy/";
    public const string PREFAB_PLAYER = PREFAB_PLAYER_FOLDER + "Player";

    public const string PREFAB_ENEMY1 = PREFAB_ENEMY_FOLDER + "1";
    public const string PREFAB_ENEMY2 = PREFAB_ENEMY_FOLDER + "2";
    public const string PREFAB_ENEMY3 = PREFAB_ENEMY_FOLDER + "3";
    public const string PREFAB_ENEMY4 = PREFAB_ENEMY_FOLDER + "4";
    public const string PREFAB_ENEMY5 = PREFAB_ENEMY_FOLDER + "5";
    public const string PREFAB_ENEMY6 = PREFAB_ENEMY_FOLDER + "6";
    public const string PREFAB_ENEMY7 = PREFAB_ENEMY_FOLDER + "7";
    public const string PREFAB_ENEMY8 = PREFAB_ENEMY_FOLDER + "8";
    public const string PREFAB_ENEMY9 = PREFAB_ENEMY_FOLDER + "9";

    /// <summary>
    /// 音频路径
    /// </summary>
    public const string AUDIO_FOLDER = "Audio/";

    /// <summary>
    /// 一些音频的名称
    /// </summary>
    public const string AUDIO_TITLE_BGM = "Title_BGM";
    public const string AUDIO_ONE_ONE_BGM = "One_One_BGM";
    public const string AUDIO_SELECT_EFF = "Select_Eff";
    public const string AUDIO_SURE_EFF = "Sure_Eff";
    public const string AUDIO_CANCAL_EFF = "Cancal_Eff";
    public const string AUDIO_PAUSE_EFF = "Pause_Eff";

    /// <summary>
    /// 配置文件路径
    /// </summary>
    private static readonly string CONFIG_FOLDER = "Config/";
    public static readonly string CONFIG_ENEMY = CONFIG_FOLDER + "EnemyConfig";
}
