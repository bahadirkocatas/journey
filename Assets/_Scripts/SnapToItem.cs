using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

[System.Serializable]
public class IntEvent : UnityEvent<int> { }

/// <summary>
/// ScrollRect içindeki elementleri belirli koordinatlara (Snap) oturtan evrensel UI aracý.
/// MVP GÜNCELLEMESÝ: MainMenuManager (Spagetti) baðýmlýlýðý tamamen silindi! 
/// Endüstri standardý olan UnityEvent (Observer Pattern) mimarisine geçildi.
/// Artýk bu scripti oyunun herhangi bir yerinde, herhangi bir menüde güvenle kullanabilirsin.
/// </summary>
public class SnapToItem : MonoBehaviour, IEndDragHandler, IBeginDragHandler
{
    [Header("Ayarlar")]
    public float snapSpeed = 10f;
    public float snapThreshold = 200f;

    [Header("HEDEF KOORDÝNATLAR (Elle Doldur)")]
    public List<Vector2> snapPositions = new List<Vector2>();

    [Header("Gerekli Bileþenler")]
    public ScrollRect scrollRect;
    public RectTransform contentPanel;

    [Header("Olaylar (Events)")]
    public IntEvent OnItemSnapped; // Hangi index'e oturduðunu Inspector'dan dýþarýya baðýrýr

    private int selectedIndex = 0;
    private bool isSnapping = false;
    private Vector2 targetPos;
    private bool isDragging = false;

    void Start()
    {
        if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
        if (contentPanel == null && scrollRect != null) contentPanel = scrollRect.content;

        if (snapPositions.Count == 0)
        {
            Debug.LogWarning("[SNAP TO ITEM]: Hedef koordinatlar (Snap Positions) boþ! Lütfen Inspector'dan doldurun.");
            return;
        }

        // Baþlangýçta 0'a git
        Invoke("SnapToFirst", 0.1f);
    }

    void SnapToFirst()
    {
        SnapToPanel(0);
    }

    void Update()
    {
        if (!isDragging && isSnapping)
        {
            if (scrollRect.velocity.magnitude < snapThreshold)
            {
                scrollRect.velocity = Vector2.zero;

                float newX = Mathf.Lerp(contentPanel.anchoredPosition.x, targetPos.x, Time.deltaTime * snapSpeed);
                contentPanel.anchoredPosition = new Vector2(newX, contentPanel.anchoredPosition.y);

                if (Mathf.Abs(contentPanel.anchoredPosition.x - targetPos.x) < 1f)
                {
                    contentPanel.anchoredPosition = new Vector2(targetPos.x, contentPanel.anchoredPosition.y);
                    isSnapping = false;
                }
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        isSnapping = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        FindClosestPanel();
    }

    void FindClosestPanel()
    {
        float currentPosX = contentPanel.anchoredPosition.x;
        float minDistance = float.MaxValue;
        int bestIndex = 0;

        for (int i = 0; i < snapPositions.Count; i++)
        {
            float dist = Mathf.Abs(currentPosX - snapPositions[i].x);
            if (dist < minDistance)
            {
                minDistance = dist;
                bestIndex = i;
            }
        }

        SnapToPanel(bestIndex);
    }

    public void SnapToPanel(int index)
    {
        if (index < 0 || index >= snapPositions.Count) return;

        selectedIndex = index;
        targetPos = snapPositions[index];
        isSnapping = true;

        // MVP REFACTOR: MainMenuManager'ý zorla bulup fonksiyon çaðýrmak yerine,
        // sadece "Ben oturdum" diye baðýrýr. Dinleyen varsa duyar.
        OnItemSnapped?.Invoke(selectedIndex);
    }

    public void SnapNext()
    {
        if (selectedIndex < snapPositions.Count - 1) SnapToPanel(selectedIndex + 1);
    }

    public void SnapPrevious()
    {
        if (selectedIndex > 0) SnapToPanel(selectedIndex - 1);
    }
}