using FortnitePorting.RenderingX.Core;

namespace FortnitePorting.RenderingX.Components;

public class TransformComponent : Component
{
    public Vector3 LocalPosition = Vector3.Zero;
    public Quaternion LocalRotation = Quaternion.Identity;
    public Vector3 LocalScale = Vector3.One;
    
    public Vector3 WorldPosition()
    {
        if (Owner?.Parent is null)
            return LocalPosition;
            
        var parentTransform = Owner.Parent?.GetComponent<TransformComponent>();
        if (parentTransform is null)
            return LocalPosition;
                
        return Vector3.TransformPosition(LocalPosition, parentTransform.WorldMatrix());
    }
    
    public Matrix4 LocalMatrix()
    {
        var translation = Matrix4.CreateTranslation(LocalPosition);
        var rotation = Matrix4.CreateFromQuaternion(LocalRotation);
        var scale = Matrix4.CreateScale(LocalScale);
        
        return scale * rotation * translation;
    }

    public Matrix4 WorldMatrix()
    {
        if (ParentTransform() is not { } parentTransform)
            return LocalMatrix();
            
        return LocalMatrix() * parentTransform.WorldMatrix();
    }
    
    private TransformComponent? ParentTransform()
    {
        var currentParent = Owner?.Parent;
        
        while (currentParent is not null)
        {
            var transform = currentParent.GetComponent<TransformComponent>();
            if (transform is not null)
                return transform;
                
            currentParent = currentParent.Parent;
        }
        
        return null;
    }
}