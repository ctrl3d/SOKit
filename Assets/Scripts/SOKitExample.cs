using UnityEditor;
using UnityEngine;
using work.ctrl3d.SOKit;

public class SOKitExample : EditorWindow
{
    [MenuItem("Tools/SO Kit Example")]
    public static void ShowWindow()
    {
        GetWindow<SOKitExample>("SO Kit Example");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Create ScriptableObject"))
        {
            // 예시: 스크립터블 오브젝트 생성 및 저장
            var result = SOKit.CreateAndSave<TestScriptableObject>("Assets/ScriptableObjects", "TestScriptableObject");
            
            if (result.Success)
            {
                EditorGUIUtility.PingObject(result.Object);
            }
            else
            {
                Debug.LogError(result.ErrorMessage);
            }
        }
    }

}
