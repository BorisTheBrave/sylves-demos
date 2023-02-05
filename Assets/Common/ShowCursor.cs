using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ShowCursor : MonoBehaviour
{
    void Start()
    {
        Cursor.visible = true;
#if UNITY_EDITOR
        Cursor.SetCursor(PlayerSettings.defaultCursor, Vector2.zero, CursorMode.ForceSoftware);
#endif
    }
}
