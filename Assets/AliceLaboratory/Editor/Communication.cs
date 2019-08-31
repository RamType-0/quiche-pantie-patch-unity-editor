﻿using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Communication {
	private const string URL = "https://s3-ap-northeast-1.amazonaws.com/samurai-blog-media/blog/wp-content/uploads/2018/09/skybox.jpg";
		
	FileCreator creator;

	public IEnumerator GetTexture(Action<Texture2D> callback) {
		Texture2D tex = null;
		var www = UnityWebRequestTexture.GetTexture(URL);
		www.timeout = 5;
		yield return www.SendWebRequest();

		// リクエストが完了するまで待機
		while(!www.isDone) {
			yield return 0;
		}

		if(www.isNetworkError || www.isHttpError) {
			Debug.Log(www.error);
		} else {
			// テクスチャデータを保存
			tex = ((DownloadHandlerTexture)www.downloadHandler).texture;
			creator = new FileCreator();
			creator.Create(tex);
		}

		callback(tex);
	}
}
