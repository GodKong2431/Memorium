using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class ScreenShotTaker : Singleton<ScreenShotTaker>
{
    void Update()
    {
        if (Keyboard.current.f12Key.wasPressedThisFrame)
        {
            string folder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures),"Screenshots");
            
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            
            string filename = "ScreenShot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            string fullPath = Path.Combine(folder, filename);
            
            ScreenCapture.CaptureScreenshot(fullPath);
            
            Debug.Log($"스크린샷 저장 위치 :\n{fullPath}");
        }
    }
}
