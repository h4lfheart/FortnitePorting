using FortnitePorting.RenderingX.Core;

namespace FortnitePorting.RenderingX.Components;

public class SpatialComponent : Component
{
    public SpatialComponent(string name = "Component", Transform? transform = null) : base(name)
    {
        Transform = transform ?? Transform.Identity;
    }
    
    public Transform Transform;

    public Vector3 WorldPosition
    {
        get
        {
            if (Actor.Parent?.RootComponent is not { } parentRootComponent)
                return Transform.Position;
                
            return Vector3.TransformPosition(Transform.Position, parentRootComponent.WorldMatrix);
        }
    }
    
    public Matrix4 LocalMatrix
    {
        get
        {
            var translation = Matrix4.CreateTranslation(Transform.Position);
            var rotation = Matrix4.CreateFromQuaternion(Transform.Rotation);
            var scale = Matrix4.CreateScale(Transform.Scale);
        
            return scale * rotation * translation;
        }
    }

    public Matrix4 WorldMatrix
    {
        get
        {
            if (GetParentRootComponent() is not { } parentTransform)
                return LocalMatrix;
            
            return LocalMatrix * parentTransform.WorldMatrix;
        }
    }
    
    private SpatialComponent? GetParentRootComponent()
    {
        var currentParent = Actor?.Parent;
        
        while (currentParent is not null)
        {
            if (currentParent.RootComponent is { } parentRootComponent)
                return parentRootComponent;
                
            currentParent = currentParent.Parent;
        }
        
        return null;
    }
}