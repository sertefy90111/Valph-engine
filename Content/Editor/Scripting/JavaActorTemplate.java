%copyright%package %package%;

import com.flaxengine.Actor;

/// <summary>
/// %class% Actor.
/// </summary>
public class %class% extends Actor
{
    @Override
    public void onBeginPlay()
    {
        super.onBeginPlay();
        // Called when the Actor is added to the game. This also runs during edit time.
    }

    @Override
    public void onEndPlay()
    {
        super.onEndPlay();
        // Called when the Actor is removed from the game.
    }

    @Override
    public void onEnable()
    {
        super.onEnable();
        // Called when the Actor is enabled.
    }

    @Override
    public void onDisable()
    {
        super.onDisable();
        // Called when the Actor is disabled.
    }
}
