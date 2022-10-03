using UnityEngine;
using DefineEnumHelper;

public class Block
{
    // 블록이 가지고 있어야 하는 정보
    // 종류
    // 위치 정보 = transform
    // 시각화 여부
    // 시각화할 오브젝트 정보
    // 최대 25개 체크 후 

    public eOreType _type
    {
        get; set;
    }
    public bool _isView
    {
        get; set;
    }
    public GameObject _objBlock
    {
        get; set;
    }

    public Block(eOreType t, bool v, GameObject obj)
    {
        _type = t;
        _isView = v;
        _objBlock = obj;
    }
}
