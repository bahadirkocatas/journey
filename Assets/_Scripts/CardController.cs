using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using Game.Data;

public class CardController : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Fizik Ayarları (Config)")]
    [SerializeField] private CardSwipeConfig swipeConfig;

    private RectTransform _rectTransform;
    private Vector3 _originalPos;
    private bool _isDragging = false;
    private bool _isCardProcessed = false;
    private CanvasGroup _canvasGroup;

    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _originalPos = _rectTransform.anchoredPosition;

        // Yeni kart yüklendiğinde sıfırla (Event ile)
        GameEvents.OnCardLoaded += ResetCardPosition;
    }

    private void OnDestroy()
    {
        GameEvents.OnCardLoaded -= ResetCardPosition;
    }

    private void Update()
    {
        if (!_isDragging && !_isCardProcessed)
        {
            _rectTransform.anchoredPosition = Vector3.Lerp(_rectTransform.anchoredPosition, _originalPos, Time.deltaTime * swipeConfig.snapSpeed);
            _rectTransform.localRotation = Quaternion.Lerp(_rectTransform.localRotation, Quaternion.identity, Time.deltaTime * swipeConfig.snapSpeed);

            if (_canvasGroup != null)
                _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, 1f, Time.deltaTime * swipeConfig.snapSpeed * 2f);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_isCardProcessed || CardManager.Instance.currentCard == null) return;
        _isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isCardProcessed || !_isDragging || CardManager.Instance.currentCard == null) return;

        float deltaX = eventData.delta.x;

        // Kilit kontrolü
        if (deltaX > 0 && CardManager.Instance.isRightLocked)
            deltaX /= (1f + (_rectTransform.anchoredPosition.x / swipeConfig.lockResistance));
        else if (deltaX < 0 && CardManager.Instance.isLeftLocked)
            deltaX /= (1f + (Mathf.Abs(_rectTransform.anchoredPosition.x) / swipeConfig.lockResistance));

        _rectTransform.anchoredPosition += new Vector2(deltaX, 0);

        float rotationZ = -_rectTransform.anchoredPosition.x * swipeConfig.rotationSpeed;
        _rectTransform.localRotation = Quaternion.Euler(0, 0, rotationZ);

        // UIHandler'a haber ver (preview ve indikatörler)
        bool isRight = _rectTransform.anchoredPosition.x > 0;
        if (Mathf.Abs(_rectTransform.anchoredPosition.x) > 10f)
        {
            GameEvents.OnCardDragged?.Invoke(isRight, CardManager.Instance.currentCard);
        }
        else
        {
            GameEvents.OnCardDragged?.Invoke(false, null); // null = temizle
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_isCardProcessed || !_isDragging) return;
        _isDragging = false;

        float finalX = _rectTransform.anchoredPosition.x;

        if (finalX >= swipeConfig.threshold && !CardManager.Instance.isRightLocked)
            StartCoroutine(ProcessCard(true));
        else if (finalX <= -swipeConfig.threshold && !CardManager.Instance.isLeftLocked)
            StartCoroutine(ProcessCard(false));
        else
            ResetCardPosition();
    }

    private IEnumerator ProcessCard(bool isRight)
    {
        _isCardProcessed = true;
        CardData processedCard = CardManager.Instance.currentCard;

        Vector2 targetPos = isRight
            ? new Vector2(Screen.width * 2f, _rectTransform.anchoredPosition.y)
            : new Vector2(-Screen.width * 2f, _rectTransform.anchoredPosition.y);

        while (Vector2.Distance(_rectTransform.anchoredPosition, targetPos) > 10f)
        {
            _rectTransform.anchoredPosition = Vector2.MoveTowards(_rectTransform.anchoredPosition, targetPos, swipeConfig.flySpeed * Time.deltaTime);
            yield return null;
        }

        // Event fırlat – UIHandler preview'ları temizleyecek
        GameEvents.OnCardSwiped?.Invoke(processedCard, isRight);

        _isCardProcessed = false;
    }

    private void ResetCardPosition()
    {
        _rectTransform.anchoredPosition = _originalPos;
        _rectTransform.localRotation = Quaternion.identity;
        _isDragging = false;
        _isCardProcessed = false;

        // UIHandler preview'ları temizlesin
        GameEvents.OnCardDragged?.Invoke(false, null);
    }
}