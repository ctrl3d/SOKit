using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace work.ctrl3d.SOKit
{
    /// <summary>
    /// Utility classes for creating and managing ScriptableObjects
    /// </summary>
    public static class SOKit
    {
        /// <summary>
        /// Enable logging or not
        /// </summary>
        public static bool EnableLogging { get; set; } = true;

        /// <summary>
        /// 에셋 이름에 .asset 확장자가 없는 경우에만 추가합니다.
        /// </summary>
        /// <param name="assetName">에셋 이름</param>
        /// <returns>확장자가 추가된 에셋 이름</returns>
        private static string EnsureAssetExtension(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
                return assetName;
        
            return assetName.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)
                ? assetName
                : $"{assetName}.asset";
        }
        
        /// <summary>
        /// 에셋 경로를 생성합니다.
        /// </summary>
        /// <param name="folderPath">폴더 경로</param>
        /// <param name="assetName">에셋 이름</param>
        /// <returns>완성된 에셋 경로</returns>
        private static string CreateAssetPath(string folderPath, string assetName)
        {
            return $"{folderPath}/{EnsureAssetExtension(assetName)}";
        }


        
        #region Sync methods

        /// <summary>
        /// ScriptableObject 인스턴스를 생성합니다.
        /// </summary>
        /// <typeparam name="T">생성할 ScriptableObject 타입</typeparam>
        /// <returns>생성 결과</returns>
        public static SOResult<T> Create<T>() where T : ScriptableObject
        {
            try
            {
                var instance = ScriptableObject.CreateInstance<T>();

                if (EnableLogging)
                    Debug.Log($"{typeof(T).Name} 생성 완료");

                return new SOResult<T>(instance);
            }
            catch (Exception ex)
            {
                var error = $"{typeof(T).Name} 생성 실패: {ex.Message}";

                if (EnableLogging)
                    Debug.LogError(error);

                return new SOResult<T>(error);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// ScriptableObject를 에셋으로 저장합니다.
        /// </summary>
        /// <typeparam name="T">저장할 ScriptableObject 타입</typeparam>
        /// <param name="so">저장할 ScriptableObject 인스턴스</param>
        /// <param name="folderPath">저장 폴더 경로 (기본값: "Assets/Resources")</param>
        /// <param name="assetName">에셋 이름 (기본값: null - 타입 이름 사용)</param>
        /// <returns>저장 결과</returns>
        public static SOResult<T> Save<T>(T so, string folderPath = "Assets/Resources", string assetName = null)
            where T : ScriptableObject
        {
            try
            {
                if (so is null)
                    return new SOResult<T>($"저장할 {typeof(T).Name} 인스턴스가 null입니다.");

                // 에셋 이름이 지정되지 않은 경우 타입 이름 사용
                if (string.IsNullOrEmpty(assetName))
                    assetName = typeof(T).Name;

                // 폴더가 없으면 생성
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
                
                var assetPath = CreateAssetPath(folderPath, assetName);

                // 이미 에셋이 존재하는지 확인
                if (HasAsset(assetPath))
                {
                    // 이미 존재하는 에셋 가져오기
                    var existingAsset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

                    if (existingAsset is not null && existingAsset != so)
                    {
                        // ScriptableObject 내용 복사 (필요에 따라 구현)
                        EditorUtility.CopySerialized(so, existingAsset);
                        EditorUtility.SetDirty(existingAsset);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                        if (EnableLogging)
                            Debug.Log($"{typeof(T).Name} 업데이트 완료: {assetPath}");

                        return new SOResult<T>(existingAsset, assetPath);
                    }
                }

                // 새 에셋 생성
                AssetDatabase.CreateAsset(so, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (EnableLogging)
                    Debug.Log($"{typeof(T).Name} 저장 완료: {assetPath}");

                return new SOResult<T>(so, assetPath);
            }
            catch (Exception ex)
            {
                var error = $"{typeof(T).Name} 저장 실패: {ex.Message}";

                if (EnableLogging)
                    Debug.LogError(error);

                return new SOResult<T>(error);
            }
        }

        /// <summary>
        /// ScriptableObject를 생성하고 에셋으로 저장합니다.
        /// </summary>
        /// <typeparam name="T">생성 및 저장할 ScriptableObject 타입</typeparam>
        /// <param name="folderPath">저장 폴더 경로</param>
        /// <param name="assetName">에셋 이름 (기본값: null - 타입 이름 사용)</param>
        /// <returns>생성 및 저장 결과</returns>
        public static SOResult<T> CreateAndSave<T>(string folderPath, string assetName)
            where T : ScriptableObject
        {
            var createResult = Create<T>();
            return createResult.Success switch
            {
                false => createResult,
                _ => Save(createResult.Data, folderPath, assetName)
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assetPath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static SOResult<T> CreateAndSave<T>(string assetPath)
            where T : ScriptableObject
        {
            var folderPath = Path.GetDirectoryName(assetPath);
            var assetName = Path.GetFileName(assetPath);

            var createResult = Create<T>();
            return createResult.Success switch
            {
                false => createResult,
                _ => Save(createResult.Data, folderPath, assetName)
            };
        }

        /// <summary>
        /// 지정된 경로에서 ScriptableObject 에셋을 로드합니다.
        /// </summary>
        /// <typeparam name="T">로드할 ScriptableObject 타입</typeparam>
        /// <param name="assetPath">에셋 경로</param>
        /// <returns>로드 결과</returns>
        public static SOResult<T> Load<T>(string assetPath) where T : ScriptableObject
        {
            try
            {
                if (string.IsNullOrEmpty(assetPath))
                    return new SOResult<T>("에셋 경로가 비어 있습니다.");

                if (!File.Exists(assetPath))
                    return new SOResult<T>($"지정된 경로에 파일이 존재하지 않습니다: {assetPath}");

                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

                if (asset is null)
                    return new SOResult<T>($"지정된 경로에서 {typeof(T).Name} 타입의 에셋을 로드할 수 없습니다: {assetPath}");

                if (EnableLogging)
                    Debug.Log($"{typeof(T).Name} 로드 완료: {assetPath}");

                return new SOResult<T>(asset, assetPath);
            }
            catch (Exception ex)
            {
                var error = $"{typeof(T).Name} 로드 실패: {ex.Message}";

                if (EnableLogging)
                    Debug.LogError(error);

                return new SOResult<T>(error);
            }
        }

        /// <summary>
        /// 에셋이 존재하는지 확인합니다.
        /// </summary>
        /// <param name="assetPath">확인할 에셋 경로</param>
        /// <returns>에셋 존재 여부</returns>
        public static bool HasAsset(string assetPath)
        {
            return File.Exists(assetPath) && AssetDatabase.LoadAssetAtPath<Object>(assetPath) is not null;
        }

        public static bool HasAsset(string folderPath, string assetName)
        {
            return HasAsset(Path.Combine(folderPath, EnsureAssetExtension(assetName)));
        }

        /// <summary>
        /// ScriptableObject 에셋을 가져오거나, 없으면 생성합니다.
        /// </summary>
        /// <typeparam name="T">가져오거나 생성할 ScriptableObject 타입</typeparam>
        /// <param name="folderPath">저장 폴더 경로 (기본값: "Assets/Resources")</param>
        /// <param name="assetName">에셋 이름 (기본값: null - 타입 이름 사용)</param>
        /// <returns>가져오기 또는 생성 결과</returns>
        public static SOResult<T> GetOrCreate<T>(string folderPath = "Assets/Resources", string assetName = null)
            where T : ScriptableObject
        {
            try
            {
                // 에셋 이름이 지정되지 않은 경우 타입 이름 사용
                if (string.IsNullOrEmpty(assetName))
                    assetName = typeof(T).Name;

                var assetPath = CreateAssetPath(folderPath, assetName);

                // 에셋이 존재하면 로드, 존재하지 않으면 생성 및 저장
                return HasAsset(assetPath)
                    ? Load<T>(assetPath)
                    :
                    // 
                    CreateAndSave<T>(folderPath, assetName);
            }
            catch (Exception ex)
            {
                var error = $"{typeof(T).Name} 가져오기/생성 실패: {ex.Message}";

                if (EnableLogging)
                    Debug.LogError(error);

                return new SOResult<T>(error);
            }
        }

        /// <summary>
        /// 지정된 경로의 에셋을 삭제합니다.
        /// </summary>
        /// <param name="assetPath">삭제할 에셋 경로</param>
        /// <returns>삭제 결과 (성공: true, 실패: false)</returns>
        public static (bool Success, string ErrorMessage) DeleteAsset(string assetPath)
        {
            try
            {
                if (string.IsNullOrEmpty(assetPath))
                    return (false, "삭제할 에셋 경로가 비어 있습니다.");

                if (!HasAsset(assetPath))
                    return (false, $"삭제할 에셋이 존재하지 않습니다: {assetPath}");

                bool result = AssetDatabase.DeleteAsset(assetPath);

                if (!result)
                    return (false, $"에셋 삭제 실패: {assetPath}");

                AssetDatabase.Refresh();

                if (EnableLogging)
                    Debug.Log($"에셋 삭제 완료: {assetPath}");

                return (true, null);
            }
            catch (Exception ex)
            {
                var error = $"에셋 삭제 실패: {ex.Message}";

                if (EnableLogging)
                    Debug.LogError(error);

                return (false, error);
            }
        }

        /// <summary>
        /// 지정된 폴더와 이름의 에셋을 삭제합니다.
        /// </summary>
        /// <param name="folderPath">폴더 경로 (기본값: "Assets/Resources")</param>
        /// <param name="assetName">삭제할 에셋 이름</param>
        /// <returns>삭제 결과 (성공: true, 실패: false)</returns>
        public static (bool Success, string ErrorMessage) Delete(string folderPath = "Assets/Resources",
            string assetName = null)
        {
            try
            {
                if (string.IsNullOrEmpty(assetName))
                    return (false, "에셋 이름이 지정되지 않았습니다.");

                var assetPath = CreateAssetPath(folderPath, assetName);
                return DeleteAsset(assetPath);
            }
            catch (Exception ex)
            {
                var error = $"에셋 삭제 실패: {ex.Message}";

                if (EnableLogging)
                    Debug.LogError(error);

                return (false, error);
            }
        }
#endif

        #endregion

#if UNITASK_SUPPORT

        #region Async methods

        /// <summary>
        /// 동기 버전의 결과를 그대로 UniTask로 래핑하여 반환
        /// </summary>
        /// <typeparam name="T">생성할 ScriptableObject 타입</typeparam>
        /// <returns>생성 결과</returns>
        public static UniTask<SOResult<T>> CreateAsync<T>() where T : ScriptableObject
        {
            var result = Create<T>();
            return UniTask.FromResult(result);
        }

#if UNITY_EDITOR
        /// <summary>
        /// ScriptableObject를 에셋으로 비동기적으로 저장합니다.
        /// </summary>
        /// <typeparam name="T">저장할 ScriptableObject 타입</typeparam>
        /// <param name="so">저장할 ScriptableObject 인스턴스</param>
        /// <param name="folderPath">저장 폴더 경로 (기본값: "Assets/Resources")</param>
        /// <param name="assetName">에셋 이름 (기본값: null - 타입 이름 사용)</param>
        /// <returns>저장 결과</returns>
        public static async UniTask<SOResult<T>> SaveAsync<T>(T so, string folderPath = "Assets/Resources",
            string assetName = null) where T : ScriptableObject
        {
            try
            {
                if (so is null)
                    return new SOResult<T>($"The {typeof(T).Name} instance to save is null.");

                // 에셋 이름이 지정되지 않은 경우 타입 이름 사용
                if (string.IsNullOrEmpty(assetName))
                    assetName = typeof(T).Name;

                // 폴더가 없으면 생성 (IO 작업은 스레드 풀에서 실행)
                if (!Directory.Exists(folderPath))
                {
                    await UniTask.RunOnThreadPool(() => Directory.CreateDirectory(folderPath));
                }
                
                var assetPath = CreateAssetPath(folderPath, assetName);

                // 이미 에셋이 존재하는지 확인
                if (await HasAssetAsync(assetPath))
                {
                    // 이미 존재하는 에셋 가져오기
                    await UniTask.SwitchToMainThread();
                    var existingAsset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

                    if (existingAsset is not null && existingAsset != so)
                    {
                        // ScriptableObject 내용 복사 (필요에 따라 구현)
                        EditorUtility.CopySerialized(so, existingAsset);
                        EditorUtility.SetDirty(existingAsset);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                        if (EnableLogging)
                            Debug.Log($"{typeof(T).Name} Update complete: {assetPath}");

                        return new SOResult<T>(existingAsset, assetPath);
                    }
                }

                // 새 에셋 생성
                await UniTask.SwitchToMainThread();
                AssetDatabase.CreateAsset(so, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (EnableLogging)
                    Debug.Log($"{typeof(T).Name} Save complete: {assetPath}");

                return new SOResult<T>(so, assetPath);
            }
            catch (Exception ex)
            {
                var error = $"{typeof(T).Name} Save Failed: {ex.Message}";

                if (EnableLogging)
                    Debug.LogError(error);

                return new SOResult<T>(error);
            }
        }

        /// <summary>
        /// ScriptableObject를 생성하고 에셋으로 비동기적으로 저장합니다.
        /// </summary>
        /// <typeparam name="T">생성 및 저장할 ScriptableObject 타입</typeparam>
        /// <param name="folderPath">저장 폴더 경로</param>
        /// <param name="assetName">에셋 이름</param>
        /// <returns>생성 및 저장 결과</returns>
        public static async UniTask<SOResult<T>> CreateAndSaveAsync<T>(string folderPath, string assetName)
            where T : ScriptableObject
        {
            var createResult = await CreateAsync<T>();
            if (!createResult.Success)
                return createResult;

            return await SaveAsync(createResult.Data, folderPath, assetName);
        }

        public static async UniTask<SOResult<T>> CreateAndSaveAsync<T>(string assetPath)
            where T : ScriptableObject
        {
            var folderPath = Path.GetDirectoryName(assetPath);
            var assetName = Path.GetFileName(assetPath);

            var createResult = await CreateAsync<T>();
            if (!createResult.Success)
                return createResult;

            return await SaveAsync(createResult.Data, folderPath, assetName);
        }

        /// <summary>
        /// 지정된 경로에서 ScriptableObject 에셋을 비동기적으로 로드합니다.
        /// </summary>
        /// <typeparam name="T">로드할 ScriptableObject 타입</typeparam>
        /// <param name="assetPath">에셋 경로</param>
        /// <returns>로드 결과</returns>
        public static async UniTask<SOResult<T>> LoadAsync<T>(string assetPath) where T : ScriptableObject
        {
            try
            {
                if (string.IsNullOrEmpty(assetPath))
                    return new SOResult<T>("The asset path is empty.");

                // 파일 존재 확인은 스레드 풀에서 수행
                var fileExists = await UniTask.RunOnThreadPool(() => File.Exists(assetPath));

                if (!fileExists)
                    return new SOResult<T>($"The file does not exist in the specified path: {assetPath}");

                // 에셋 로드는 메인 스레드에서 수행
                await UniTask.SwitchToMainThread();
                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

                if (asset is null)
                    return new SOResult<T>($"지정된 경로에서 {typeof(T).Name} 타입의 에셋을 로드할 수 없습니다: {assetPath}");

                if (EnableLogging)
                    Debug.Log($"{typeof(T).Name} Load complete: {assetPath}");

                return new SOResult<T>(asset, assetPath);
            }
            catch (Exception ex)
            {
                var error = $"{typeof(T).Name} Load Failed: {ex.Message}";

                if (EnableLogging)
                    Debug.LogError(error);

                return new SOResult<T>(error);
            }
        }

        /// <summary>
        /// 에셋이 존재하는지 비동기적으로 확인합니다.
        /// </summary>
        /// <param name="assetPath">확인할 에셋 경로</param>
        /// <returns>에셋 존재 여부</returns>
        public static async UniTask<bool> HasAssetAsync(string assetPath)
        {
            var fileExists = await UniTask.RunOnThreadPool(() => File.Exists(assetPath));

            if (!fileExists)
                return false;

            await UniTask.SwitchToMainThread();
            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            return asset is not null;
        }

        public static async UniTask<bool> HasAssetAsync(string folderPath, string assetName)
        {
            var assetPath = CreateAssetPath(folderPath, assetName);
            return await HasAssetAsync(assetPath);
        }

        /// <summary>
        /// ScriptableObject 에셋을 비동기적으로 가져오거나, 없으면 생성합니다.
        /// </summary>
        /// <typeparam name="T">가져오거나 생성할 ScriptableObject 타입</typeparam>
        /// <param name="folderPath">저장 폴더 경로 (기본값: "Assets/Resources")</param>
        /// <param name="assetName">에셋 이름 (기본값: null - 타입 이름 사용)</param>
        /// <returns>가져오기 또는 생성 결과</returns>
        public static async UniTask<SOResult<T>> GetOrCreateAsync<T>(string folderPath = "Assets/Resources",
            string assetName = null)
            where T : ScriptableObject
        {
            try
            {
                // 에셋 이름이 지정되지 않은 경우 타입 이름 사용
                if (string.IsNullOrEmpty(assetName))
                    assetName = typeof(T).Name;

                var assetPath = CreateAssetPath(folderPath, assetName);

                // 에셋이 존재하면 로드
                if (await HasAssetAsync(assetPath))
                    return await LoadAsync<T>(assetPath);

                // 존재하지 않으면 생성 및 저장
                return await CreateAndSaveAsync<T>(folderPath, assetName);
            }
            catch (Exception ex)
            {
                var error = $"{typeof(T).Name} Failed to import/create: {ex.Message}";

                if (EnableLogging)
                    Debug.LogError(error);

                return new SOResult<T>(error);
            }
        }

        /// <summary>
        /// 지정된 경로의 에셋을 비동기적으로 삭제합니다.
        /// </summary>
        /// <param name="assetPath">삭제할 에셋 경로</param>
        /// <returns>삭제 결과 (성공: true, 실패: false)</returns>
        public static async UniTask<(bool Success, string ErrorMessage)> DeleteAssetAsync(string assetPath)
        {
            try
            {
                if (string.IsNullOrEmpty(assetPath))
                    return (false, "The path to the asset to be deleted is empty.");

                var exists = await HasAssetAsync(assetPath);
                if (!exists)
                    return (false, $"삭제할 에셋이 존재하지 않습니다: {assetPath}");

                await UniTask.SwitchToMainThread();
                var result = AssetDatabase.DeleteAsset(assetPath);

                if (!result)
                    return (false, $"에셋 삭제 실패: {assetPath}");

                AssetDatabase.Refresh();

                if (EnableLogging)
                    Debug.Log($"에셋 삭제 완료: {assetPath}");

                return (true, null);
            }
            catch (Exception ex)
            {
                var error = $"에셋 삭제 실패: {ex.Message}";

                if (EnableLogging)
                    Debug.LogError(error);

                return (false, error);
            }
        }

        /// <summary>
        /// 지정된 폴더와 이름의 에셋을 비동기적으로 삭제합니다.
        /// </summary>
        /// <param name="folderPath">폴더 경로 (기본값: "Assets/Resources")</param>
        /// <param name="assetName">삭제할 에셋 이름</param>
        /// <returns>삭제 결과 (성공: true, 실패: false)</returns>
        public static async UniTask<(bool Success, string ErrorMessage)> DeleteAsync(
            string folderPath = "Assets/Resources", string assetName = null)
        {
            try
            {
                if (string.IsNullOrEmpty(assetName))
                    return (false, "에셋 이름이 지정되지 않았습니다.");

                var assetPath = CreateAssetPath(folderPath, assetName);
                return await DeleteAssetAsync(assetPath);
            }
            catch (Exception ex)
            {
                var error = $"에셋 삭제 실패: {ex.Message}";

                if (EnableLogging)
                    Debug.LogError(error);

                return (false, error);
            }
        }

        /// <summary>
        /// 여러 ScriptableObject 에셋을 한 번에 로드합니다.
        /// </summary>
        /// <typeparam name="T">로드할 ScriptableObject 타입</typeparam>
        /// <param name="assetPaths">로드할 에셋 경로 배열</param>
        /// <returns>로드된 ScriptableObject 배열</returns>
        public static async UniTask<T[]> LoadMultipleAsync<T>(string[] assetPaths) where T : ScriptableObject
        {
            // UniTask.WhenAll을 사용하여 병렬 처리
            var tasks = new UniTask<SOResult<T>>[assetPaths.Length];

            for (var i = 0; i < assetPaths.Length; i++)
            {
                tasks[i] = LoadAsync<T>(assetPaths[i]);
            }

            var results = await UniTask.WhenAll(tasks);

            // 성공적으로 로드된 에셋만 반환
            var successfulAssets = new List<T>();
            foreach (var result in results)
            {
                if (result.Success && result.Data != null)
                {
                    successfulAssets.Add(result.Data);
                }
            }

            return successfulAssets.ToArray();
        }

        /// <summary>
        /// 진행 상황을 보고하면서 여러 작업을 수행합니다.
        /// </summary>
        /// <typeparam name="T">대상 ScriptableObject 타입</typeparam>
        /// <param name="assetPaths">처리할 에셋 경로 배열</param>
        /// <param name="progress">진행 상황 리포터</param>
        /// <param name="action">각 에셋에 적용할 작업 (null이면 로드만 수행)</param>
        /// <returns>처리된 에셋 배열</returns>
        public static async UniTask<T[]> ProcessWithProgressAsync<T>(
            string[] assetPaths,
            IProgress<float> progress = null,
            Func<T, UniTask> action = null) where T : ScriptableObject
        {
            var assets = new List<T>();

            for (var i = 0; i < assetPaths.Length; i++)
            {
                var result = await LoadAsync<T>(assetPaths[i]);

                if (result.Success && result.Data != null)
                {
                    if (action != null)
                    {
                        await action(result.Data);
                    }

                    assets.Add(result.Data);
                }

                // 진행 상황 보고
                progress?.Report((float)(i + 1) / assetPaths.Length);
            }

            return assets.ToArray();
        }
#endif

        #endregion

#endif
    }
}