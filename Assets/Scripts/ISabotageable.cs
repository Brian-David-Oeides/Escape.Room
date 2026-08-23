public enum SabotageLineCategory { Lever, Switch, Valve }

public interface ISabotageable
{
    string PuzzleID { get; }
    SabotageLineCategory VoiceLineCategory { get; }
    void Sabotage();
}
