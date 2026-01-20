namespace TimerMod.Triggers;

public interface Trigger
{
    bool active();
    void destroy();
}
