using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class ImageProcessorClient : MonoBehaviour
{
    //public float requestInterval = 1f;
    [Header("Settings")]
    //public string ipAddress = "172.22.21.43"; //used to test ping
    public string serverUrl = "http://172.22.21.43:8081/video";
    [Header("Images")]
    public RawImage displayImage;
    public RawImage displayImageRecebida;
    public Texture2D imageToSend;
    [Header("Imagens to change")]
    private int imageIndex = 0;
    private Texture2D[] images;

    public Texture2D i01, i02, i03, i04, i05, i06, i07, i08, i09, i10, i11, i12, i13, i14, i15, i16, i17, i18, i19, i20, i21, i22, i23, i24, i25, i26, i27, i28, i29, i30, i31, i32, i33, i34, i35, i37, i38, i39, i40, i41, i42, i43, i44, i45, i46, i47, i48, i49, i50, i51, i52, i53, i54, i55, i56, i57, i58, i59, i60, i61, i62, i63, i64, i65, i66, i67, i68;

    [Header("Camera")]
    public bool useCameraCapture = false;
    private UnityEngine.Windows.WebCam.PhotoCapture photoCaptureObject = null;
    private Texture2D cameraTexture = null;
    public TextMeshPro butaoCamera;

    int frameCount = 0;
    private List<string> logBuffer = new List<string>();
    private int logWriteInterval = 10; // Write to file every 10 log entries
    private string startTimeBeforeConvert, startTime, endTime;

    void Start()
    {
        Debug.Log("===== Starting ImageProcessorClient =====");
        Debug.Log($"Server URL: {serverUrl}");

        images = new Texture2D[] { i01, i02, i03, i04, i05, i06, i07, i08, i09, i10, i11, i12, i13, i14, i15, i16, i17, i18, i19, i20, i21, i22, i23, i24, i25, i26, i27, i28, i29, i30, i31, i32, i33, i34, i35, i37, i38, i39, i40, i41, i42, i43, i44, i45, i46, i47, i48, i49, i50, i51, i52, i53, i54, i55, i56, i57, i58, i59, i60, i61, i62, i63, i64, i65, i66, i67, i68};

        if (!useCameraCapture && imageToSend == null)
        {
            Debug.LogError("ERRO: imageToSend não foi atribuído no Inspector e useCameraCapture está desativado!");
            return;
        }
        
        if (!useCameraCapture)
        {
            Debug.Log($"Imagem inicial: {imageToSend.width}x{imageToSend.height}, Formato: {imageToSend.format}");
        }
        
        StartCoroutine(SendCameraImageRoutine());
    }

    IEnumerator SendCameraImageRoutine()
    {
        // Inicializa a captura da câmera se useCameraCapture estiver habilitado
        if (useCameraCapture)
        {
            Resolution cameraResolution = UnityEngine.Windows.WebCam.PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

            cameraTexture = new Texture2D(cameraResolution.width, cameraResolution.height);

            UnityEngine.Windows.WebCam.PhotoCapture.CreateAsync(false, delegate (UnityEngine.Windows.WebCam.PhotoCapture captureObject)
            {
                photoCaptureObject = captureObject;

                UnityEngine.Windows.WebCam.CameraParameters c = new UnityEngine.Windows.WebCam.CameraParameters();
                c.hologramOpacity = 0.0f;
                c.cameraResolutionWidth = cameraResolution.width;
                c.cameraResolutionHeight = cameraResolution.height;
                c.pixelFormat = UnityEngine.Windows.WebCam.CapturePixelFormat.BGRA32;

                photoCaptureObject.StartPhotoModeAsync(c, delegate (UnityEngine.Windows.WebCam.PhotoCapture.PhotoCaptureResult result)
                {
                    Debug.Log("Start Photo Mode Result: " + result.success);
                });
            });
            // Aguarda a inicialização da câmera
            while (photoCaptureObject == null)
            {
                yield return null;
            }
        }
        
        while (true)
        {
            frameCount++;
            Debug.Log($"\n===== Starting Frame #{frameCount} =====");
            
            yield return new WaitForEndOfFrame();

            Texture2D textureToSend;
            if (useCameraCapture)
            {
                displayImageRecebida.texture = null;
                // Captura um frame da câmera
                if (photoCaptureObject != null)
                {
                    photoCaptureObject.TakePhotoAsync(delegate (UnityEngine.Windows.WebCam.PhotoCapture.PhotoCaptureResult result, UnityEngine.Windows.WebCam.PhotoCaptureFrame photoCaptureFrame)
                    {
                        if (result.success)
                        {
                            // Copia os dados da câmera para a textura
                            photoCaptureFrame.UploadImageDataToTexture(cameraTexture);
                            cameraTexture.Apply();
                        }
                        else
                        {
                            Debug.LogError("Failed to take photo!");
                        }
                    });
                }
                else
                {
                    Debug.LogWarning("Photo capture object is null, waiting for initialization...");
                    yield return null; // Wait a frame and try again
                    continue; // Skip the rest of the loop and retry
                }

                // Aguarda a captura da foto
                while (cameraTexture.GetPixels().Length == 0)
                {
                    yield return null;
                }
                textureToSend = cameraTexture;
            }
            else
            {
                textureToSend = imageToSend;
                textureToSend = images[imageIndex];
                imageToSend = textureToSend;
            }

            Texture2D rgbTexture = ConvertToRGB24(imageToSend);
            Texture2D processedTexture = ResizeTexture(rgbTexture, 640, 640);
            
            if (rgbTexture != imageToSend)
            {
                Destroy(rgbTexture);
            }

            if (displayImage != null)
            {
                displayImage.texture = processedTexture;
            }

            startTimeBeforeConvert = System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff");

            byte[] imageBytes = processedTexture.EncodeToPNG();
            string base64Image = Convert.ToBase64String(imageBytes);
            
            WWWForm form = new WWWForm();
            form.AddField("imageData", base64Image);

            startTime = System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff");

            //#if UNITY_EDITOR
            //string filePathA = Path.Combine(Application.persistentDataPath, $"{frameCount}input.txt");
            //File.WriteAllBytes(filePathA, base64Image.Select(c => (byte)c).ToArray());
            //Debug.Log($"Saved debug TXT to: {filePathA}");
            //#endif

            using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
            {
                www.downloadHandler = new DownloadHandlerBuffer();
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Error in request: {www.error}");
                }
                else
                {
                    endTime = System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff");

                    //Debug.Log($"Frame #{frameCount}; Image {images[imageIndex]}; Start: {startTime}; End: {endTime}");

                    //Debug.Log("Sucess!");
                    //string responseString = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data);
                    string responseString = www.downloadHandler.text;

                    string[] responseParts = responseString.Split(new char[] { ' ' }, 6);
                    if (responseParts.Length != 6)
                    {
                        Debug.LogError($"Invalid response format. Expected 6 parts, got {responseParts.Length}");
                        yield break;
                    }

                    string imageData = responseParts[0].Trim();
                    string param1 = responseParts[1].Trim(); // Extract parameter 1
                    string param2 = responseParts[2].Trim(); // Extract parameter 2
                    string param3 = responseParts[3].Trim(); // Extract parameter 3
                    string param4 = responseParts[4].Trim(); // Extract parameter 4
                    string param5 = responseParts[5].Trim(); // Extract parameter 5

                    //Debug.Log("test:" + param3);

                    /*#if UNITY_EDITOR
                    string filePathB = Path.Combine(Application.persistentDataPath, $"{frameCount}_response.txt");
                    File.WriteAllBytes(filePathB, responseString.Select(c => (byte)c).ToArray());
                    #endif*/

                    Texture2D texture = DecodeBase64ToTexture(imageData,frameCount);

                    displayImageRecebida.texture = texture;
                    //Debug.Log("Received image displayed successfully!");

                    /*#if UNITY_EDITOR
                    //string filePathC = Path.Combine(Application.persistentDataPath, $"{frameCount}_DEPOISimage.txt");
                    //File.WriteAllBytes(filePathC, imageBytes);
                    string filePathD = Path.Combine(Application.persistentDataPath, $"{frameCount}_DEPOISimage.png");
                    File.WriteAllBytes(filePathD, texture.EncodeToPNG());
                    #endif*/

                    string logEntry = $"{frameCount}; {images[imageIndex]?.name}; {startTimeBeforeConvert}; {startTime}; {endTime}; {param1}; {param2}; {param3}; {param4}; {param5}";
                    //Debug.Log(logEntry);

                    #if UNITY_EDITOR
                    SaveLogToFile(logEntry);
                    #endif
                }
            }

            Destroy(processedTexture);

            // Update the image index every 5 frames
            if (frameCount % 10 == 0 && !useCameraCapture)
            {
                imageIndex = (imageIndex + 1) % images.Length;
                Debug.Log($"Changing image to: {images[imageIndex].name}");
            }
            //yield return new WaitForSeconds(requestInterval);
        }
    }

    // Limpa os recursos da câmera quando o script é desabilitado ou destruído
    private void OnDisable()
    {
        FlushLogToFile();
        if (photoCaptureObject != null)
        {
            photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        }
    }

    void OnStoppedPhotoMode(UnityEngine.Windows.WebCam.PhotoCapture.PhotoCaptureResult result)
    {
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }

    public Texture2D DecodeBase64ToTexture(string base64String, int frameCountA)
    {
        try
        {
            Debug.Log("Decoding base64 image......");//TO BYTES
            byte[] imageBytes = Convert.FromBase64String(base64String);

            // Creates a new texture
            Texture2D texture = new Texture2D(640, 640, TextureFormat.RGB24, false);

            //#if UNITY_EDITOR
            //string filePathA = Path.Combine(Application.persistentDataPath, $"{frameCountA}_image.txt");
            //File.WriteAllBytes(filePathA, imageBytes);
            //string filePathB = Path.Combine(Application.persistentDataPath, $"{frameCountA}_image.png");
            //File.WriteAllBytes(filePathB, texture.EncodeToPNG());
            //#endif

            Debug.Log($"Size of the decoded image: {imageBytes.Length} bytes");
            try
            {
                // Tries to load as compressed image (PNG/JPEG)
                if (texture.LoadImage(imageBytes))
                {
                    Debug.Log("Received image uploaded successfully PNG/JPG!");
                    return texture;
                } else {
                    Debug.LogWarning("Failed to load image as PNG/JPG, to be treated as raw bytes");
                    // If it fails, it treats it as raw bytes
                    texture.Reinitialize(640, 640);
                    texture.LoadRawTextureData(imageBytes);
                    texture.Apply();
                    return texture;
                }
            }
            catch
            {
                Debug.LogWarning("It failed everything again, trying to treat it as raw bytes.");
                texture.LoadRawTextureData(imageBytes);
                texture.Apply();
                return texture;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro ao decodificar imagem: {e.Message}");
            return null;
        }
    }

    private Texture2D ConvertToRGB24(Texture2D source)
    {
        if (source.format == TextureFormat.RGB24)
        {
            return source;
        }

        Texture2D rgbTexture = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt);
        
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        
        rgbTexture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        rgbTexture.Apply();
        
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return rgbTexture;
    }

    private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt);

        Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();

        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    public void ToggleCameraMode()
    {
        useCameraCapture = !useCameraCapture;
        Debug.Log("Camera mode toggled. useCameraCapture is now: " + useCameraCapture);
        if(useCameraCapture)
        {
            butaoCamera.text = "Turn off";
        }
        else
        {
            butaoCamera.text = "Turn On";
        }
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    void SaveLogToFile(string logData)
    {
        logBuffer.Add(logData);

        if (frameCount % logWriteInterval == 0)
        {
            FlushLogToFile();
        }
    }

    void FlushLogToFile()
    {
        if (logBuffer.Count > 0)
        {
            string filePath = Path.Combine(Application.persistentDataPath, "logTest.txt");

            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    foreach (string logEntry in logBuffer)
                    {
                        writer.WriteLine(logEntry);
                    }
                }

                Debug.Log($"Log salvo em: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao salvar o log no arquivo: {e.Message}");
            }

            logBuffer.Clear();
        }
    }

    // Ensure the log is flushed when the application quits
    private void OnApplicationQuit()
    {
        FlushLogToFile();
    }

}
