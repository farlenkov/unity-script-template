using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityScriptTemplate
{
    [CreateAssetMenu(fileName = "ScriptTemplateMenu", menuName = "Script Template Menu")]
    public class ScriptTemplateMenuConfig : ScriptableObject
    {
        [Header("Menu Class Generation")]

        [SerializeField]
        internal string SubMenuName = "My Game";

        [SerializeField]
        internal string NewFilePrefix = "New";
    }
}
