using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace work.ctrl3d.SOKit.Editor.Test
{
    /// <summary>
    /// SO Kit 테스트용 에디터 창
    /// </summary>
    public class SOKitTestWindow : EditorWindow
    {
        private string assetName = "TestSO";
        private string folderPath = "Assets/ScriptableObjects";
        private TestScriptableObject currentSO;
        private List<TestScriptableObject> loadedSOs = new();
        private Vector2 scrollPosition;

        [MenuItem("Tools/SO Kit/Test Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<SOKitTestWindow>("SO Kit 테스트");
            window.minSize = new Vector2(400, 450);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("SO Kit 테스트 도구", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 파라미터 설정 영역
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("ScriptableObject 생성 설정", EditorStyles.boldLabel);
            
            assetName = EditorGUILayout.TextField("에셋 이름", assetName);
            folderPath = EditorGUILayout.TextField("저장 경로", folderPath);
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // 기능 버튼 영역
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("생성", GUILayout.Height(30)))
            {
                CreateTestSO();
            }
            
            if (GUILayout.Button("생성 및 저장", GUILayout.Height(30)))
            {
                CreateAndSaveTestSO();
            }
            
            if (GUILayout.Button("전체 목록 불러오기", GUILayout.Height(30)))
            {
                LoadAllSOs();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 현재 선택된 SO 표시 영역
            EditorGUILayout.Space();
            if (currentSO != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("현재 ScriptableObject", EditorStyles.boldLabel);
                
                EditorGUI.BeginChangeCheck();
                currentSO.testName = EditorGUILayout.TextField("이름", currentSO.testName);
                currentSO.testValue = EditorGUILayout.IntField("값", currentSO.testValue);
                currentSO.testColor = EditorGUILayout.ColorField("색상", currentSO.testColor);
                currentSO.testPosition = EditorGUILayout.Vector3Field("위치", currentSO.testPosition);
                currentSO.testDescription = EditorGUILayout.TextArea(
                    currentSO.testDescription, 
                    GUILayout.Height(60)
                );
                
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(currentSO);
                }
                
                EditorGUILayout.Space();
                if (GUILayout.Button("이 ScriptableObject 저장"))
                {
                    SaveCurrentSO();
                }
                
                EditorGUILayout.EndVertical();
            }
            
            // 로드된 SO 목록 표시 영역
            EditorGUILayout.Space();
            if (loadedSOs.Count > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("불러온 ScriptableObjects 목록", EditorStyles.boldLabel);
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
                for (int i = 0; i < loadedSOs.Count; i++)
                {
                    var so = loadedSOs[i];
                    if (so != null)
                    {
                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        
                        EditorGUILayout.LabelField($"{so.name} (값: {so.testValue})", GUILayout.Width(200));
                        
                        if (GUILayout.Button("선택", GUILayout.Width(60)))
                        {
                            currentSO = so;
                        }
                        
                        if (GUILayout.Button("삭제", GUILayout.Width(60)))
                        {
                            DeleteSO(so);
                            break; // 리스트가 변경되므로 루프 종료
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// TestScriptableObject 생성
        /// </summary>
        private void CreateTestSO()
        {
            var result = SOKit.Create<TestScriptableObject>();
            if (result.Success)
            {
                currentSO = result.Object;
                currentSO.name = assetName;
                Debug.Log($"ScriptableObject 생성 완료: {currentSO.name}");
            }
            else
            {
                Debug.LogError(result.ErrorMessage);
            }
        }

        /// <summary>
        /// TestScriptableObject 생성 및 저장
        /// </summary>
        private void CreateAndSaveTestSO()
        {
            var result = SOKit.CreateAndSave<TestScriptableObject>(folderPath, assetName);
            if (result.Success)
            {
                currentSO = result.Object;
                Debug.Log($"ScriptableObject 생성 및 저장 완료: {result.AssetPath}");
                
                // 에셋 선택
                Selection.activeObject = currentSO;
                
                // 목록 새로고침
                LoadAllSOs();
            }
            else
            {
                Debug.LogError(result.ErrorMessage);
            }
        }

        /// <summary>
        /// 현재 ScriptableObject 저장
        /// </summary>
        private void SaveCurrentSO()
        {
            if (currentSO == null)
            {
                Debug.LogError("저장할 ScriptableObject가 없습니다.");
                return;
            }
            
            var result = SOKit.Save(currentSO, folderPath, currentSO.name);
            if (result.Success)
            {
                Debug.Log($"ScriptableObject 저장 완료: {result.AssetPath}");
                
                // 목록 새로고침
                LoadAllSOs();
            }
            else
            {
                Debug.LogError(result.ErrorMessage);
            }
        }

        /// <summary>
        /// 모든 TestScriptableObject 불러오기
        /// </summary>
        private void LoadAllSOs()
        {
            loadedSOs.Clear();
            
            // 에셋 경로 찾기
            var guids = AssetDatabase.FindAssets("t:TestScriptableObject");
            
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var result = SOKit.Load<TestScriptableObject>(assetPath);
                
                if (result.Success && result.Object != null)
                {
                    loadedSOs.Add(result.Object);
                }
            }
            
            Debug.Log($"{loadedSOs.Count}개의 TestScriptableObject를 불러왔습니다.");
        }

        /// <summary>
        /// ScriptableObject 삭제
        /// </summary>
        private void DeleteSO(TestScriptableObject so)
        {
            if (so == null)
                return;
            
            var assetPath = AssetDatabase.GetAssetPath(so);
            if (string.IsNullOrEmpty(assetPath))
                return;
            
            var result = SOKit.DeleteAsset(assetPath);
            if (result.Success)
            {
                Debug.Log($"ScriptableObject 삭제 완료: {assetPath}");
                
                // 현재 선택된 SO가 삭제되었다면 null로 설정
                if (currentSO == so)
                    currentSO = null;
                
                // 목록 새로고침
                LoadAllSOs();
            }
            else
            {
                Debug.LogError(result.ErrorMessage);
            }
        }
    }
}