using System;
using UnityEngine;

[Serializable]
public struct UpgradeLayoutPreset
{
    public UpgradeSubMenuLayout layout;
    public Sprite layoutSprite;

    [Tooltip("1920x1080 屏幕像素坐标，原点在左上角。")]
    public Rect panelRect;

    [Tooltip("升级按钮区域，可在 Inspector 中自行微调。")]
    public Rect buttonRect;
}
