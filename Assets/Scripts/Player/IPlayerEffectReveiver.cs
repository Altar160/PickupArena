public interface IPlayerEffectReceiver
{
    void GivePoints(int points);

    void ApplySpeedBoost(float multiplier, float duration);

    void ApplyFreeze(float duration);
}