using UnityEngine;

[CreateAssetMenu(fileName = "TestScriptableObject", menuName = "Scriptable Objects/TestScriptableObject")]
public class TestScriptableObject : ScriptableObject
{
    public string testName = "테스트";
    public int testValue = 100;
    public Color testColor = Color.red;
    public Vector3 testPosition = Vector3.zero;
        
    [TextArea(3, 5)]
    public string testDescription = "이것은 테스트 설명입니다.";

}
