﻿using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

namespace AliceLaboratory.Editor {
	public class Gateway {
		private const string DREAMS_BASE_URL = "https://labten.net/pantie-patch/api/dream/";
		private const string CONVERTING_BASE_URL = "https://labten.net/pantie-patch/api/convert/";

		private float _progress = 0.0f;
		private WWW www;

		FilerOperator creator;


		public Gateway() {
			www = new WWW(DREAMS_BASE_URL);
		}

		public Gateway(string fileName, string modelName) {
			www = new WWW(CONVERTING_BASE_URL + modelName + "/" + fileName);
		}
		
		public async UniTask<Dream> GetDreamsData() 
		{
			var request = UnityWebRequest.Get(DREAMS_BASE_URL);

			await request.SendWebRequest();

			_progress = request.downloadProgress;

			if (request.isNetworkError || request.isHttpError)
			{
				Debug.LogError(request.error);
				return null;
			}

			return JsonUtility.FromJson<Dream>(request.downloadHandler.text);
		}

		public async UniTask<AvatarsData> GetAvatarsData()
        {
			var request = UnityWebRequest.Get(CONVERTING_BASE_URL);

			await request.SendWebRequest();

			_progress = request.downloadProgress;

			if (request.isNetworkError || request.isHttpError)
			{
				Debug.LogError(request.error);
				return null;
			}

			return JsonUtility.FromJson<AvatarsData>(request.downloadHandler.text);
        }

		public async UniTask<Texture2D> GetDreamTexture(string fileName)
		{
			var request = UnityWebRequestTexture.GetTexture(DREAMS_BASE_URL + fileName);

			await request.SendWebRequest();

			_progress = request.downloadProgress;

			if (request.isNetworkError || request.isHttpError)
            {
				Debug.LogError(request.error);
				return null;
            }

			Texture2D tex = DownloadHandlerTexture.GetContent(request);

			return tex;
		}

		public GatewayState GetConvertedTexture(string fileName, string modelName, Texture baseTex) {
			Texture2D tex;

			www.MoveNext();
			
			// リクエストが完了した時の処理
			if (www.isDone) {
				tex = www.texture;

				// 重ねるアバターのテクスチャが設定されていればテクスチャを合成する
				if (baseTex != null) {
                    // Pathからアバターのテクスチャを取得
                    var baseTexPath = AssetDatabase.GetAssetPath(baseTex);
					var baseTex2D = FilerOperator.GetTexture(baseTexPath);
					tex = TextureUtils.Overlap(overTex:tex, baseTex:baseTex2D);
				}

				var dir = "ConvertedDreams/" + modelName;
				// テクスチャデータの保存
				creator = new FilerOperator();
				creator.Create(fileName, dir, tex);
			
				return GatewayState.GETTING_CONVERTED_TEXTURE_COMPLETED;
			}
			
			return GatewayState.GETTING_CONVERTED_TEXTURE;
		}

		public float GetProgress() {
			return _progress;
		}

		public void Clear() {
			www.Dispose();
		}
	}
}
