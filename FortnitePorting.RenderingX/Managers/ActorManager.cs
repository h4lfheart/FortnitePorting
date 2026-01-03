using FortnitePorting.RenderingX.Core;

namespace FortnitePorting.RenderingX.Managers;

public class ActorManager : Manager
{
    public T CreateActor<T>(string name = "Actor") where T : Actor, new()
    {
        var actor = new T
        {
            Name = name,
            SceneRef = SceneRef
        };
        
        actor.Initialize();
        
        SceneRef?.Root.AddChild(actor);
        return actor;
    }
    
    public Actor CreateActor(string name = "Actor")
    {
        return CreateActor<Actor>(name);
    }
}