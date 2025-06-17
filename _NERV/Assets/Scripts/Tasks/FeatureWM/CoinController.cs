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

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);

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
        for (int i = 0; i < n && _coinsAccumulated < CoinBarSize; i++)
        {
            // 1) Instantiate under the full‐screen Canvas, keep world position
            var go = Instantiate(MovingCoinPrefab, CanvasRect, false);
            go.transform.SetAsLastSibling();  // on top
            var rt = go.GetComponent<RectTransform>();

            // center pivot/anchors so position = screen pixels
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            // make this moving coin green
            var moveImg = go.GetComponent<Image>();
            if (moveImg != null)
                moveImg.color = Color.green;


            // 2) Directly place the coin at the click screen pixel
            rt.position = new Vector3(screenPos.x, screenPos.y + 10, 0f);


            // 3) Compute the target slot’s world position (UI slot)
            var slotRT = _slots[_coinsAccumulated].rectTransform;
            Vector3 slotPos = slotRT.position;  // world‐space UI position

            // 4) Animate from click → slot
            float t = 0f;
            Vector3 start = rt.position;
            while (t < MoveDuration)
            {
                t += Time.deltaTime;
                go.transform.position = Vector3.Lerp(start, slotPos, t / MoveDuration);
                yield return null;
            }
            go.transform.position = slotPos;
            Destroy(go);

            // 5) Fill that slot
            _slots[_coinsAccumulated].color = Color.green;
            _coinsAccumulated++;
        }


        // 6) Flash & reset if full
        if (_coinsAccumulated >= CoinBarSize)
        {
            CoinBarWasJustFilled = true;
            OnCoinBarFilled?.Invoke();
            yield return StartCoroutine(FlashAndReset());
            CoinBarWasJustFilled = false; // reset after
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
            Color off   = new Color(orig.r, orig.g, orig.b, 0f);
            for (int f = 0; f < PunishmentFlashes; f++)
            {
                img.color = off;
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
