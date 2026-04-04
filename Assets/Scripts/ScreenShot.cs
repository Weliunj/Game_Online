using UnityEngine;
using System.Collections;
using System.IO;

public class ScreenShot : MonoBehaviour
{
    void Update()
    {
        // Kiểm tra phím bấm (Lưu ý: Nếu dùng Input System mới thì thay đổi như hướng dẫn trước)
        if (Input.GetKeyDown(KeyCode.J))
        {
            StartCoroutine(CaptureScreen());
        }
    }

    IEnumerator CaptureScreen()
    {
        // 1. Chờ kết thúc khung hình để đảm bảo mọi thứ đã được render xong
        yield return new WaitForEndOfFrame();

        // 2. Thiết lập thông số ảnh
        int width = Screen.width;
        int height = Screen.height;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

        // 3. Đọc dữ liệu từ buffer màn hình nạp vào Texture
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        // 4. Chuyển đổi sang định dạng PNG
        byte[] bytes = tex.EncodeToPNG();
        Destroy(tex); // Giải phóng bộ nhớ ngay lập tức

        // 5. XỬ LÝ ĐƯỜNG DẪN LƯU FILE NGOÀI ASSETS
        // ".." nghĩa là nhảy ra khỏi thư mục Assets 1 bậc
        string folderPath = Path.Combine(Application.dataPath, "../MyGameScreenshots");
        
        // Tạo tên file duy nhất dựa trên thời gian thực
        string fileName = "Capture_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        string fullPath = Path.Combine(folderPath, fileName);

        // 6. Kiểm tra và tạo thư mục nếu chưa tồn tại
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // 7. Ghi file xuống ổ cứng
        File.WriteAllBytes(fullPath, bytes);
        
        Debug.Log("<color=green>Ảnh đã lưu tại:</color> " + fullPath);
    }
}