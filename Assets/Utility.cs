
using UnityEngine;

class U
{
    public static UnityEngine.Vector2 from(System.Numerics.Vector2 v)
    {
        UnityEngine.Vector2 newV = new UnityEngine.Vector2();
        newV.x = v.X;
        newV.y = v.Y;
        return newV;
    }
    public static System.Numerics.Vector2 from(UnityEngine.Vector2 v)
    {
        System.Numerics.Vector2 newV = new System.Numerics.Vector2();
        newV.X = v.x;
        newV.Y = v.y;
        return newV;
    }
}
    


