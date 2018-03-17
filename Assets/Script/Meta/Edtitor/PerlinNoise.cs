using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinNoise : MonoBehaviour { 
    
    public int pixWidth = 1000;
    public int pixHeight = 1000;
    public float scale = 1.0F; 
    private Texture2D noiseTex;
    private Color[] pix;
    private Renderer rend;

    void Start()
    {
        rend = GetComponent<MeshRenderer>();
        noiseTex = rend.material.GetTexture("_PerlinNoiseTexture") as Texture2D;
        if (noiseTex == null) {
            noiseTex = new Texture2D(pixWidth, pixHeight);
            rend.material.SetTexture("_PerlinNoiseTexture", noiseTex);
        }

        pix = new Color[noiseTex.width * noiseTex.height];
    }
    void Update()
    {
        CalcNoise();
    }

    void CalcNoise()
    {
        var Colors = noiseTex.GetPixels();
        Debug.LogFormat("{0}<{1}<{2}", Colors.Length, noiseTex.width, noiseTex.height);
        Debug.Log (pix[0].r); Debug.Log(pix[3].r); Debug.Log(pix[5].r);
        float y = 0.0F;
        while (y < noiseTex.height)
        {
            float x = 0.0F;
            while (x < noiseTex.width)
            {
                var color = Colors[(int)y * noiseTex.width + (int)x]; 

                float xCoord = x / noiseTex.width * scale;
                float yCoord = y / noiseTex.height * scale;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);

                var Color = 
                pix[(int)y * noiseTex.width + (int)x] = new Color(sample, sample, sample);
                
                x++;
            }
            y++;
        } 
        noiseTex.SetPixels(pix);
        noiseTex.Apply();
    }
}
