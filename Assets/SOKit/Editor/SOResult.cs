using UnityEngine;

namespace work.ctrl3d.SOKit
{
    /// <summary>
    /// ScriptableObject 작업 결과를 담는 구조체
    /// </summary>
    public readonly struct SOResult<T> where T : ScriptableObject
    {
        /// <summary>
        /// 작업 성공 여부
        /// </summary>
        public readonly bool Success;
        
        /// <summary>
        /// ScriptableObject 인스턴스 (성공 시)
        /// </summary>
        public readonly T Object;
        
        /// <summary>
        /// 오류 메시지 (실패 시)
        /// </summary>
        public readonly string ErrorMessage;
        
        /// <summary>
        /// 에셋 경로 (해당하는 경우)
        /// </summary>
        public readonly string AssetPath;

        /// <summary>
        /// 성공 결과 생성
        /// </summary>
        public SOResult(T obj, string assetPath = null)
        {
            Success = true;
            Object = obj;
            ErrorMessage = null;
            AssetPath = assetPath;
        }

        /// <summary>
        /// 실패 결과 생성
        /// </summary>
        public SOResult(string errorMessage)
        {
            Success = false;
            Object = null;
            ErrorMessage = errorMessage;
            AssetPath = null;
        }
    }
}