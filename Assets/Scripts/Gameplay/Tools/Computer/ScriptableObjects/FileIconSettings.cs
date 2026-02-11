using UnityEngine;

[CreateAssetMenu(fileName = "FileIconSettings", menuName = "Computer/File Icon Settings")]
public class FileIconSettings : ScriptableObject
{
    [Header("Default File Type Icons")]
    public Sprite documentIcon;
    public Sprite photoIcon;
    public Sprite videoIcon;
    public Sprite audioIcon;
    public Sprite folderIcon;
    public Sprite fingerprintIcon;
    public Sprite unknownIcon;
    
    [Header("File Icon Prefab")]
    public GameObject fileIconPrefab;
    
    [Header("Animation Settings")]
    [Range(0.1f, 2f)]
    public float iconLoadDelay = 0.2f;
    [Range(0.1f, 1f)]
    public float iconFadeInDuration = 0.3f;
} 