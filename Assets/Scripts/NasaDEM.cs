// project https://github.com/unitycoder/nasadem
// signup at at https://www.nasadem.xyz/ to get your API key

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class NasaDEM : MonoBehaviour
{
    public string APIKey = "";
    public Renderer r;

    string url = "https://www.nasadem.xyz/api/v1/dem/";

    public double latitude = 69.7795399;
    public double longitude = 23.3147295;
    [Range(5, 10)]
    public int zoomLevel = 10;

    public bool useCache = true;

    [Tooltip("Cache folder inside StreamingAssets/")]
    public string cacheFolder = "cache";
    string cachePath;


    IEnumerator Start()
    {
        if (string.IsNullOrEmpty(APIKey)) Debug.LogError("Missing API key..", gameObject);
        if (r == null) Debug.LogError("Renderer is null..", gameObject);

        cachePath = Path.Combine(Application.streamingAssetsPath, cacheFolder);
        if (Directory.Exists(cachePath) == false)
        {
            Directory.CreateDirectory(cachePath);
        }

        Texture2D res = null;

        // fetch data
        yield return StartCoroutine(GetDEM(latitude, longitude, zoomLevel, value => res = value));

        // show in mesh
        r.material.mainTexture = res;
    }

    IEnumerator GetDEM(double lat, double lon, double height, System.Action<Texture2D> Result)
    {
        var x = long2tilex(lon, zoomLevel);
        var y = lat2tiley(height, zoomLevel);

        var req = url + zoomLevel + "/" + x + "/" + y + ".png";
        Debug.Log("Request= " + req);

        var cacheFileName = Path.Combine(cachePath, zoomLevel + "_" + x + "_" + y + ".png");
        // TODO add caching check
        if (useCache == true && File.Exists(cacheFileName))
        {
            Debug.Log("Loading cached file: " + cachePath);
            // load cached
            var fileBytes = File.ReadAllBytes(cacheFileName);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(fileBytes);
            Result(tex);
        }
        else // fetch it
        {
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(req))
            {
                www.SetRequestHeader("Authorization", APIKey);

                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    var tex = DownloadHandlerTexture.GetContent(www);
                    Result(tex);

                    // save to cache
                    if (useCache == true)
                    {
                        Debug.Log("Saving cached file: " + cacheFileName);
                        var bytes = tex.EncodeToPNG();
                        File.WriteAllBytes(cacheFileName, bytes);
                    }
                }
            }
        }
    }


    // https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#C.23
    int long2tilex(double lon, int z)
    {
        return (int)(Math.Floor((lon + 180.0) / 360.0 * (1 << z)));
    }

    int lat2tiley(double lat, int z)
    {
        return (int)Math.Floor((1 - Math.Log(Math.Tan(ToRadians(lat)) + 1 / Math.Cos(ToRadians(lat))) / Math.PI) / 2 * (1 << z));
    }

    double tilex2long(int x, int z)
    {
        return x / (double)(1 << z) * 360.0 - 180;
    }

    double tiley2lat(int y, int z)
    {
        double n = Math.PI - 2.0 * Math.PI * y / (double)(1 << z);
        return 180.0 / Math.PI * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
    }

    double ToRadians(double degrees)
    {
        return degrees * 4.0 * Math.Atan(1.0) / 180.0;
    }
}
