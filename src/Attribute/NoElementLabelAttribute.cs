using UnityEngine;

namespace MornLib
{
    /// <summary>
    /// 配列・リストの各要素に付く「Element 0」「Element 1」… のラベルを非表示にする。
    /// 単独フィールドに付けた場合はそのラベルを消す。
    /// </summary>
    public sealed class NoElementLabelAttribute : PropertyAttribute
    {
    }
}
