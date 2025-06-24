using FortnitePorting.RenderingX.Core;

namespace FortnitePorting.RenderingX.Components;

public class TransformComponent : Component
{
    public Vector3 LocalPosition = Vector3.Zero;
    public Quaternion LocalRotation = Quaternion.Identity;
    public Vector3 LocalScale = Vector3.One;
    
    public Vector3 WorldPosition
    {
        get
        {
            if (Owner.Parent == null)
                return LocalPosition;
            
            var parentTransform = Owner.Parent.GetComponent<TransformComponent>();
            if (parentTransform == null)
                return LocalPosition;
                
            return Vector3.TransformPosition(LocalPosition, parentTransform.GetWorldMatrix());
        }
    }
    
    
    public Matrix4 GetLocalMatrix()
    {
        var translation = Matrix4.CreateTranslation(LocalPosition);
        var rotation = Matrix4.CreateFromQuaternion(LocalRotation);
        var scale = Matrix4.CreateScale(LocalScale);
        
        return scale * rotation * translation;
    }

    public Matrix4 GetWorldMatrix()
    {
        if (GetParentTransform() is not { } parentTransform)
            return GetLocalMatrix();
            
        return GetLocalMatrix() * parentTransform.GetWorldMatrix();
    }
    
    private TransformComponent? GetParentTransform()
    {
        var currentParent = Owner.Parent;
        
        while (currentParent != null)
        {
            var transform = currentParent.GetComponent<TransformComponent>();
            if (transform != null)
                return transform;
                
            currentParent = currentParent.Parent;
        }
        
        return null;
    }
}