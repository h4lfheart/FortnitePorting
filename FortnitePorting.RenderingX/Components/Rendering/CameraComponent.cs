using FortnitePorting.RenderingX.Core;

namespace FortnitePorting.RenderingX.Components.Rendering;

public class CameraComponent : SpatialComponent
{
    public Vector3 Direction = Vector3.UnitX;
    public Vector3 Up = Vector3.UnitY;
    public float FieldOfView = 60.0f;
    public float AspectRatio = 16.0f / 9.0f;
    public float NearPlane = 0.1f;
    public float FarPlane = 10000.0f;
    
    public float Speed = 0.05f;
    public float Sensitivity = 0.5f;
    
    public void UpdateDirection(float deltaX, float deltaY)
    {
        deltaX *= Sensitivity * 0.01f;
        deltaY *= Sensitivity * 0.01f;

        var right = Vector3.Normalize(Vector3.Cross(Direction, Up));
        
        var yawRotation = Matrix4.CreateRotationY(-deltaX);
        var pitchRotation = Matrix4.CreateFromAxisAngle(right, -deltaY);
        
        var newDirection = Vector3.TransformNormal(Direction, pitchRotation * yawRotation);
        
        var upDot = Vector3.Dot(newDirection, Up);
        if (Math.Abs(upDot) < 0.99f)
        {
            Direction = Vector3.Normalize(newDirection);
        }
    }
    
    public void LookAt(Vector3 targetPosition)
    {
        Direction = Vector3.Normalize(targetPosition - WorldPosition());
    }

    public Matrix4 ViewMatrix()
    {
        return Matrix4.LookAt(WorldPosition(), WorldPosition() + Direction, Up);
    }

    public Matrix4 ProjectionMatrix()
    {
        return Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(FieldOfView),
            AspectRatio,
            NearPlane,
            FarPlane);
    }
}