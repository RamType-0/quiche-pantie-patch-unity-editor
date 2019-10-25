using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AliceLaboratory.Editor {
    public class FilerOperator {

        private const string ASSET_DIR_PATH = "Assets/AliceLaboratory/";
        private const string OBJ_PATH = ASSET_DIR_PATH + "ScriptableObjects/AvatersDataObject.asset";
        //private const string CONVERTED_TEX_DIR_PATH = "Assets/ExampleAssets/sample.png";

        public void Create(string fileName, string parentDir, Texture2D _texture) {
            var assetPath = ASSET_DIR_PATH + parentDir + "/";
            var absPath = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(absPath)) {
                Directory.CreateDirectory(absPath);
                AssetDatabase.ImportAsset(absPath);
            }
        
            // 新しくテクスチャを保存
            var png = _texture.EncodeToPNG();
            var filePath = assetPath + fileName;
            File.WriteAllBytes(filePath, png);
            AssetDatabase.ImportAsset(filePath);
        }


        public static List<string> getExistsTextures(string parentDir = "Dreams") {
            string[] filePathArray;
            var fileNames = new List<string>();

            if (!Directory.Exists(ASSET_DIR_PATH + parentDir)) {
                return null;
            }
            
            filePathArray = Directory.GetFiles(ASSET_DIR_PATH + parentDir, "*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)).ToArray();

            foreach (var filePath in filePathArray) {
                var file = Path.GetFileName(filePath);
                fileNames.Add(file);
            }
            
            return fileNames;
        }


        public static Texture2D GetTexture(string path) {
            var tex = new Texture2D(0, 0);
            tex.LoadImage(File.ReadAllBytes(path));
            return tex;
        }


        //Scriptable objectとして保存
        public void SaveAvatarsData(AvatarsData data) {
            var obj = ScriptableObject.CreateInstance<AvatarsDataObject>();

            obj.DisplayNames = data.display_names;
            obj.Models = data.models;

            // 新規の場合は作成
            if (!AssetDatabase.Contains(obj as UnityEngine.Object)) {
                string dir = Path.GetDirectoryName(OBJ_PATH);
                if(!Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }
                AssetDatabase.CreateAsset(obj, OBJ_PATH);
            }
            obj.hideFlags = HideFlags.NotEditable;
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public AvatarsData readAvatersData() {
            var data = new AvatarsData();
            var obj = AssetDatabase.LoadAssetAtPath<AvatarsDataObject>(OBJ_PATH);

            if (obj != null) {
                data.display_names = obj.DisplayNames;
                data.models = obj.Models;

                return data;
            }
            return null;
        }
    }
}