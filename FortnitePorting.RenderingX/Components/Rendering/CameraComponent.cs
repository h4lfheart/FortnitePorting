using CUE4Parse.UE4.Objects.Core.Math;
using FortnitePorting.RenderingX.Core;
using FortnitePorting.RenderingX.Extensions;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace FortnitePorting.RenderingX.Components.Rendering;

public class CameraComponent : SpatialComponent
{
    public Vector3 Forward = Vector3.UnitX;
    public Vector3 Up = Vector3.UnitY;
    public Vector3 Right => Vector3.Normalize(Vector3.Cross(Forward, Up));
    
    public float FieldOfView = 70.0f;
    public float AspectRatio = 16.0f / 9.0f;
    public float NearPlane = 0.1f;
    public float FarPlane = 10000.0f;
    
    public float Speed = 5f;
    public float Sensitivity = 0.5f;
    
    public void UpdateDirection(float deltaX, float deltaY)
    {
        deltaX *= Sensitivity * 0.01f;
        deltaY *= Sensitivity * 0.01f;

        var right = Vector3.Normalize(Vector3.Cross(Forward, Up));
        
        var yawRotation = Matrix4.CreateRotationY(-deltaX);
        var pitchRotation = Matrix4.CreateFromAxisAngle(right, -deltaY);
        
        var newDirection = Vector3.TransformNormal(Forward, pitchRotation * yawRotation);
        
        var upDot = Vector3.Dot(newDirection, Up);
        if (Math.Abs(upDot) < 0.99f)
        {
            Forward = Vector3.Normalize(newDirection);
        }
    }
    
    public void Move(Vector3 direction, float deltaTime)
    {
        Transform.Position += direction * Speed * deltaTime;
    }
    
    public void UpdateMovement(KeyboardState keyboard, float deltaTime)
    {
        if (keyboard.IsKeyDown(Keys.W)) Move(Forward, deltaTime);
        if (keyboard.IsKeyDown(Keys.S)) Move(-Forward, deltaTime);
        if (keyboard.IsKeyDown(Keys.A)) Move(-Right, deltaTime);
        if (keyboard.IsKeyDown(Keys.D)) Move(Right, deltaTime);
        if (keyboard.IsKeyDown(Keys.E)) Move(Up, deltaTime);
        if (keyboard.IsKeyDown(Keys.Q)) Move(-Up, deltaTime);
    }
    
    public void LookAt(Vector3 targetPosition)
    {
        Forward = Vector3.Normalize(targetPosition - WorldPosition);
    }

    public Matrix4 ViewMatrix()
    {
        return Matrix4.LookAt(WorldPosition, WorldPosition + Forward, Up);
    }

    public Matrix4 ProjectionMatrix()
    {
        return Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(FieldOfView),
            AspectRatio,
            NearPlane,
            FarPlane);
    }
    
    public void FrameBounds(FBox bounds)
    {
        var center = bounds.GetCenter().ToVector3();
        var extent = bounds.GetExtent().ToVector3();
    
        var radius = extent.Length;
    
        var fovRadians = MathHelper.DegreesToRadians(FieldOfView);
        var distance = radius / MathF.Tan(fovRadians / 2f);
    
        distance *= 1.5f;
    
        var direction = new Vector3(-1f, -1f, -1f);
        direction = Vector3.Normalize(direction);
    
        Transform.Position = center - direction * distance;
    
        LookAt(center);
    }
}