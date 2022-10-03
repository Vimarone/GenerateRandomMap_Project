using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;      // Enumerable
using csDelaunay;
using System.IO;

// 정적 멤버 함수로 이루어진 클래스로 만들 생각이지만 아직 정해진건 아님( : MonoBehaviour 삭제)
public class MapDrawer
{
    public static Sprite DrawSprite(Vector2Int size, Color[] colorDatas)
    {
        Texture2D texture = new Texture2D(size.x, size.y);
        texture.filterMode = FilterMode.Point;
        texture.SetPixels(colorDatas);
        texture.Apply();

        Rect rect = new Rect(0, 0, size.x, size.y);
        Sprite sprite = Sprite.Create(texture, rect, Vector2.one * 0.5f);
        return sprite;
    }

    public static Sprite DrawVoronoiToSprite(Voronoi vo, Color cColor, Color eColor, Color[] aColor)
    {
        Rect rect = vo.PlotBounds;
        int width = Mathf.RoundToInt(rect.width);
        int height = Mathf.RoundToInt(rect.height);
        Color[] pixelColors = Enumerable.Repeat(Color.white, width * height).ToArray();
        List<Vector2> siteCoords = vo.SiteCoords();
        List<int> posCenters = new List<int>();

        // 무게 중심 그리기
        foreach(Vector2 coord in siteCoords)
        {
            int x = Mathf.RoundToInt(coord.x);
            int y = Mathf.RoundToInt(coord.y);

            int index = x * width + y;
            pixelColors[index] = cColor;
            posCenters.Add(index);
        }

        Vector2Int size = new Vector2Int(width, height);
        // edge 그리기
        // 모든 폴리곤 정보를 얻어온다
        foreach(Site site in vo.Sites)
        {
            // 해당 폴리곤의 이웃 폴리곤을 가져온다
            List<Site> neighbors = site.NeighborSites();
            foreach(Site neighbor in neighbors)
            {
                // 이웃한 폴리곤들에게 겹치는 가장자리(Edge)를 유도한다
                Edge edge = vo.FindEdgeFromAdjacentPolygons(site, neighbor);
                if (edge.ClippedVertices is null)
                    continue;

                // 가장자리를 이루는 모서리의 꼭지점 2개를 얻어온다
                Vector2 corner1 = edge.ClippedVertices[LR.LEFT];
                Vector2 corner2 = edge.ClippedVertices[LR.RIGHT];
                
                // 1차 함수 그래프를 그리듯 텍스쳐에 선을 그린다
                Vector2 targetPoint = corner1;
                float delta = 1 / (corner2 - corner1).magnitude;
                float lerpRatio = 0;

                while((int)targetPoint.x != (int)corner2.x || (int)targetPoint.y != (int)corner2.y)
                {
                    // 선형 보간 계산을 이용해 corner1과 corner2 사이의 점을 얻어온다
                    targetPoint = Vector2.Lerp(corner1, corner2, lerpRatio);
                    lerpRatio += delta;

                    // 텍스쳐의 좌표는 정수지만 보로노이 다이어그램의 좌표 영역은 실수라서 변환이 필요하다
                    int x = Mathf.Clamp((int)targetPoint.x, 0, size.x - 1);
                    int y = Mathf.Clamp((int)targetPoint.y, 0, size.y - 1);

                    int index = x * size.x + y;
                    pixelColors[index] = eColor;
                }
            }

        }
        // 색칠하기
        // 재귀함수, Tree 이용
        // pixel로 이루어진 선과 면
        // 선의 색(eColor)과 자신의 색(aColor[])을 탐지
        // pixelColors = 스프라이트 전체 색상 정보
        // pixelColors[index]에 원하는 컬러를 넣으면 됨
        // 센터 좌표(posCenters)부터 컬러 삽입

        for (int n = 0; n < posCenters.Count; n++)
        {
            // 1차원 배열을 2차원 배열로 바꾸는 기능
            // index = x * width + y
            int cx = posCenters[n] / width;
            int cy = posCenters[n] % width;
            Color currColor = aColor[n % aColor.Length];

            // 재귀함수 호출 부분
            // 경계선에 닿기 전이나 같은 색상의 픽셀에 닿기 전까지 왼쪽으로 pixel의 색상을 변경하며 전진
            // 경계선이나 같은 색상의 픽셀에 닿았다면 위로 pixel의 색상을 변경하며 전진
            ColorringFace(pixelColors, cx, cy, eColor, currColor, size);

            // 퍼즐 게임(애니팡)
        }
        // =====
        return DrawSprite(size, pixelColors);
    }

    public static void ColorringFace(Color[] pixelColors, int x, int y, Color checkColor, Color targetColor, Vector2Int size)
    {
        if (x >= size.x || x < 0 || y >= size.y || y < 0)
            return;
        if (pixelColors[x + size.x * y] == checkColor)
            return;
        if (pixelColors[x + size.x * y] == targetColor)
            return;
        pixelColors[x + size.x * y] = targetColor;

        ColorringFace(pixelColors, x - 1, y, checkColor, targetColor, size);
        ColorringFace(pixelColors, x + 1, y, checkColor, targetColor, size);
        ColorringFace(pixelColors, x, y - 1, checkColor, targetColor, size);
        ColorringFace(pixelColors, x, y + 1, checkColor, targetColor, size);
        // 모든 픽셀을 뒤지면서 중복 처리만 막아주면 됨
        // 그래픽적으로 생각하지 말고 숫자로 생각할 것
    }

    public static float[] GetRadialGradientMask(Vector2Int size, int maskRadius)
    {
        float[] colorDatas = new float[size.x * size.y];

        // 맵의 중심
        Vector2Int center = size / 2;

        float radius = center.y;// x여도 상관없음
        int index = 0; ;
        for (int y = 0; y < size.y; y++)
        {
            for(int x = 0; x < size.x; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                // 맵의 중심부부터 외곽 방향으로의 거리에 따른 색의 변경(마스킹 범위도 고려한 계산)
                float distFromCenter = Vector2Int.Distance(center, pos) + (radius - maskRadius);
                float colorFactor = distFromCenter / radius;

                // 위의 계산으로는 중심점에 멀수록 1에 가까워진다. 원래는 멀어질수록 0에 가까워져야 함
                colorDatas[index++] = 1 - colorFactor;
            }
        }
        return colorDatas;
    }

    public static void CreateFileFromTexture2D(Texture2D texture, string saveFileName)
    {
        byte[] by = texture.EncodeToPNG();
        string path = Application.dataPath + "/" + saveFileName;  // 어셋 폴더 밑(모바일 버전 사용 불가)
        FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        BinaryWriter bw = new BinaryWriter(fs);
        bw.Write(by);
        bw.Close();
        fs.Close();
    }
}
