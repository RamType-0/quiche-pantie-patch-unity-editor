using System;
using UnityEditor;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;

#if UNITY_2019_1_OR_NEWER
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif


namespace AliceLaboratory.Editor {
    public class PantiePatchEditorConvertWindow : EditorWindow {


        [SerializeField] VisualTreeAsset visualTree = default;
        

        AsyncReactiveProperty<Texture> BaseAvatarTexture { get; } = new AsyncReactiveProperty<Texture>(null);
        AsyncReactiveProperty<Texture> ConvertTexture { get; } = new AsyncReactiveProperty<Texture>(null);
        AsyncReactiveProperty<int> SelectedAvatarIndex { get; } = new AsyncReactiveProperty<int>(-1);
        AsyncReactiveProperty<bool> ConvertRunning { get; } = new AsyncReactiveProperty<bool>(false);

        ReadOnlyAsyncReactiveProperty<bool> CanStartConvert { get; set; } 
        
        //スクロール位置
        private Vector2 _scrollPosition = Vector2.zero;

        private AvatarsData _avatarsData;

        /// <summary>
        /// Initialization
        /// </summary>
        [MenuItem("Editor/PantiePatch/パンツ変換")]
        private static void Init() {
            
            var window = GetWindow<PantiePatchEditorConvertWindow>();
            window.titleContent = new GUIContent("パンツ変換");
            window.Show();
        }
        
        #region Unity Method
        
        private void OnEnable() {
            // ScriptableObjectからアバターデータを読み込む
            var file = new FilerOperator();
            _avatarsData = file.readAvatersData();

            CanStartConvert = UniTaskAsyncEnumerable.CombineLatest(ConvertTexture, SelectedAvatarIndex, ConvertRunning,
                (convertTexture, selectedAvatarIndex, convertRunning) => convertTexture != null && selectedAvatarIndex!=-1 && !convertRunning).ToReadOnlyAsyncReactiveProperty(default);

            CreateGUI();
        }

        /// <summary>
        /// GUI setting
        /// </summary>
        private void _OnGUI() {
            using (new GUILayout.VerticalScope()) {
                using (new GUILayout.HorizontalScope()) {
                    EditorGUILayout.LabelField("変換するパンツを選択");
                    var option = new []{GUILayout.Width (64), GUILayout.Height (64)};
                    ConvertTexture.Value = EditorGUILayout.ObjectField(ConvertTexture.Value, typeof(Texture), false, option) as Texture;
                }
                using (new GUILayout.HorizontalScope()) {
                    EditorGUILayout.LabelField("重ねるアバターのテクスチャを選択");
                    var option = new []{GUILayout.Width (64), GUILayout.Height (64)};
                    BaseAvatarTexture.Value = EditorGUILayout.ObjectField(BaseAvatarTexture.Value, typeof(Texture), false, option) as Texture;
                }
                EditorGUILayout.LabelField("変換対象のアバター");
            }
            if (_avatarsData != null) {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                SelectedAvatarIndex.Value = GUILayout.SelectionGrid(SelectedAvatarIndex.Value, _avatarsData.display_names, 2);
                EditorGUILayout.EndScrollView();
            } else {
                EditorGUILayout.HelpBox(
                    "アバターのデータがダウンロードされていません\nメニューのデータダウンロード > 対応アバター情報の更新からデータのダウンロードをして下さい",
                    MessageType.Info
                );
            }
            using (new GUILayout.VerticalScope()) {
                GUILayout.Space(20);
                EditorGUI.BeginDisabledGroup(!CanStartConvert.Value);
                if(GUILayout.Button("変換")) {
                    UniTask.Void(async () =>
                    {
                        await Convert(ConvertTexture.Value, BaseAvatarTexture.Value, SelectedModelName);
                    });
                }
                EditorGUI.EndDisabledGroup();
            }
        }
        
        #endregion

         void CreateGUI()
         {
#if UNITY_2019_1_OR_NEWER
            var root = rootVisualElement;
#else
            var root = this.GetRootVisualContainer();
#endif
            visualTree.CloneTree(root);
            root.Bind(new SerializedObject(this));
            

        
            
            var convertTextureSelector = root.Q<ObjectField>("ConvertTextureSelector");
            convertTextureSelector.objectType = typeof(Texture);

            convertTextureSelector.RegisterValueChangedCallback((change) => ConvertTexture.Value = (Texture)change.newValue);

            var baseAvatarTextureSelector = root.Q<ObjectField>("BaseAvatarTextureSelector");
            baseAvatarTextureSelector.objectType = typeof(Texture);
            baseAvatarTextureSelector.RegisterValueChangedCallback((change) => BaseAvatarTexture.Value = (Texture)change.newValue);

            var avatarsView = root.Q<IMGUIContainer>();
            avatarsView.onGUIHandler = () =>
            {
                if (_avatarsData != null)
                {
                    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                    SelectedAvatarIndex.Value = GUILayout.SelectionGrid(SelectedAvatarIndex.Value, _avatarsData.display_names, 2);
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "アバターのデータがダウンロードされていません\nメニューのデータダウンロード > 対応アバター情報の更新からデータのダウンロードをして下さい",
                        MessageType.Info
                    );
                }
            };

            var convertButton = root.Q<Button>();
            convertButton.SetEnabled(false);

            CanStartConvert.ForEachAsync((canStartConvert) => convertButton.SetEnabled(canStartConvert));

            

           
            convertButton.clicked += UniTask.Action(async () =>
            {

                await Convert(ConvertTexture.Value, BaseAvatarTexture.Value, SelectedModelName);

            });

        

        }

        string SelectedModelName => _avatarsData.models[SelectedAvatarIndex.Value];


        private async UniTask Convert(Texture convertTexture,Texture baseAvatarTexture,string modelName) 
        {
            ConvertRunning.Value = true;
            var _gateway = new Gateway();
            var fileName = convertTexture.name + ".png";

            UniTask.Void(async () =>
            {
                while (ConvertRunning.Value)
                {
                    EditorUtility.DisplayProgressBar("Converting", "Your dream come true soon...", _gateway.GetProgress());
                    await UniTask.Yield();
                }
                EditorUtility.ClearProgressBar();
                
            });

            var tex = await _gateway.GetConvertedTexture(fileName, modelName, baseAvatarTexture);

            // 重ねるアバターのテクスチャが設定されていればテクスチャを合成する
            if (baseAvatarTexture != null)
            {
                // Pathからアバターのテクスチャを取得
                var baseTexPath = AssetDatabase.GetAssetPath(baseAvatarTexture);
                var baseTex2D = FilerOperator.GetTexture(baseTexPath);
                tex = TextureUtils.Overlap(overTex: tex, baseTex: baseTex2D);
            }

            // テクスチャデータの保存
            var dir = "ConvertedDreams/" + modelName;
            var creator = new FilerOperator();
            creator.Create(fileName, dir, tex);
            ConvertRunning.Value = false;
            Debug.Log("Converting completed!");
        }

    }
}