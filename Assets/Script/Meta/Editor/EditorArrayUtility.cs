using System;
using UnityEditor;

public static class EditorArrayUtility
{
    // Unity duplicates the last element when growing serialized arrays. Initialize new
    // elements explicitly, while preserving all values in the overlapping region.
    public static void Resize(SerializedProperty array, int size, Action<SerializedProperty> initialize)
    {
        var oldSize = array.arraySize;
        if (oldSize == size) return;
        array.arraySize = size;
        for (var i = oldSize; i < size; i++) initialize(array.GetArrayElementAtIndex(i));
    }
}
