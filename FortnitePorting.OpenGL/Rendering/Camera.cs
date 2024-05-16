using OpenTK.Mathematics;

namespace FortnitePorting.OpenGL.Rendering;

public class Camera
{
    public Vector3 Position;
    public Vector3 Direction;
    public Vector3 Up = Vector3.UnitY;

    public float Yaw = -90.0f;
    public float Pitch;
    public float FOV = 60f;
    public float Speed = 0.25f;
    public float Sensitivity = 0.5f;
    public float Near = 0.01f;
    public float Far = 100f;
    public float AspectRatio = 16f / 9f;

    public Camera()
    {
        //Position = new Vector3(0, 1, 1);
        //Direction = Vector3.Zero;

        Position = new Vector3(0.741224229f, 1.47437632f, 1.39653087f);
        Direction = new Vector3(-0.430533648f, -0.398749083f, -0.809715986f);

        //SetupDirection();
    }

    private void SetupDirection()
    {
        var yaw = MathF.Atan((-Position.X - Direction.X) / (Position.Z - Direction.Z));
        var pitch = MathF.Atan((Position.Y - Direction.Y) / (Position.Z - Direction.Z));
        CalculateDirection(MathHelper.RadiansToDegrees(yaw), MathHelper.RadiansToDegrees(pitch));
    }

    public void CalculateDirection(float x, float y)
    {
        Yaw += x;
        Pitch -= y;

        Pitch = Math.Clamp(Pitch, -89f, 89f);

        var direction = Vector3.Zero;
        var yaw = MathHelper.DegreesToRadians(Yaw);
        var pitch = MathHelper.DegreesToRadians(Pitch);
        direction.X = MathF.Cos(yaw) * MathF.Cos(pitch);
        direction.Y = MathF.Sin(pitch);
        direction.Z = MathF.Sin(yaw) * MathF.Cos(pitch);
        Direction = Vector3.Normalize(direction);
    }


    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(Position, Position + Direction, Up);
    }

    public Matrix4 GetProjectionMatrix()
    {
        return Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOV), AspectRatio, Near, Far);
    }
}