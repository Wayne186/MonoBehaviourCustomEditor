using UnityEngine;

public class TestInspectFunction : MonoBehaviour
{
    [SerializeField] GameObject gameObject;
    public int randomInt;

    private void Start()
    {
        
    }

    public enum TestEnum
    {
        Enum1,
        Enum2,
        Enum3
    }
    // This is our fancy attribute. Easy no?
    [InspectFunction]
    public void TryInspectFunction(Bounds bound, GameObject obj, Collider col, Color color, Vector3 vector3, string str)
    {
        Debug.LogFormat("{0} {1} {2} {3} {4} {5}", bound.ToString(), obj.name, col.name, color.ToString(), vector3.ToString(), str);
    }

    [InspectFunction]
    public void AnotherInspectFunction(GameObject obj, Collider col, Color color, Vector3 vector3, string str)
    {
        Debug.LogFormat("{0} {1} {2} {3} {4}", obj.name, col.name, color.ToString(), vector3.ToString(), str);
    }

    [InspectFunction]
    private void TryEnum(TestEnum e)
    {
        Debug.Log(e);
    }

    [InspectFunction]
    private void NoArgs()
    {
        Debug.Log("No args");
    }

    public void NotInspecting()
    {

    }
}
