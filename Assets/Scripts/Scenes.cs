using UnityEngine;

public static class Scenes
{
    public const string NONE = "None";
    public const string LOAD = "00_LoadingScene";
    public const string START = "01_StartScene";
    public const string TUTORIAL = "02_TutorialScene";
    public const string STEP1 = "03_Step1Scene";
    public const string STEP2 = "04_Step2Scene";
    public const string FINAL = "05_FinalStepScene";
    public const string TEST = "test_hallway";
}

public enum AchievementKey
{
    NONE = 0,
    ALIVE = 1,
    POTENTIAL = 2,
    STRONGER = 3,
    HIDDEN = 4,
}