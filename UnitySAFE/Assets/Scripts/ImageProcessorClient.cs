using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;


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
    [Header("Camera")]
    public bool useCameraCapture = false;
    private UnityEngine.Windows.WebCam.PhotoCapture photoCaptureObject = null;
    private Texture2D cameraTexture = null;

    private List<float> processingTimes = new List<float>();

    void Start()
    {
        Debug.Log("===== Starting ImageProcessorClient =====");
        Debug.Log($"Server URL: {serverUrl}");

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
        int frameCount = 0;

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
            float startTime = Time.time;
            
            yield return new WaitForEndOfFrame();


            Texture2D textureToSend;
            if (useCameraCapture)
            {
                // Captura um frame da câmera
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

            byte[] imageBytes = processedTexture.EncodeToPNG();
            string base64Image = Convert.ToBase64String(imageBytes);
            
            WWWForm form = new WWWForm();
            form.AddField("imageData", base64Image);

            //#if UNITY_EDITOR
            //string filePathA = Path.Combine(Application.persistentDataPath, $"{frameCount}input.txt");
            //File.WriteAllBytes(filePathA, base64Image.Select(c => (byte)c).ToArray());
            //Debug.Log($"Saved debug TXT to: {filePathA}");
            //#endif

            using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
            {
                www.downloadHandler = new DownloadHandlerBuffer();
                yield return www.SendWebRequest();

                float endTime = Time.time;
                float elapsedTime = endTime - startTime;
                processingTimes.Add(elapsedTime);
                Debug.Log($"Frame #{frameCount} took {elapsedTime} seconds to process");

                if (processingTimes.Count % 10 == 0) // Every 10 frames, log stats
                {
                    LogProcessingStats();
                }

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Error in request: {www.error}");
                }
                else
                {
                    Debug.Log("Sucess!");
                    //string responseString = System.Text.Encoding.UTF8.GetString(www.downloadHandler.data);
                    string responseString = www.downloadHandler.text;

                    string[] responseParts = responseString.Split(new char[] { ' ' }, 3);
                    if (responseParts.Length != 3)
                    {
                        Debug.LogError($"Invalid response format. Expected 3 parts, got {responseParts.Length}");
                        yield break;
                    }

                    string imageData = responseParts[0].Trim();

                    #if UNITY_EDITOR
                    string filePathB = Path.Combine(Application.persistentDataPath, $"{frameCount}_response.txt");
                    File.WriteAllBytes(filePathB, imageData.Select(c => (byte)c).ToArray());
                    #endif

                    Texture2D texture = DecodeBase64ToTexture(imageData,frameCount);

                    displayImageRecebida.texture = texture;
                    Debug.Log("Received image displayed successfully!");

                    #if UNITY_EDITOR
                    //string filePathC = Path.Combine(Application.persistentDataPath, $"{frameCount}_DEPOISimage.txt");
                    //File.WriteAllBytes(filePathC, imageBytes);
                    string filePathD = Path.Combine(Application.persistentDataPath, $"{frameCount}_DEPOISimage.png");
                    File.WriteAllBytes(filePathD, texture.EncodeToPNG());
                    #endif
                }
            }

            Destroy(processedTexture);
            //yield return new WaitForSeconds(requestInterval);
        }
    }

    // Limpa os recursos da câmera quando o script é desabilitado ou destruído
    private void OnDisable()
    {
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

    void LogProcessingStats()
    {
        if (processingTimes.Count == 0) return;
        float mean = processingTimes.Average();
        float stdDev = Mathf.Sqrt(processingTimes.Average(v => Mathf.Pow(v - mean, 2)));
        Debug.Log($"[Processing Stats] Mean: {mean:F3} sec, Std Dev: {stdDev:F3} sec");
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
}
