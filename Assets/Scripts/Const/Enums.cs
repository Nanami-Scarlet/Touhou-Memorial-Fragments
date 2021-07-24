public enum SceneName
{
    Main,
    Game,
    COUNT
}

public enum Player
{
    player_0,
    player_1,
}

public enum Degree
{
    NORMAL,
    LUNATIC
}

public enum GameStatus
{
    Gameing,
    Pause,          //为了方便，UI界面统称Pause
    Chating
}

public enum TimeUnit
{
    MilliSecond,
    Second,
}

public enum InputState
{
    DOWN,
    UP,
    PRESS,
    NONE            //默认为UI按键
}

public enum NormalStage
{
    stage1_1,
    stage1_2,
    //stage2_1,
    COUNT
}

public enum BossStage
{
    stage_B1,
    stage_B2,
    //stage_B3,
    stage_B4,
    COUNT
}

public enum EliteStage
{
    stage2_2,
    COUNT
}

public enum ChatStage
{
    Ending,
    COUNT
}

public enum PlayerState
{
    NORMAL,
    INVINCIBLE
}

public enum AudioType
{
    Graze,
    Items,
    PlayerDead,
    EnemyDead,
    GetBomb,
    ReleaseBomb,
    Extend,
    TimeOut,
    Card,
    TimeUP,
    GetFinalCard,
    Bonus,
    COUNT
}

public enum Mode
{
    NORMAL,
    LUNATIC,
    PRACTICE,
    SPELL
}