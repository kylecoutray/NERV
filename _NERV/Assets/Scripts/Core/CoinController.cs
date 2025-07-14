using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CoinController : MonoBehaviour
{
    public static CoinController Instance { get; private set; }
    public System.Action OnCoinBarFilled; // add this at the top
    public bool CoinBarWasJustFilled = false;


    [Header("UI References")]
    public RectTransform CanvasRect;       // full‐screen UI Canvas
    public RectTransform CoinBar;          // the bar holding Slot0…SlotN-1
    public Image CoinBarBG;        // background Image behind the bar
    public GameObject MovingCoinPrefab; // TaskObjects/MovingCoin.prefab

    [Header("Bar Settings")]
    public int CoinBarSize = 5;
    public float MoveDuration = 0.5f;
    public float FlashDuration = 0.1f;
    public int FlashRepeats = 2;

    [Header("Punishment Flash")]
    public int PunishmentFlashes = 2;     // how many on/off flashes
    public float PunishmentFlashDur = 0.2f;   // each on/off duration

    Image[] _slots;
    int _coinsAccumulated;
    public int CurrentCoins => _coinsAccumulated; // public getter for current coins
    public System.Action<int> OnCoinsChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        

        // Auto‐find the real UI Canvas if needed
        if (CanvasRect == null || CanvasRect.GetComponent<Canvas>() == null)
        {
            var canv = FindObjectOfType<Canvas>(true);
            if (canv != null) CanvasRect = canv.GetComponent<RectTransform>();
        }

        // Cache & gray out slots
        _slots = new Image[CoinBarSize];
        for (int i = 0; i < CoinBarSize; i++)
        {
            _slots[i] = CoinBar.GetChild(i).GetComponent<Image>();
            _slots[i].color = new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        // Make BG transparent
        CoinBarBG.color = new Color(0, 0, 0, 0);
        _coinsAccumulated = 0;
    }

    /// <summary>
    /// Spawn `n` coins at the screen‐space click point, then animate them into the bar.
    /// </summary>
    public void AddCoinsAtScreen(int n, Vector2 screenPos)
    {
        StartCoroutine(AddCoinsRoutine(n, screenPos));


    }

    private IEnumerator AddCoinsRoutine(int n, Vector2 screenPos)
    {
        // Cache Canvas and camera for render-mode handling
        var canvasComp = CanvasRect.GetComponent<Canvas>();
        Camera cam = (canvasComp.renderMode == RenderMode.ScreenSpaceOverlay)
                    ? null
                    : canvasComp.worldCamera;

        for (int i = 0; i < n && _coinsAccumulated < CoinBarSize; i++)
        {
            // 1) Instantiate under the full-screen Canvas
            var go = Instantiate(MovingCoinPrefab, CanvasRect, false);
            go.transform.SetAsLastSibling();
            var rt = go.GetComponent<RectTransform>();

            // center pivot/anchors so position = screen pixels or world point
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot      = new Vector2(0.5f, 0.5f);

            // make this moving coin green
            var moveImg = go.GetComponent<Image>();
            if (moveImg != null)
                moveImg.color = Color.green;

            // 2) Compute start position based on render mode
            Vector3 startPos;
            if (canvasComp.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                startPos = new Vector3(screenPos.x, screenPos.y + 10f, 0f);
            }
            else
            {
                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    CanvasRect,
                    screenPos + Vector2.up * 10f,
                    cam,
                    out startPos
                );
            }
            rt.position = startPos;

            // 3) Compute the target slot’s position (world-space works for all modes)
            var slotRT = _slots[_coinsAccumulated].rectTransform;
            Vector3 slotPos = slotRT.position;

            // 4) Animate from start → slot
            float t = 0f;
            while (t < MoveDuration)
            {
                t += Time.deltaTime;
                rt.position = Vector3.Lerp(startPos, slotPos, t / MoveDuration);
                yield return null;
            }
            rt.position = slotPos;
            Destroy(go);

            // 5) Fill that slot & notify
            _slots[_coinsAccumulated].color = Color.white;
            _coinsAccumulated++;
            OnCoinsChanged?.Invoke(_coinsAccumulated);
        }

        // 6) Flash & reset if full
        if (_coinsAccumulated >= CoinBarSize)
        {
            CoinBarWasJustFilled = true;
            OnCoinBarFilled?.Invoke();
            yield return StartCoroutine(FlashAndReset());
            CoinBarWasJustFilled = false;
        }
    }





    IEnumerator FlashAndReset()
    {
        for (int i = 0; i < FlashRepeats; i++)
        {
            CoinBarBG.color = Color.red;
            yield return new WaitForSeconds(FlashDuration);
            CoinBarBG.color = Color.blue;
            yield return new WaitForSeconds(FlashDuration);
        }
        CoinBarBG.color = new Color(0, 0, 0, 0);

        for (int i = 0; i < _slots.Length; i++)
            _slots[i].color = new Color(0.5f, 0.5f, 0.5f, 1f);

        _coinsAccumulated = 0;
    }


    /// <summary>
    /// Flash the last-filled slot n times, then remove that coin from the bar.
    /// </summary>
    public void RemoveCoins(int n = 1)
    {
        StartCoroutine(RemoveCoinsRoutine(n));
    }

    private IEnumerator RemoveCoinsRoutine(int n)
    {
        for (int i = 0; i < n; i++)
        {
            if (_coinsAccumulated <= 0)
                yield break;

            // index of the last filled slot
            int idx = _coinsAccumulated - 1;
            var img = _slots[idx];

            // flash it on/off
            Color orig = img.color;
            for (int f = 0; f < PunishmentFlashes; f++)
            {
                img.color = Color.red; // flash red
                yield return new WaitForSeconds(PunishmentFlashDur);
                img.color = orig;
                yield return new WaitForSeconds(PunishmentFlashDur);
            }

            // now “remove” it: grey it out and decrement
            img.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            _coinsAccumulated--;
        }
    }


}
