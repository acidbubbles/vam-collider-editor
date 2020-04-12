using System;

public class RigidBodyDisabler : MVRScript
{
    public override void Init()
    {
        try
        {
            SuperController.LogMessage("Plugin installed");
        }
        catch (Exception e)
        {
            SuperController.LogError("Failed to initialize plugin: " + e);
        }
    }
}
