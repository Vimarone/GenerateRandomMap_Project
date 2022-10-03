using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using csDelaunay;
using DefineEnumHelper;

public class CreateMapTex : MonoBehaviour
{
    [Header("맵 기본정보")]
    [SerializeField] Vector2Int _size;
    [SerializeField] int _nodeAmount = 0;
    [SerializeField] int _loydIterateCount = 0;
    [SerializeField, Range(0f, 0.4f)] float _noiseFrequency = 0;
    [SerializeField] int _noiseOctave = 0;
    [SerializeField] int _seed = 100;
    [SerializeField] int _noiseMaskingRadius = 0;
    [Header("맵 View")]
    [SerializeField] SpriteRenderer _voronoiMapRenderer = null;
    [SerializeField] SpriteRenderer _noiseMapRenderer = null;
    [Header("맵 수정값")]
    [SerializeField] Color[] _areaColors;
    [SerializeField] Color _centerodColor = Color.cyan;
    [SerializeField] Color _edgeColor = Color.black;
    //[SerializeField, Range(0f, 0.5f)] float _landNoiseThreshold = 0;
    // 다섯 단계로 나누기
    [SerializeField, Range(2, 20)] int _floorLevelCount = 2;


    void Awake()
    {
        Voronoi vo = GenerateVoronoi(_size, _nodeAmount, _loydIterateCount);

        _voronoiMapRenderer.sprite = MapDrawer.DrawVoronoiToSprite(vo, _centerodColor, _edgeColor, _areaColors);
    }

    void LateUpdate()
    {
        GenerateMap();
    }


    Voronoi GenerateVoronoi(Vector2Int size, int nodeAmount, int loydIterateCount)
    {
        // nodeAmount만큼 랜덤하게 특이점의 정보를 생성
        List<Vector2> centroids = new List<Vector2>();
        for(int n = 0; n < nodeAmount; n++)
        {
            int rx = Random.Range(0, size.x);
            int ry = Random.Range(0, size.y);
            centroids.Add(new Vector2(rx, ry));
        }

        // 실제로 특이점을 찍을 수 있도록 정보를 넘겨줌
        Rect rt = new Rect(0, 0, size.x, size.y);
        Voronoi vo = new Voronoi(centroids, rt, loydIterateCount);
        return vo;
    }

    // 중간 과정(노이즈 맵)
    float[] CreateMapShape(Vector2Int size, float frequency, int octave)
    {
        // seed가 없으면(0이라면) 노이즈가 생성되지 않으므로 0이면 랜덤값 부여
        int seed = (_seed == 0) ? Random.Range(1, int.MaxValue) : _seed;

        FastNoiseLite noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);    // Fractional Brownian motion
        noise.SetFrequency(frequency);
        noise.SetFractalOctaves(octave);
        noise.SetSeed(seed);

        float[] mask = MapDrawer.GetRadialGradientMask(size, _noiseMaskingRadius);

        // 색은 0 ~ 1 사이의 범위이며 0은 검은색, 1은 흰색을 나타낸다
        float[] colorDatas = new float[size.x * size.y];
        int index = 0;
        for(int y = 0; y < size.y; y++)
        {
            for(int x = 0; x < size.x; x++)
            {
                float noiseColorFactor = noise.GetNoise(x, y);
                // 노이즈 값의 범위가 -1 ~ 1이기 때문에 0 ~ 1 범위의 값으로 변환하기 위한 계산
                //noiseColorFactor = (noiseColorFactor + 1) * 0.5f;
                //colorDatas[index++] = noiseColorFactor;

                // 특정 값 이하의 노이즈는 모두 0
                //float color = noiseColorFactor > _landNoiseThreshold ? 1f : 0f;
                //colorDatas[index++] = color;

                // 기준값의 개수 = count
                // 단계별 노이즈
                noiseColorFactor = (noiseColorFactor + 1) * 0.5f;
                noiseColorFactor *= mask[index];
                float color = (noiseColorFactor >= (1 - 1f / _floorLevelCount)) ? 1f : ((int)(noiseColorFactor * _floorLevelCount) / (float)_floorLevelCount);
                colorDatas[index++] = color;
            }
        }
        return colorDatas;
    }

    // 실질적인 과정(노이즈 맵)
    public void GenerateMap()
    {
        float[] noiseColors = CreateMapShape(_size, _noiseFrequency, _noiseOctave);
        Color[] colors = new Color[noiseColors.Length];
        for(int n = 0; n < colors.Length; n++)
        {
            byte[] color = System.BitConverter.GetBytes(noiseColors[n]);
            float r = noiseColors[n];  /*color[0] / 255.0f;*/
            float g = noiseColors[n];  /*color[1] / 255.0f;*/
            float b = noiseColors[n];  /*color[2] / 255.0f;*/
            colors[n] = new Color(r, g, b, 1);
        }

        _noiseMapRenderer.sprite = MapDrawer.DrawSprite(_size, colors);
    }

    void OnGUI()
    {
        if(GUI.Button(new Rect(0, 0, 220, 60), "바이옴 Map 생성"))
        {
            MapDrawer.CreateFileFromTexture2D(_voronoiMapRenderer.sprite.texture, "biom.png");
        }
        if (GUI.Button(new Rect(0, 65, 220, 60), "프렉탈 Map 생성"))
        {
            MapDrawer.CreateFileFromTexture2D(_noiseMapRenderer.sprite.texture, "rand.png");
        }
    }

}
