﻿//using UnityEngine;
//using System.Collections;
//using System.Linq;

//[CreateAssetMenu()]
//public class TextureData : UpdatableData
//{

//	const int textureSize = 512;
//	const TextureFormat textureFormat = TextureFormat.RGB565;

//	public Layer[] layers;

//	float savedMinHeight;
//	float savedMaxHeight;

//	public void ApplyToMaterial(Material material)
//	{

//		material.SetInt("layerCount", layers.Length);
//		material.SetColorArray("baseColours", layers.Select(x => x.tint).ToArray());
//		material.SetFloatArray("baseStartHeights", layers.Select(x => x.startHeight).ToArray());
//		material.SetFloatArray("baseBlends", layers.Select(x => x.blendStrength).ToArray());
//		material.SetFloatArray("baseColourStrength", layers.Select(x => x.tintStrength).ToArray());
//		material.SetFloatArray("baseTextureScales", layers.Select(x => x.textureScale).ToArray());
//		Texture2DArray texturesArray = GenerateTextureArray(layers.Select(x => x.texture).ToArray());
//		material.SetTexture("baseTextures", texturesArray);

//		UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
//	}

//	public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
//	{
//		savedMinHeight = minHeight;
//		savedMaxHeight = maxHeight;

//		material.SetFloat("minHeight", minHeight);
//		material.SetFloat("maxHeight", maxHeight);
//	}

//	Texture2DArray GenerateTextureArray(Texture2D[] textures)
//	{
//		Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
//		for (int i = 0; i < textures.Length; i++)
//		{
//			textureArray.SetPixels(textures[i].GetPixels(), i);
//		}
//		textureArray.Apply();
//		return textureArray;
//	}

//	[System.Serializable]
//	public class Layer
//	{
//		public Texture2D texture;
//		public Color tint;
//		[Range(0, 1)]
//		public float tintStrength;
//		[Range(0, 1)]
//		public float startHeight;
//		[Range(0, 1)]
//		public float blendStrength;
//		public float textureScale;
//	}


//}

using UnityEngine;
using System.Linq;
[CreateAssetMenu()]
public class TextureData : UpdatableData
{
    public Layer[] layers;


    private const int textureSize = 512;
    private const TextureFormat textureFormat = TextureFormat.RGB565;

    private float savedMinHeight;
    private float savedMaxHeight;


    private int layerCountID = Shader.PropertyToID("layerCount");
    private int baseColorsID = Shader.PropertyToID("baseColors");
    private int baseColorStrengthID = Shader.PropertyToID("baseColorStrength");
    private int baseStartHeightID = Shader.PropertyToID("baseStartHeight");
    private int baseBlendsID = Shader.PropertyToID("baseBlends");
    private int baseTextureScaleID = Shader.PropertyToID("baseTextureScale");

    public void ApplyToMaterial(Material material)
    {

        material.SetInt(layerCountID, layers.Length);
        material.SetColorArray(baseColorsID, layers.Select(x => x.tint).ToArray());
        material.SetFloatArray(baseStartHeightID, layers.Select(x => x.startHeight).ToArray());
        material.SetFloatArray(baseBlendsID, layers.Select(x => x.blendStrenght).ToArray());
        material.SetFloatArray(baseColorStrengthID, layers.Select(x => x.tintStrength).ToArray());
        material.SetFloatArray(baseTextureScaleID, layers.Select(x => x.textureScale).ToArray());
        Texture2DArray texturesArray = GenerateTextureArray(layers.Select(x => x.texture).ToArray());
        material.SetTexture("baseTextures", texturesArray);

        UpdateMeshHeight(material, savedMinHeight, savedMaxHeight);
    }

    public void UpdateMeshHeight(Material material, float minHeight, float maxHeight)
    {
        savedMinHeight = minHeight;
        savedMaxHeight = maxHeight;
        Debug.Log("heights updated");
        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }

    private Texture2DArray GenerateTextureArray(Texture2D[] textures)
    {
        Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
        for (int i = 0; i < textures.Length; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }
        textureArray.Apply();
        return textureArray;
    }


    [System.Serializable]
    public class Layer
    {
        public Texture2D texture;
        public Color tint;
        [Range(0, 1)]
        public float tintStrength;
        [Range(0, 1)]
        public float startHeight;
        [Range(0, 1)]
        public float blendStrenght;
        public float textureScale;
    }

}
