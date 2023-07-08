using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityUtility;

namespace UnityScriptTemplate
{
    public class AssetPostprocessor : UnityEditor.AssetPostprocessor
    {
        static Type menuConfigType = typeof(ScriptTemplateMenuConfig);
        static Type textAssetType = typeof(TextAsset);

        static StringBuilder classBuilder;
        static string[] menuConfigGuids;
        static List<string> templateRootFolders = new List<string>();

        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                if (PostprocessImportedAsset(path))
                {
                    GenerateMenus();
                    break;
                }
            }

            menuConfigGuids = null;
            templateRootFolders.Clear();
        }

        static bool PostprocessImportedAsset(string path)
        {
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);

            if (menuConfigType.IsAssignableFrom(assetType))
                return PostprocessConfig(path);

            if (textAssetType.IsAssignableFrom(assetType))
                return PostprocessTemplate(path);

            return false;
        }

        static bool PostprocessTemplate(string templatePath)
        {
            if (!templatePath.EndsWith(".cs.txt", true, CultureInfo.InvariantCulture))
                return false;

            if (!FillTemplateRootFolders())
                return false;

            var templateFolder = Path.GetDirectoryName(templatePath);

            for (var i = 0; i < templateRootFolders.Count; i++)
            {
                var templateRootFolder = templateRootFolders[i];

                if (templateFolder.IndexOf(templateRootFolder) == 0)
                    return true;
            }

            return false;
        }

        static bool FillTemplateRootFolders()
        {
            if (templateRootFolders.Count > 0)
                return true;

            if (menuConfigGuids == null)
                menuConfigGuids = AssetDatabase.FindAssets("t:ScriptTemplateMenuConfig");

            if (menuConfigGuids.Length == 0)
                return false;

            for (var i = 0; i < menuConfigGuids.Length; i++)
            {
                var menuConfigGuid = menuConfigGuids[i];
                var menuConfigPath = AssetDatabase.GUIDToAssetPath(menuConfigGuid);
                var menuConfigFolder = Path.GetDirectoryName(menuConfigPath);
                var menuConfigTemplatesFolder = Path.Combine(menuConfigFolder, "Templates");

                if (!templateRootFolders.Contains(menuConfigTemplatesFolder))
                    templateRootFolders.Add(menuConfigTemplatesFolder);
            }

            return true;
        }

        static bool PostprocessConfig(string menuConfigPath)
        {
            var menuConfig = AssetDatabase.LoadAssetAtPath<ScriptTemplateMenuConfig>(menuConfigPath);
            return menuConfig != null;
        }

        static void GenerateMenus()
        {
            var ticks = DateTime.UtcNow.Ticks;

            if (menuConfigGuids == null)
                menuConfigGuids = AssetDatabase.FindAssets("t:ScriptTemplateMenuConfig");

            for (var i = 0; i < menuConfigGuids.Length; i++)
            {
                var menuConfigGuid = menuConfigGuids[i];
                var menuConfigPath = AssetDatabase.GUIDToAssetPath(menuConfigGuid);
                var menuConfig = AssetDatabase.LoadAssetAtPath<ScriptTemplateMenuConfig>(menuConfigPath);

                GenerateMenu(menuConfig, menuConfigPath, ref ticks);
            }

            AssetDatabase.Refresh();
        }

        static void GenerateMenu(
            ScriptTemplateMenuConfig menuConfig,
            string configPath,
            ref long idCounter)
        {
            var configFileName = Path.GetFileNameWithoutExtension(configPath);
            var configDir = Path.GetDirectoryName(configPath);
            var templatesDir = Path.Combine(configDir, "Templates");
            var classPath = Path.Combine(configDir, $"{configFileName}.cs");

            CheckTemplatesFolder(templatesDir);

            var templateGuids = AssetDatabase.FindAssets("t:TextAsset *.cs", new string[] { templatesDir });

            if (classBuilder == null)
                classBuilder = new StringBuilder();

            classBuilder.AppendLine($"using UnityEditor;\n");
            classBuilder.AppendLine($"namespace UnityScriptTemplate_Generated\n{{");
            classBuilder.AppendLine($"\tpublic static partial class ScriptCreateMenu_{idCounter++}\n\t{{");

            classBuilder.AppendLine("\t\tpublic static void CreateScript(string templatePath, string scriptName)\n\t\t{");
            classBuilder.AppendLine("\t\t\tProjectWindowUtil.CreateScriptAssetFromTemplateFile(");
            classBuilder.AppendLine("\t\t\t\ttemplatePath,");
            classBuilder.AppendLine("\t\t\t\tscriptName);\n\t\t}");

            for (var i = 0; i < templateGuids.Length; i++)
            {
                var templateGuid = templateGuids[i];
                var templatePath = AssetDatabase.GUIDToAssetPath(templateGuid);
                var templateFileName = Path.GetFileNameWithoutExtension(templatePath);

                classBuilder.AppendLine($"\n\t\t[MenuItem(\"Assets/Create/{menuConfig.SubMenuName}/{templateFileName}\", priority = 80)]");
                classBuilder.AppendLine($"\t\tpublic static void Create_{idCounter++}()\n\t\t{{");
                classBuilder.AppendLine($"\t\t\tCreateScript(");
                classBuilder.AppendLine($"\t\t\t\t\"{templatePath}\",");
                classBuilder.AppendLine($"\t\t\t\t\"{menuConfig.NewFilePrefix}{templateFileName}\");\n\t\t}}");
            }

            classBuilder.AppendLine($"\t}}\n}}");
            File.WriteAllText(classPath, classBuilder.ToString());

            classBuilder.Clear();
        }

        static void CheckTemplatesFolder(string templatesDir)
        {
            if (AssetDatabase.IsValidFolder(templatesDir))
                return;

            AssetDatabase.CreateFolder(
                Path.GetDirectoryName(templatesDir),
                Path.GetFileName(templatesDir));

            var exempleAssets = AssetDatabase.FindAssets("t:TextAsset ScriptTemplateExample.cs");

            if (exempleAssets.Length == 0)
                return;

            var examplePath = AssetDatabase.GUIDToAssetPath(exempleAssets[0]);
            var exampleText = AssetDatabase.LoadAssetAtPath<TextAsset>(examplePath);
            var exampleNewPath = Path.Combine(templatesDir, "ScriptTemplateExample.cs.txt");
            File.WriteAllText(exampleNewPath, exampleText.text);

            AssetDatabase.Refresh();
        }
    }
}