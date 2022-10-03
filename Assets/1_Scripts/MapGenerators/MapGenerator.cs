using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DefineEnumHelper;

public class MapGenerator : MonoBehaviour
{
    [Header("맵 자원")]
    [SerializeField] GameObject[] _prefabOreObject;
    [SerializeField] Texture2D _perlinNoiseMap;
    [SerializeField] Texture2D _biomMap;
    [Header("맵 기본 정보")]
    [SerializeField] Transform _mapRoot;
    [SerializeField] int _mapMaxHeight = 128;   // 공기도 블록의 일종(3차원 입방체로 만들어야 함)
    [SerializeField] float _floorLevelCount = 12;
    
    [SerializeField] int _mapGroundHeightOffset = 30;
    [SerializeField] int _snowOnHeight = 36;
    [SerializeField] int _stonOnHeight = 34;
    [SerializeField] int _forestOnHeight = 32;
    [SerializeField] int _grassOnHeight = 31;
    [SerializeField] int _soilOnHeight = 1;
    
    [SerializeField] float _stoneGenRatio = 25;
    [SerializeField] float _ironGenRatio = 10;
    [SerializeField] float _goldGenRatio = 4;
    [SerializeField] float _diamondGenRatio = 1;

    [SerializeField] int _caveSizeOffset = 70;
    [SerializeField] int _cloudSizeOffset = 50;


    Block[,,] _worldBlocks;

    void Start()
    {
        StartCoroutine(MapLoad(_perlinNoiseMap));
    }
    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            RaycastHit rHit;
            Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(r, out rHit))
            {
                // 제거 및 isView 체크
                // rHit = 블록
                // 블록의 위치를 이용해 월드블록 좌표로 주변 체크(null 여부 확인)
                Vector3 pos = new Vector3(rHit.transform.position.x, rHit.transform.position.y, rHit.transform.position.z);
                if (_worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z]._type != eOreType.UnDestroyed)
                {
                    Destroy(rHit.transform.gameObject);
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            for (int k = -1; k <= 1; k++)
                            {
                                if (i == 0 && j == 0 && k == 0)
                                    continue;
                                if (pos.y + j < rHit.transform.position.y)
                                {
                                    Block tempBlock = _worldBlocks[(int)pos.x + i, (int)pos.y + j, (int)pos.z + k];
                                    if (tempBlock != null && !tempBlock._isView)
                                        CreateBlock((int)pos.y + j, new Vector3(pos.x + i, pos.y + j, pos.z + k), true);
                                }
                            }
                        }
                    }
                }                
            }
        }
    }

    IEnumerator MapLoad(Texture2D mapInfo)
    {
        Vector2Int size = new Vector2Int(mapInfo.width, mapInfo.height);
        _worldBlocks = new Block[size.x, _mapMaxHeight, size.y];

        // 맵 지형을 만든다
        yield return GenerateBlockMap(mapInfo, size);

        // 동굴 지형을 만든다
        yield return GenerateBlockCave(3, 150);

        yield return GenerateBlockCloud(5, 50);
    }
    IEnumerator GenerateBlockMap(Texture2D mapInfo, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                if (mapInfo.GetPixel(x, z).r < (1 - 1f / _floorLevelCount))
                {
                    int y = Mathf.RoundToInt(mapInfo.GetPixel(x, z).grayscale * _floorLevelCount);
                    y += _mapGroundHeightOffset;

                    Vector3 pos = new Vector3(x, y - 0.5f, z);
                    CreateBlock(y, pos, true);
                    while (y-- > 0)
                    {
                        pos = new Vector3(x, y - 0.5f, z);
                        CreateBlock(y, pos, false);
                    }
                }
            }
            yield return null;
        }
    }
    IEnumerator GenerateBlockCave(int caveCount, int caveSize)
    {
        // 임의의 위치에서 null이 아닌 임의의 방향으로 땅을 파나가는 것
        // 현재 하나를 파면 주변 블록이 생김
        // 블록 배열에서 null로만 만들어주면 됨
        // 없애는 크기에 따라 동굴의 크기가 달라질 것임
        // 불필요한 움직임까지 고려하여 파야함
        // 동굴의 크기와 개수를 결정해야함

        // 구멍 크기
        int holeSize = 1;
        // 동굴 개수만큼 동굴 생성
        for(int n = 0; n < caveCount; n++)
        {
            // 최초 위치 선정(맵보다 밑에 위치하도록 해야 한다)
            int posX = Random.Range(holeSize, _perlinNoiseMap.width - holeSize);
            int posY = Random.Range(holeSize, _mapGroundHeightOffset - holeSize - 5);   // 표면보단 밑에 있기를 바라는 마음에서 마이너스 5
            int posZ = Random.Range(holeSize, _perlinNoiseMap.height - holeSize);

            // 동굴 크기의 변화를 위한 계산
            int cSize = caveSize + Random.Range(0, _caveSizeOffset);

            // 크기만큼 파도록 한다(단, 바닥일 경우 pass)
            for(int m = 0; m < cSize; m++)
            {
                // 구멍 크기만큼 판다
                for(int x = -holeSize; x <= holeSize; x++)
                {
                    for(int y = -holeSize; y <= holeSize; y++)
                    {
                        for(int z = -holeSize; z <= holeSize; z++)
                        {
                            Vector3 blockPos = new Vector3(posX + x, posY + y, posZ + z);
                            if(_worldBlocks[(int)blockPos.x, (int)blockPos.y, (int)blockPos.z] != null)
                            {
                                Block now = _worldBlocks[(int)blockPos.x, (int)blockPos.y, (int)blockPos.z];
                                // 자원 타일이나 바닥 타일일 경우 삭제 금지
                                if (now._type == eOreType.UnDestroyed || now._type >= eOreType.Stone && now._type <= eOreType.Diamond)
                                    continue;
                                else
                                {
                                    Destroy(now._objBlock);
                                    _worldBlocks[(int)blockPos.x, (int)blockPos.y, (int)blockPos.z] = null;
                                }
                            }
                        }
                    }
                }

                // 랜덤하게 위치를 변경(기준 높이를 넘지 않도록 제한)
                while (true)
                {
                    posX += Random.Range(-1, 2);            // -1 ~ 1 랜덤
                    if (posX < holeSize || posX >= _perlinNoiseMap.width - holeSize)
                        continue;
                    else
                        break;
                }
                while (true)
                {
                    posZ += Random.Range(-1, 2);            // -1 ~ 1 랜덤
                    if (posZ < holeSize || posZ >= _perlinNoiseMap.height - holeSize)
                        continue;
                    else
                        break;
                }
                while (true)
                {
                    posY += Random.Range(-1, 2);            // -1 ~ 1 랜덤
                    if (posY < holeSize || posY >= _mapGroundHeightOffset - 4 - holeSize)
                        continue;
                    else
                        break;
                }
            }
            yield return null;
        }

        // caveCount만큼 만들고 난 후 주변 벽을 보이게 한다
        for(int z = 1; z < _perlinNoiseMap.height - 1; z++)
        {
            for(int x = 1; x < _perlinNoiseMap.width - 1; x++)
            {
                for(int y = 1; y < _mapGroundHeightOffset; y++)
                {
                    if (_worldBlocks[x, y, z] == null)
                    {
                        for (int z1 = -1; z1 <= 1; z1++)
                        {
                            for (int x1 = -1; x1 <= 1; x1++)
                            {
                                for (int y1 = -1; y1 <= 1; y1++)
                                {
                                    if (!(x1 == 0 && y1 == 0 && z1 == 0))
                                    {
                                        Block neighbor = _worldBlocks[x + x1, y + y1, z + z1];
                                        if(neighbor != null)
                                        {
                                            Vector3 neighborPos = new Vector3(x + x1, y + y1, z + z1);
                                            DrawBlock(neighborPos);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        yield return null;
    }
    IEnumerator GenerateBlockCloud(int cloudCount, int cloudSize)
    {
        // 하늘 위에 구름 블록 만들기
        // 1. 일정 높이 이상의 null인 블럭 찾기
        // 2. 생성
        // 3. null하고 맞닿은 부분만 create

        // 구멍 크기
        int holeSize = 1;
        // 구름 개수만큼 구름 생성
        for (int n = 0; n < cloudCount; n++)
        {
            // 최초 위치 선정(맵보다 위에 위치하도록 해야 한다)
            int posX = Random.Range(holeSize, _perlinNoiseMap.width - holeSize);
            int posY = Random.Range(_mapGroundHeightOffset + holeSize + 20, _mapMaxHeight - holeSize);
            int posZ = Random.Range(holeSize, _perlinNoiseMap.height - holeSize);

            // 구름 크기의 변화를 위한 계산
            int cSize = cloudSize + Random.Range(0, _cloudSizeOffset);

            // 크기만큼 파도록 한다(단, 바닥일 경우 pass)
            for (int m = 0; m < cSize; m++)
            {
                // 구멍 크기만큼 판다
                for (int x = -holeSize; x <= holeSize; x++)
                {
                    for (int y = -holeSize; y <= holeSize; y++)
                    {
                        for (int z = -holeSize; z <= holeSize; z++)
                        {
                            Vector3 blockPos = new Vector3(posX + x, posY + y, posZ + z);
                            if (_worldBlocks[(int)blockPos.x, (int)blockPos.y, (int)blockPos.z] == null)
                            {
                                //_worldBlocks[(int)blockPos.x, (int)blockPos.y, (int)blockPos.z] = new Block(eOreType.Sky, false, null);
                                CreateBlock((int)blockPos.y, blockPos, false);
                            }
                        }
                    }
                }

                // 랜덤하게 위치를 변경(기준 높이를 넘지 않도록 제한)
                while (true)
                {
                    posX += Random.Range(-1, 2);            // -1 ~ 1 랜덤
                    if (posX < holeSize || posX >= _perlinNoiseMap.width - holeSize)
                        continue;
                    else
                        break;
                }
                while (true)
                {
                    posZ += Random.Range(-1, 2);            // -1 ~ 1 랜덤
                    if (posZ < holeSize || posZ >= _perlinNoiseMap.height - holeSize)
                        continue;
                    else
                        break;
                }
                while (true)
                {
                    posY += Random.Range(-1, 2);            // -1 ~ 1 랜덤
                    if (posY >= _mapMaxHeight - holeSize || posY < _mapGroundHeightOffset + holeSize + 20)
                        continue;
                    else
                        break;
                }
            }
            yield return null;
        }

        // cloudCount만큼 만들고 난 후 주변 벽을 보이게 한다
        for (int z = 1; z < _perlinNoiseMap.height - 1; z++)
        {
            for (int x = 1; x < _perlinNoiseMap.width - 1; x++)
            {
                for (int y = _mapGroundHeightOffset + 21; y < _mapMaxHeight - 1; y++)
                {
                    if (_worldBlocks[x, y, z] == null)
                    {
                        for (int z1 = -1; z1 <= 1; z1++)
                        {
                            for (int x1 = -1; x1 <= 1; x1++)
                            {
                                for (int y1 = -1; y1 <= 1; y1++)
                                {
                                    if (!(x1 == 0 && y1 == 0 && z1 == 0))
                                    {
                                        Block neighbor = _worldBlocks[x + x1, y + y1, z + z1];
                                        if (neighbor != null)
                                        {
                                            Vector3 neighborPos = new Vector3(x + x1, y + y1, z + z1);
                                            CreateBlock((int)neighborPos.y, neighborPos, true);
                                            //Destroy(neighbor._objBlock);
                                            //_worldBlocks[(int)neighborPos.x, (int)neighborPos.y, (int)neighborPos.z] = null;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        yield return null;
    }
    void CreateBlock(int y, Vector3 pos, bool isView)
    {
        float mineral = Random.Range(0, 100);
        Color mineralColor = _biomMap.GetPixel((int)pos.x, (int)pos.z);
        eOreType mineralType;

        if (mineralColor.Equals(Color.blue))
            mineralType = eOreType.Iron;
        else if (mineralColor.Equals(Color.red))
            mineralType = eOreType.Gold;
        else if (mineralColor.Equals(Color.cyan))
            mineralType = eOreType.Diamond;
        else if (mineralColor.Equals(Color.gray))
            mineralType = eOreType.Stone;
        else
            mineralType = eOreType.Soil;

        if (y == 0)
        {
            GameObject go = Instantiate(_prefabOreObject[(int)eOreType.UnDestroyed], pos, Quaternion.identity, _mapRoot);
            _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.UnDestroyed, true, go);
        }else if(y >= _mapGroundHeightOffset + 20)
        {
            if (isView)
            {
                GameObject go = Instantiate(_prefabOreObject[(int)eOreType.Snow], pos, Quaternion.identity, _mapRoot);
                _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Snow, true, go);
            }
            else
                _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Snow, false, null);
        }
        else if (mineral < _diamondGenRatio && mineralType == eOreType.Diamond)
        {
            if (isView)
            {
                GameObject go = Instantiate(_prefabOreObject[(int)eOreType.Diamond], pos, Quaternion.identity, _mapRoot);
                _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Diamond, true, go);
            }
            else
            {
                _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Diamond, false, null);
            }
        }
        else if(mineral < _diamondGenRatio + _goldGenRatio && mineralType == eOreType.Gold)
        {
            if (isView)
            {
                GameObject go = Instantiate(_prefabOreObject[(int)eOreType.Gold], pos, Quaternion.identity, _mapRoot);
                _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Gold, true, go);
            }
            else
            {
                _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Gold, false, null);
            }
        }
        else if (mineral < _diamondGenRatio + _goldGenRatio + _ironGenRatio && mineralType == eOreType.Iron)
        {
            if (isView)
            {
                GameObject go = Instantiate(_prefabOreObject[(int)eOreType.Iron], pos, Quaternion.identity, _mapRoot);
                _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Iron, true, go);
            }
            else
            {
                _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Iron, false, null);
            }
        }
        else if (mineral < _diamondGenRatio + _goldGenRatio + _ironGenRatio + _stoneGenRatio && mineralType == eOreType.Stone)
        {
            if (isView)
            {
                GameObject go = Instantiate(_prefabOreObject[(int)eOreType.Stone], pos, Quaternion.identity, _mapRoot);
                _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Stone, true, go);
            }
            else
            {
                _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Stone, false, null);
            }
        }
        else
        {
            if (y >= _snowOnHeight)
            {
                if (isView)
                {
                    GameObject go = Instantiate(_prefabOreObject[(int)eOreType.Snow], pos, Quaternion.identity, _mapRoot);
                    _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Snow, true, go);
                }
                else
                    _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Snow, false, null);
            }
            else if (y >= _stonOnHeight)
            {
                if (isView)
                {
                    GameObject go = Instantiate(_prefabOreObject[(int)eOreType.Stone], pos, Quaternion.identity, _mapRoot);
                    _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Stone, true, go);
                }
                else
                    _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Stone, false, null);
            }
            else if (y >= _forestOnHeight)
            {
                if (isView)
                {
                    GameObject go = Instantiate(_prefabOreObject[(int)eOreType.Forest], pos, Quaternion.identity, _mapRoot);
                    _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Forest, true, go);
                }
                else
                    _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Forest, false, null);
            }
            else if (y >= _grassOnHeight)
            {
                if (isView)
                {
                    GameObject go = Instantiate(_prefabOreObject[(int)eOreType.Grass], pos, Quaternion.identity, _mapRoot);
                    _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Grass, true, go);
                }
                else
                    _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Grass, false, null);
            }
            else if (y >= _soilOnHeight)
            {
                if (isView)
                {
                    GameObject go = Instantiate(_prefabOreObject[(int)eOreType.Soil], pos, Quaternion.identity, _mapRoot);
                    _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Soil, true, go);
                }
                else
                    _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Soil, false, null);
            }
            else
            {
                if (isView)
                {
                    GameObject go = Instantiate(_prefabOreObject[(int)eOreType.Grass], pos, Quaternion.identity, _mapRoot);
                    _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Grass, true, null);
                }
                else
                    _worldBlocks[(int)pos.x, (int)pos.y, (int)pos.z] = new Block(eOreType.Grass, false, null);
            }
        }
        
    }
    void FindingBlockCheckSurrounding(Vector3 blockPos) 
    {

    }
    void DrawBlock(Vector3 neighbor)
    {
        CreateBlock((int)neighbor.y, neighbor, true);
    }
    
}
