using System.IO;
using DivineSense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DivineSenseEditor {
public static class DivineSensePreview {
 public static void Render(){
 EditorSceneManager.OpenScene("Assets/Scenes/Archive/DivineSense_Arena.unity");
 var session=Object.FindFirstObjectByType<GameSession>();var settings=session.Settings;
 Random.InitState(461);Object.FindFirstObjectByType<PlatformSpawner>().PopulateInitialPlatforms();
 var camera=session.World.Camera;camera.backgroundColor=new Color(.13f,.23f,.34f);var rt=new RenderTexture(1280,800,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();File.WriteAllBytes("/tmp/divine-camera-preview.png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(image);Debug.Log("DIVINE_PREVIEW_OK");
 }
}
}
