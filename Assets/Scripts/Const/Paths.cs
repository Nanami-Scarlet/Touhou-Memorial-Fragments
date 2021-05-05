using UnityEngine;

public class Paths
{
    /// <summary>
    /// Prefab路径
    /// </summary>
    private const string PREFAB_FOLDER = "Prefab/";
    private const string PREFAB_UI_FOLDER = PREFAB_FOLDER + "UI/";
    private const string PREFAB_ITEM_FOLDER = PREFAB_FOLDER + "Item/";
    private const string PREFAB_BULLET_SETTING_FOLDER = PREFAB_FOLDER + "BulletSetting/";

    public const string PREFAB_START_VIEW = PREFAB_UI_FOLDER + "StartView";
    public const string PREFAB_DEGREE_VIEW = PREFAB_UI_FOLDER + "DegreeView";
    public const string PREFAB_PLAYER_VIEW = PREFAB_UI_FOLDER + "PlayerView";
    public const string PREFAB_GAME_VIEW = PREFAB_UI_FOLDER + "GameView";
    public const string PREFAB_PAUSE_VIEW = PREFAB_UI_FOLDER + "PauseView";
    public const string PREFAB_DYNAMIC_VIEW = PREFAB_UI_FOLDER + "DynamicView";
    public const string PREFAB_STAGE_LABEL = PREFAB_UI_FOLDER + "Stage";

    public const string PREFAB_ITEM_P = PREFAB_ITEM_FOLDER + "P";
    public const string PREFAB_ITEM_POINT = PREFAB_ITEM_FOLDER + "Point";

    private const string PREFAB_GAME_FOLDER = PREFAB_FOLDER + "Game/";
    private const string PREFAB_PLAYER_FOLDER = PREFAB_GAME_FOLDER + "Player/";
    private const string PREFAB_ENEMY_FOLDER = PREFAB_GAME_FOLDER + "Enemy/";
    public const string PREFAB_PLAYER = PREFAB_PLAYER_FOLDER + "Player";

    public const string PREFAB_BULLET_SETTING = PREFAB_BULLET_SETTING_FOLDER + "BulletSetting";

    public const string ASSET_BULLET_FOLDER = "Bullet/";

    public const string PREFAB_ENEMY1 = PREFAB_ENEMY_FOLDER + "1";
    public const string PREFAB_ENEMY2 = PREFAB_ENEMY_FOLDER + "2";
    public const string PREFAB_ENEMY3 = PREFAB_ENEMY_FOLDER + "3";
    public const string PREFAB_ENEMY4 = PREFAB_ENEMY_FOLDER + "4";
    public const string PREFAB_ENEMY5 = PREFAB_ENEMY_FOLDER + "5";
    public const string PREFAB_ENEMY6 = PREFAB_ENEMY_FOLDER + "6";
    public const string PREFAB_ENEMY7 = PREFAB_ENEMY_FOLDER + "7";
    public const string PREFAB_ENEMY8 = PREFAB_ENEMY_FOLDER + "8";
    public const string PREFAB_ENEMY9 = PREFAB_ENEMY_FOLDER + "9";
    public const string PREFAB_ENEMY10 = PREFAB_ENEMY_FOLDER + "10";
    public const string PREFAB_ENEMY11 = PREFAB_ENEMY_FOLDER + "11";

    /// <summary>
    /// 音频路径
    /// </summary>
    public const string AUDIO_FOLDER = "Audio/";

    /// <summary>
    /// 一些音频的名称
    /// </summary>
    public const string AUDIO_TITLE_BGM = "Title_BGM";
    public const string AUDIO_SELECT_EFF = "Select_Eff";
    public const string AUDIO_SURE_EFF = "Sure_Eff";
    public const string AUDIO_CANCAL_EFF = "Cancal_Eff";
    public const string AUDIO_PAUSE_EFF = "Pause_Eff";
    public const string AUDIO_ITEM_EFF = "Item_Eff";
    public const string AUDIO_EDEATH_EFF = "EDeath_Eff";
    public const string AUDIO_EXTEND_EFF = "Extend_Eff";
    public const string AUDIO_GET_BOMB_EFF = "Card_Eff";
    public const string AUDIO_GRAZE_EFF = "Graze_Eff";
    public const string AUDIO_DEATH_EFF = "Death_Eff";
    public const string AUDIO_BOMB_EFF = "Bomb_Eff";

    /// <summary>
    /// 配置文件路径
    /// </summary>
    private static readonly string CONFIG_FOLDER = "Config/";
    public static readonly string CONFIG_ENEMY = CONFIG_FOLDER + "EnemyConfig";
    public static readonly string CONFIG_VOLUME = CONFIG_FOLDER + "VolumeConfig";
    public static readonly string CONFIG_BGM = CONFIG_FOLDER + "BGMConfig";
}
