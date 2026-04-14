using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    // Hàm này sẽ được gọi khi Toggle UI thay đổi trạng thái (Check/Uncheck)
    public void SetFullscreen(bool isFullscreen)
    {
        Debug.Log($"[SettingsManager] SetFullscreen được gọi với giá trị: {isFullscreen}");

        if (isFullscreen)
        {
            // Chuyển sang Fullscreen (sử dụng độ phân giải gốc của màn hình hiện tại)
            Resolution currentRes = Screen.currentResolution;
            Screen.SetResolution(currentRes.width, currentRes.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            // Chuyển về chế độ cửa sổ (Windowed) với kích thước 960x540
            Screen.SetResolution(960, 540, FullScreenMode.Windowed);
        }
    }
}