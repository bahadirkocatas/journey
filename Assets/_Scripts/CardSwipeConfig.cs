using UnityEngine;

[CreateAssetMenu(fileName = "CardSwipeConfig", menuName = "Game/Card Swipe Config")]
public class CardSwipeConfig : ScriptableObject
{
    [Header("Sürükleme Eþikleri")]
    public float threshold = 300f;
    public float flySpeed = 2500f;
    public float snapSpeed = 10f;
    public float rotationSpeed = 0.05f;
    public float lockResistance = 50f;
}